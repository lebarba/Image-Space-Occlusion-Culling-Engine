#include "StdAfx.h"
#include "TiledRasterizer.h"
#include "OcclusionEngineLib.h"
#include "Utils.h"

#define MIN_CHUNK_TO_PARALLELIZE 10

TiledRasterizer::TiledRasterizer(TiledDepthBuffer *pdepthBuffer)
{
	depthBuffer = pdepthBuffer;

	completelyInsideOccluderTiles.reserve(depthBuffer->tiles.size());
	borderOccluderTiles.reserve(depthBuffer->tiles.size());
	testTiles.reserve(depthBuffer->tiles.size());
}

TiledRasterizer::~TiledRasterizer(void) 
{

}

void calculateBoundingBox(	int boundingBox[2][2],
							const Occluder::OccluderPoint points[], 
							const int numberOfPoints,
							const int width, const int height)
{

	//Initialize with the first value already and make one less comparison.

	//Minimum X.
	boundingBox[0][0] = points[0].x;
	//Minimum Y.
	boundingBox[0][1] = points[0].y;
	//Maximum X.
	boundingBox[1][0] = points[0].x;
	//Maximum Y.
	boundingBox[1][1] = points[0].y;

	
	for( int i = 1 ; i < numberOfPoints ; i++ ) {
		
		//Find Minimum X.
		if( points[i].x < boundingBox[0][0])
			boundingBox[0][0] = points[i].x;


		//Find Minimum Y.
		if( points[i].y < boundingBox[0][1])
			boundingBox[0][1] = points[i].y;


		//Find Maximum X.
		if( points[i].x > boundingBox[1][0])
			boundingBox[1][0] = points[i].x;

	
		//Find Maximum Y.
		if( points[i].y > boundingBox[1][1])
			boundingBox[1][1] = points[i].y;
	}

	//Clip bounding box to buffer limits.
	boundingBox[0][0] = clampTo(boundingBox[0][0], 0, width);
	boundingBox[1][0] = clampTo(boundingBox[1][0], 0, width);

	boundingBox[0][1] = clampTo(boundingBox[0][1], 0, height);
	boundingBox[1][1] = clampTo(boundingBox[1][1], 0, height);
}


//Given three 2D points non-aligned points returns the three edge coefficients.
void calculateEdgeCoefficients( int edges[MAX_EDGES][3], 
								const Occluder::OccluderPoint points[], 
								const int numberOfPoints ) 
{

	int nextPoint;

	//Calculate the edge equations for every available edge in the hull.
	for( int i = 0 ; i < numberOfPoints ; i++ ) {

		//Find edge equations for Edge i.
		nextPoint = (i+1) % numberOfPoints;
		edges[i][0] = points[nextPoint].y - points[i].y;
		edges[i][1] = points[i].x -points[nextPoint].x;

		//Haven't precision issues for C coefficient since points are integers.
		edges[i][2] = -points[i].x * points[nextPoint].y + points[i].y * points[nextPoint].x;

		//Assure that third point is in the positive plane of the line formed by the first and second point.
		if( edges[i][0] * points[(i+2) % numberOfPoints].x + edges[i][1] * points[(i+2) % numberOfPoints].y + edges[i][2] < 0 ) 
		{
			//If in the negative side, negate coefficients to change the halfspace.
			edges[i][0] *= -1;
			edges[i][1] *= -1;
			edges[i][2] *= -1;
		}
	}

	//For the remamining unused edges set default values so they dont mess with SSE parallel tests.
	for( int i = numberOfPoints ; i < MAX_EDGES ; i++ )
	{
		edges[i][0] = edges[i][1] = 0;
		edges[i][2] = 0;
	}

}

//Returns the tile number for a given point in the tiled depthbuffer.
inline int getTileNumber( const int x, const int y, const int tileSize, const int screenWidth)
{
	return (x / tileSize) + (y/tileSize) * (screenWidth / tileSize);
}

//Returns the offset in the dephbuffer for a given x, y position in the tiled depthbuffer.
inline int getBufferPositionOffset( const int x, const int y, const int tileSize, const int screenWidth)
{
	return ( y % tileSize)*tileSize + (x % tileSize) + getTileNumber(x,y,tileSize,screenWidth)*tileSize*tileSize;
}

//Based on the convex hull bounding box get all the tiles inside that rectangle.
void getListOfPotentialTilesInsideBoundingBox( std::vector<Tile*> &tiles, 
											   const int convexHullBoundingBox[2][2], 
											   const std::vector<Tile*> &allBufferTiles,
											   const int tileSize, const int bufferWidth)

{

	int x1,x2,y1,y2;

	x1 =  convexHullBoundingBox[0][0];
	x2 =  convexHullBoundingBox[1][0];
	
	y1 =  convexHullBoundingBox[0][1];
	y2 =  convexHullBoundingBox[1][1];

	tiles.clear();

		
	int rowOffset = bufferWidth / tileSize;

	int columnStart = x1 / tileSize;
	int columnEnd = x2 / tileSize;

	int rowStart = y1 / tileSize;
	int rowEnd =  y2 / tileSize;

	int totalTiles = (columnEnd-columnStart + 1) * ( rowEnd-rowStart + 1);
	
	//Reserve the total number of tiles inside the rectangle.
	tiles.resize(totalTiles);

	int tile;
	int row, column;

	int counter = 0;

	//Add all the tiles into the potential list
	for( row = rowStart ; row <= rowEnd; row ++ )
	{
		for( column = columnStart; column <= columnEnd ; column++ )
		{
			tile = column + row*rowOffset;
			tiles[counter] = allBufferTiles[tile];
			counter++;
		}
	}

}


//Given edge coefficients A and B, returns the trivial reject corner.
inline int getTileTrivialRejectCorner( const int A, const int B ) 
{
	//Tile corners
	//  0________1
	//  |        |
	//  |        |
	//  |________|
	//  3        2  

	if( A < 0 && B <= 0 )
		return 0;

	if( A >=0 && B < 0 )
		return 1;

	if( A > 0 && B >= 0 )
		return 2;

	//if( A <= 0 && B > 0 ) //There is no other combination so return 3.
		return 3;
}

//Given tile trivial reject corner, returns the trivial accept corner.
inline int getTileTrivialAcceptCorner( int corner ) 
{
	return (corner + 2 ) % 4;
}


void classifyTilesBasedOnTrivialCorners(const int edges[MAX_EDGES][3],
										const int numberOfEdges,
										const int convexHullBoundingBox[2][2], 
										const int tileSize, const int bufferWidth,
										const std::vector<Tile*> &allBufferTiles,
										std::vector<Tile*> &completelyInsideOccluderTiles,
										std::vector<Tile*> &borderOccluderTiles)
{	
	

	
	int x1,x2,y1,y2;

	x1 =  convexHullBoundingBox[0][0];
	x2 =  convexHullBoundingBox[1][0];
	
	y1 =  convexHullBoundingBox[0][1];
	y2 =  convexHullBoundingBox[1][1];

	int rowOffset = bufferWidth / tileSize;

	int columnStart = x1 / tileSize;
	int columnEnd = x2 / tileSize;

	int rowStart = y1 / tileSize;
	int rowEnd =  y2 / tileSize;

	int totalTiles = (columnEnd-columnStart + 1) * ( rowEnd-rowStart + 1);
	
	//Reserve the total number of tiles inside the rectangle.
	completelyInsideOccluderTiles.reserve(totalTiles);
	borderOccluderTiles.reserve(totalTiles);

	int tile;
	int row, column;

	int edgeTrivialRejectCorner[MAX_EDGES];
	int edgeTrivialAcceptCorner[MAX_EDGES];

	int edgeAccumulator[MAX_EDGES] = {0};

	//Find the trivial reject and accept corner for each edge.
	for( int c = 0 ; c < numberOfEdges ; c++ ) 
	{
		edgeTrivialRejectCorner[c] = getTileTrivialRejectCorner(edges[c][0], edges[c][1]);
		edgeTrivialAcceptCorner[c] = getTileTrivialAcceptCorner(edgeTrivialRejectCorner[c]);
	}


	int testPointsX[MAX_EDGES];
	int testPointsY[MAX_EDGES];
	int testPointX;
	int testPointY;

	

	int e;
	bool triviallyAccepted;

	//OpenMP directives for parallel region:
	//int chunk;
	//TODO: OpenMP commented because it seems to be working fine the serial way.
	//#if defined USE_OMP
	//	//Set the chunk to process by every thread.
	//	chunk = totalTiles / omp_get_max_threads();
	//	if( chunk == 0 ) chunk = 1;

	//	#pragma omp parallel shared (rowStart, rowEnd, columnStart, columnEnd, numberOfEdges, rowOffset, allBufferTiles, edgeTrivialRejectCorner, edgeTrivialAcceptCorner, Tile::size )  \
	//						 private(row, column, e, tile,testPointsX, testPointsY, testPointX, testPointY, edgeAccumulator, triviallyAccepted )
	//#endif
	//	{
	//#if defined USE_OMP
	//	#pragma omp for schedule(static, chunk)
	//#endif


	//Add all the tiles into the potential list
	for( row = rowStart ; row <= rowEnd; row ++ )
	{

		tile = columnStart + row*rowOffset;

		for( e = 0 ; e < numberOfEdges; e++ )
		{
			//The position of the trivial reject corner.
			testPointsX[e] = allBufferTiles[tile]->extremePoints[edgeTrivialRejectCorner[e]][0];
			testPointsY[e] = allBufferTiles[tile]->extremePoints[edgeTrivialRejectCorner[e]][1];
			edgeAccumulator[e] = edges[e][0] * testPointsX[e] +  edges[e][1] * testPointsY[e] +  edges[e][2];
		}

		for( column = columnStart; column <= columnEnd ; column++ )
		{
			//Current tile number in the sreen.
			tile = column + row*rowOffset;


			//If all edges are positive in the trivial reject corner then the tile is not discarded.
			if((edgeAccumulator[0] |
				edgeAccumulator[1] |
				edgeAccumulator[2] |
				edgeAccumulator[3]  ) >= 0 )
			{
					triviallyAccepted = true;

					//Check every edge for trivial accept. If there is at least one reject for the trivial accept, then it is partially covered.
					for( e = 0 ; e < numberOfEdges; e++ )
					{
						//The position of the trivial reject corner.
						testPointX = allBufferTiles[tile]->extremePoints[edgeTrivialAcceptCorner[e]][0];
						testPointY = allBufferTiles[tile]->extremePoints[edgeTrivialAcceptCorner[e]][1];

						//if at least one trivial reject, then exclude tile from list.
						if( edges[e][0] * testPointX +  edges[e][1] * testPointY +  edges[e][2] < 0 )
						{
							triviallyAccepted = false;
							break;
						}
					}

					if( triviallyAccepted == true)
						//#pragma omp critical
						completelyInsideOccluderTiles.push_back(allBufferTiles[tile]);
					else
						//#pragma omp critical
						borderOccluderTiles.push_back(allBufferTiles[tile]);

			}


			edgeAccumulator[0] += edges[0][0]*Tile::size;
			edgeAccumulator[1] += edges[1][0]*Tile::size;
			edgeAccumulator[2] += edges[2][0]*Tile::size;
			edgeAccumulator[3] += edges[3][0]*Tile::size;

		}
	}

		//}//End of OpenMP parallel scope

}

//Finds the maximum and minimum depth of an occluder. 
//Since it is planar, we only needs to find the extreme points.
void getOccluderMinMaxDepth(float &minOccluderDepthPoint,
							float &maxOccluderDepthPoint,
							const Occluder::OccluderPoint points[], 
							const int numberOfPoints )
{

	minOccluderDepthPoint = points[0].depth;
	maxOccluderDepthPoint = points[0].depth;

	for( int i = 1 ; i < numberOfPoints ; i++ )
	{
		if( points[i].depth < minOccluderDepthPoint )
			minOccluderDepthPoint = points[i].depth;
		
		if( points[i].depth > maxOccluderDepthPoint )
			maxOccluderDepthPoint = points[i].depth;
	}

}


inline const float interpolateDepth(const int x,  const int y,
									const int x1, const int y1,
									const int x2, const int y2,
									const int x3, const int y3,
									const float depthP1, const float depthP2, const float depthP3, 
									const float triangleArea)

{

	float u, v, w;

	float Ax, Bx, Ay, By;
	const float denominator = -1.0f / ( (y2-y3)*(x1-x3)+(x3-x2)*(y1-y3) );

	Ax = (float) x2 - x3;
	Ay = (float)  y2 - y3;
	Bx = (float)  x - x3;
	By = (float)  y - y3;
	u = denominator*( Ax*By - Bx*Ay );

	Ax = (float) x1 - x3;
	Ay = (float) y1 - y3;
	Bx = (float) x - x3;
	By = (float) y - y3;
	v = -denominator*( Ax*By - Bx*Ay );

	Ax = (float) x1 - x2;
	Ay = (float) y1 - y2;
	Bx = (float) x - x2;
	By = (float) y - y2;
	w = denominator*( Ax*By - Bx*Ay );


	return u*depthP1  + v*depthP2  + w*depthP3;
}

//Interpolate values in x,y based on precalculated factors.
inline const float interpolateDepthOptimized(const float x,  const float y,
											 const int Ax1, const int Ay1,
											 const int Ax2, const int Ay2,
											 const int Ax3, const int Ay3,
											 const int x2,  const int y2,
											 const int x3,  const int y3,
											 const float inverseDenominator,
											 const float depthP1, const float depthP2, const float depthP3, 
											 const float triangleArea)
{

	float u, v, w;

	float Bx, By;

	Bx = x - x3;
	By = y - y3;
	u = ( Ax1*By - Bx*Ay1 );

	Bx = x - x3;
	By = y - y3;
	v = -( Ax2*By - Bx*Ay2 );

	Bx = x - x2;
	By = y - y2;
	w = ( Ax3*By - Bx*Ay3 );

	return (u*depthP1  + v*depthP2  + w*depthP3)*inverseDenominator;
}

//Rasterizes a convex hull with a fixed depth.
void TiledRasterizer::rasterizeConvexHull(const Occluder occluder)										   
{

	//The bounding box of the Convex Hull.
	int convexHullBoundingBox[2][2];
	
	std::vector<Tile*> pendingRasterizationTiles(depthBuffer->tiles.size());


	tilesDefferredFromRasterization = 0;
	
	completelyInsideOccluderTiles.clear();
	borderOccluderTiles.clear();
	pendingRasterizationTiles.clear();


	//The list of all the edges of the convex hull with all interior points in the positive side of the half planes.
	int convexHullEdgeList[MAX_EDGES][3] = {0}; //Consists of [edgeNumber][Coefficient]

	//The minumum and maximum depth values of the occluder.
	float minOccluderDepthPoint;
	float maxOccluderDepthPoint;

	const int numberOfEdges = occluder.numberOfPoints;
	
	//Calculate edge equations.
	calculateEdgeCoefficients( convexHullEdgeList, occluder.points, numberOfEdges);
	

	//Form a triangle with the first three points. Used later to interpolate.
	const float triangleArea = (convexHullEdgeList[0][2] + convexHullEdgeList[1][2] + convexHullEdgeList[2][2]) * 0.5f;

	//Get the convex hull bounding box.
	calculateBoundingBox(convexHullBoundingBox, occluder.points, numberOfEdges, depthBuffer->width -1, depthBuffer->height -1);
		

	//Discard convex polygons that have no thickness in any of the dimensions.
	if( convexHullBoundingBox[0][0] == convexHullBoundingBox[1][0] ||
		convexHullBoundingBox[0][1] == convexHullBoundingBox[1][1] )
		return;

	//Get Occluder minimum and maximum depth.
	getOccluderMinMaxDepth(minOccluderDepthPoint, maxOccluderDepthPoint, occluder.points, numberOfEdges);
	
	//From the potential list of tiles, determine those fully covered and those partially covered.
	classifyTilesBasedOnTrivialCorners(convexHullEdgeList, numberOfEdges, convexHullBoundingBox, Tile::size, depthBuffer->width,
									   depthBuffer->tiles, completelyInsideOccluderTiles, borderOccluderTiles);
	
	

	Tile *currentTile;

	const int px1 = occluder.points[0].x;
	const int px2 = occluder.points[1].x;
	const int px3 = occluder.points[2].x;

	const int py1 = occluder.points[0].y;
	const int py2 = occluder.points[1].y;
	const int py3 = occluder.points[2].y;

	const float z1 = clampTo(occluder.points[0].depth, 0.0f, 1.0f); //Clamp depth values between 0 and 1.
	const float z2 = clampTo(occluder.points[1].depth, 0.0f, 1.0f);
	const float z3 = clampTo(occluder.points[2].depth, 0.0f, 1.0f);

			
	//Precalculate coefficients for the barycentric interpolation.
 	const int Ax1 = px2 - px3;
	const int Ay1 = py2 - py3;
	const int Ax2 = px1 - px3;
	const int Ay2 = py1 - py3;
	const int Ax3 = px1 - px2;
	const int Ay3 = py1 - py2;
	const float inverseDenominator = -1.0f / ( (Ay1)*(Ax2)-(Ax1)*(Ay2) );

		
	float initialDepth;

	//Loop for every tile completly covered (inside) by the convex hull.
	for( unsigned int i = 0 ; i < completelyInsideOccluderTiles.size() ; i++ ) 
	{

		Tile::ETileStatus newTileStatus;

		currentTile = completelyInsideOccluderTiles[i];

		newTileStatus = currentTile->status;


		//If a not initialized Tile has to be completelly filled, get the four depth corners and set the status to CompletelyCoveredNotRasterized.
		if( currentTile->status == Tile::NotInitialized ) {
			
			newTileStatus = Tile::CompletelyCoveredNotRasterized;

			//Calcuate the depth for the tile four extreme points and find the minimum depth of them.
			for( int p = 0 ; p < 4 ; p++)
			{
				currentTile->depthPoints[p] = interpolateDepthOptimized((float)currentTile->extremePoints[p][0], (float)currentTile->extremePoints[p][1],
																		Ax1, Ay1,
																		Ax2, Ay2,
																		Ax3, Ay3,
																		px2, py2,
																		px3, py3,
																		inverseDenominator,
																		z1, z2, z3,
																		triangleArea);

				//Find the minimum depth for the tile.
				if( currentTile->depthPoints[p] < currentTile->minDepth  )
					currentTile->minDepth  = currentTile->depthPoints[p];
			}

			tilesDefferredFromRasterization++;
		}

		//If the tile was completely covered before but not rasterized, need to check if the
		//new convex hull depth fully covers all pixels (i.e has closer depth on all points).
		if( currentTile->status == Tile::CompletelyCoveredNotRasterized ) 
		{

			float newHullDepth[4]; //The new hull depth for each of the four extremes.
			int cornersWithLessDepth = 0; // The number of extremes with less depth.

			//Get the depth for the new hull in every of the four extreme points.
			for( int p = 0 ; p < 4 ; p++)
			{
				
				newHullDepth[p] = interpolateDepthOptimized((float)currentTile->extremePoints[p][0], (float)currentTile->extremePoints[p][1],
															Ax1, Ay1,
															Ax2, Ay2,
															Ax3, Ay3,
															px2, py2,
															px3, py3,
															inverseDenominator,
															z1, z2, z3,
															triangleArea);
				
				if( newHullDepth[p] < currentTile->depthPoints[p] ){
					cornersWithLessDepth++;
				}

				//Find the minimum depth for the tile.
				if( newHullDepth[p] < currentTile->minDepth  )
					currentTile->minDepth  = newHullDepth[p];
			}

			//If no corners are closer in depth, then the tile is left intact since old tile is completely occluding the new hull region.
			if( cornersWithLessDepth == 0 ) {
				
				newTileStatus = Tile::CompletelyCoveredNotRasterized;
				tilesDefferredFromRasterization++;

			}else {

				//If at least there is one corner with less depth, then old tile content has to be rasterized with new hull content.
				if( cornersWithLessDepth < 4 ) {
				
					newTileStatus = Tile::Rasterized;
					pendingRasterizationTiles.push_back(currentTile);
				}

				//The new hull region fully covers the previous tile content, however no need to rasterize.
				if( cornersWithLessDepth == 4 ) {
				
					newTileStatus = Tile::CompletelyCoveredNotRasterized;

					for( int p = 0 ; p < 4 ; p++)
						 currentTile->depthPoints[p] = newHullDepth[p];

					tilesDefferredFromRasterization++;
				}
			}

		}


		//If the tile was previously partially covered need to determine if all the four extreme points of the new hull 
		//will be closer in depth to the maximum value of the previous content.
		if( currentTile->status == Tile::Rasterized) 
		{

			float newHullDepth[4]; //The new hull depth for each of the four extremes.
			bool tileFullyCovered;

			tileFullyCovered = true;

			//Calcuate the depth for the tile four extreme points.
			for( int p = 0 ; p < 4 ; p++)
			{
				newHullDepth[p] = interpolateDepthOptimized((float)currentTile->extremePoints[p][0], (float)currentTile->extremePoints[p][1],
															Ax1, Ay1,
															Ax2, Ay2,
															Ax3, Ay3,
															px2, py2,
															px3, py3,
															inverseDenominator,
															z1, z2, z3,
															triangleArea);


				//If there is at least one extreme depth that is greater than the whole tile minimum depth.
				if( newHullDepth[p]  > currentTile->minDepth )
				{
					tileFullyCovered = false;
				}

			}

			//If the tile is fully covered by the current hull.
			if( tileFullyCovered == true ) 
			{
				for( int p = 0 ; p < 4 ; p++) 
				{
					currentTile->depthPoints[p] = newHullDepth[p];
		
					//Find the minimum depth for the tile.
					if( newHullDepth[p] < currentTile->minDepth  )
						currentTile->minDepth = newHullDepth[p];

				}

				newTileStatus = Tile::CompletelyCoveredNotRasterized;
				
				tilesDefferredFromRasterization++;

			}else
			{
				//Rasterize the tile since the new hull doesn't cover all the tile.
				//It will continue being partially covered.
				pendingRasterizationTiles.push_back(currentTile);

				newTileStatus = Tile::Rasterized;
			}
			
		}

		currentTile->previousStatus = currentTile->status;
		currentTile->status = newTileStatus;
	}
	
	

	//Treat the partially covered (border) tiles diffently.	
	for( unsigned int i = 0 ; i < borderOccluderTiles.size() ; i++ ) 
	{

		Tile::ETileStatus newTileStatus;

		currentTile = borderOccluderTiles[i];

		newTileStatus = currentTile->status;

		//If the new hull covers part of the tile, then it needs to be rasterized.
		if( currentTile->status == Tile::NotInitialized ||
			currentTile->status == Tile::Rasterized) 
		{
			newTileStatus = Tile::Rasterized;
			pendingRasterizationTiles.push_back(currentTile);
		}

		if( currentTile->status == Tile::CompletelyCoveredNotRasterized )

		{
			//Only change the status if the new occluder has at leas a closer depth than the whole tile.
			if( minOccluderDepthPoint <= currentTile->minDepth )
			{
				newTileStatus = Tile::Rasterized;
				pendingRasterizationTiles.push_back(currentTile);
			}
		}

		currentTile->previousStatus = currentTile->status;
		currentTile->status = newTileStatus;
	}
	
	//Tile extreme points.
	int x1, x2;
	int y1, y2;

	//Loop Counters
	int i;

	//Current point positions.
	int x,y;
	
	//The dz/dx and dz/dy.
	float xIncrement, yIncrement;
	
	//Values for interpolating using differentials.
	float valueX, valueY;

	//The size of the batch for each worker thread.
	int chunk;

	//The pointer to the first value of the current tile.
	float *DepthBufferTilePtr;

	float xIncrementConst;


	//SSE registers.
	__m128 xIncrements;
	__m128 xIncrementsMultiplier;
	__m128 valuesX;

	__m128 minValues;
	__m128 depthValues;

	__m128i edgesAcumulatorForPoint1; 
	__m128i edgesAcumulatorForPoint2; 
	__m128i edgesAcumulatorForPoint3; 
	__m128i edgesAcumulatorForPoint4; 

	__m128i edgeAccumulatorY; 
	__m128i initialEdgeValues; 

	__m128i convexhullEdgeA; 
	__m128i convexhullEdgeAIncrements;
	__m128i convexhullEdgeB; 

	__m128 edgePassMask;

	
	//The edge values to add every time the point changes in X.
	convexhullEdgeA.m128i_i32[0] = convexHullEdgeList[0][0];
	convexhullEdgeA.m128i_i32[1] = convexHullEdgeList[1][0];
	convexhullEdgeA.m128i_i32[2] = convexHullEdgeList[2][0];
	convexhullEdgeA.m128i_i32[3] = convexHullEdgeList[3][0];

	//Increment the edge values by four at a time.
	#if defined USE_SSE4
		convexhullEdgeAIncrements =_mm_mullo_epi32 (convexhullEdgeA, _mm_set1_epi32(4));
	#else
		convexhullEdgeAIncrements.m128i_i32[0] = convexhullEdgeA.m128i_i32[0] * 4;
		convexhullEdgeAIncrements.m128i_i32[1] = convexhullEdgeA.m128i_i32[1] * 4;
		convexhullEdgeAIncrements.m128i_i32[2] = convexhullEdgeA.m128i_i32[2] * 4;
		convexhullEdgeAIncrements.m128i_i32[3] = convexhullEdgeA.m128i_i32[3] * 4;
	#endif

	//The edge values to add every time the point changes in Y.
	convexhullEdgeB.m128i_i32[0] = convexHullEdgeList[0][1];
	convexhullEdgeB.m128i_i32[1] = convexHullEdgeList[1][1];
	convexhullEdgeB.m128i_i32[2] = convexHullEdgeList[2][1];
	convexhullEdgeB.m128i_i32[3] = convexHullEdgeList[3][1];


	//OpenMP directives for parallel region:
	#if defined USE_OMP

		if( numberOfThreads == 0 )
			numberOfThreads = omp_get_max_threads();
		else
			numberOfThreads = clampTo(numberOfThreads, 1, INT_MAX);

		omp_set_num_threads(numberOfThreads);
		
		//Set the chunk to process by every thread.
		chunk = pendingRasterizationTiles.size() / numberOfThreads;
		if( chunk == 0 ) chunk = 1;

		#pragma omp parallel if(chunk >= MIN_CHUNK_TO_PARALLELIZE) shared (convexHullEdgeList, convexhullEdgeA, convexhullEdgeB, convexhullEdgeAIncrements, pendingRasterizationTiles, chunk, triangleArea, Ax1, Ay1, Ax2, Ay2, Ax3, Ay3, inverseDenominator, Tile::size )  \
							 private(currentTile, edgePassMask, minValues, depthValues, edgesAcumulatorForPoint1, edgesAcumulatorForPoint2, edgesAcumulatorForPoint3, edgesAcumulatorForPoint4, edgeAccumulatorY, initialEdgeValues, DepthBufferTilePtr, xIncrementConst, valueX, valueY, valuesX, xIncrementsMultiplier, xIncrements, i, x, y, x1, x2, y1, y2, xIncrement, yIncrement, initialDepth)
	#endif
		{
	#if defined USE_OMP
			#pragma omp for schedule(static, chunk)
	#endif
			
	//Itereate for every tile pending for rasterization.
	for(  i = 0 ; i < (int) pendingRasterizationTiles.size() ; i++ ) 
	{
		
		currentTile = pendingRasterizationTiles[i];
		
		x1 = currentTile->extremePoints[0][0];
		x2 = currentTile->extremePoints[2][0];
		y1 = currentTile->extremePoints[0][1];
		y2 = currentTile->extremePoints[2][1];
				
		if( currentTile->status == Tile::Rasterized )
		{
			//Estimate the conservative minimm depth by assigning the minimum depth of the whole occluder.
			if( minOccluderDepthPoint < currentTile->minDepth)
				currentTile->minDepth = minOccluderDepthPoint;
		}
		else
		{
			//Estimate the conservative minimm depth by assigning the minimum depth of the whole occluder.
			currentTile->minDepth = minOccluderDepthPoint;
		}

		DepthBufferTilePtr = &(depthBuffer->buffer[getBufferPositionOffset(currentTile->extremePoints[0][0], currentTile->extremePoints[0][1], Tile::size, depthBuffer->width)]);

		//If the tile was previously completely covered and not rasterized and has become a partially covered tile,
		//then first we need to rasterizer the previous completely covered and then rasterizer the partial polygon over it.
		if( currentTile->status == Tile::Rasterized && 
			currentTile->previousStatus == Tile::CompletelyCoveredNotRasterized  ) 
		{

				//The depth increment per unit in x.
				xIncrementConst = (currentTile->depthPoints[1] - currentTile->depthPoints[0])/ (Tile::size - 1);

				//Create a 4 float register with the increments to add in each row start.
				xIncrementsMultiplier.m128_f32[0]  = 0;
				xIncrementsMultiplier.m128_f32[1]  = xIncrementConst;
				xIncrementsMultiplier.m128_f32[2]  = xIncrementConst * 2.0f;
				xIncrementsMultiplier.m128_f32[3]  = xIncrementConst * 3.0f;

				xIncrements = _mm_set1_ps( ( ( currentTile->depthPoints[1] - currentTile->depthPoints[0]) / (Tile::size - 1) )* 4);
				yIncrement  = (currentTile->depthPoints[3] - currentTile->depthPoints[0]) / (Tile::size - 1);

	
				valuesX  = _mm_set1_ps(currentTile->depthPoints[0]);
				valuesX = _mm_add_ps(valuesX, xIncrementsMultiplier);
	
				valueY = 0.0f;

				for( y = y1 ; y <= y2 ; y++ )
				{
					for( x = x1 ; x <= x2; x += 4 )
					{

						//Set four depth values at a time.
						_mm_store_ps(DepthBufferTilePtr, valuesX);

						//Increment four depth values.
						valuesX = _mm_add_ps(valuesX, xIncrements);

						//Move the buffer offset pointer four units ahead.
						DepthBufferTilePtr += 4;
					}
					
					valueY += yIncrement;

					valuesX  = _mm_set1_ps(currentTile->depthPoints[0] + valueY);
					valuesX = _mm_add_ps(valuesX, xIncrementsMultiplier);
				}


		}

		//The depth for top left point of the tile.
		initialDepth = interpolateDepthOptimized((float)currentTile->extremePoints[0][0], (float)currentTile->extremePoints[0][1], Ax1, Ay1, Ax2, Ay2, Ax3, Ay3, px2, py2,  px3, py3, inverseDenominator, z1, z2, z3, triangleArea);

		//The depth diference per every x increment.
		xIncrement  = (  interpolateDepthOptimized((float)currentTile->extremePoints[1][0], (float)currentTile->extremePoints[1][1], Ax1, Ay1, Ax2, Ay2, Ax3, Ay3, px2, py2,  px3, py3, inverseDenominator, z1, z2, z3, triangleArea)
			           - initialDepth )/ (Tile::size - 1);
			
		//The depth diference per every y increment.
		yIncrement  = (  interpolateDepthOptimized((float)currentTile->extremePoints[3][0], (float)currentTile->extremePoints[3][1], Ax1, Ay1, Ax2, Ay2, Ax3, Ay3, px2, py2,  px3, py3, inverseDenominator, z1, z2, z3, triangleArea)
			           - initialDepth )/ (Tile::size - 1);

		valueX = initialDepth;
		valueY = 0.0f;

		//Get the address of the first pixel of the tile.
		DepthBufferTilePtr = depthBuffer->buffer + getBufferPositionOffset(currentTile->extremePoints[0][0], currentTile->extremePoints[0][1], Tile::size, depthBuffer->width);

		//Calculate edge accumulators so only additions are used in the inner loop.
		initialEdgeValues.m128i_i32[0] = convexHullEdgeList[0][0] * x1 +  convexHullEdgeList[0][1] * y1 +  convexHullEdgeList[0][2];
		initialEdgeValues.m128i_i32[1] = convexHullEdgeList[1][0] * x1 +  convexHullEdgeList[1][1] * y1 +  convexHullEdgeList[1][2];
		initialEdgeValues.m128i_i32[2] = convexHullEdgeList[2][0] * x1 +  convexHullEdgeList[2][1] * y1 +  convexHullEdgeList[2][2];
		initialEdgeValues.m128i_i32[3] = convexHullEdgeList[3][0] * x1 +  convexHullEdgeList[3][1] * y1 +  convexHullEdgeList[3][2];


		//Set zero for the initial edges accumulator since we start at the top.
		edgeAccumulatorY = _mm_set1_epi32(0);
		
		//The variable to store the edges point test result.
		edgesAcumulatorForPoint1.m128i_i32[0] = initialEdgeValues.m128i_i32[0];
		edgesAcumulatorForPoint1.m128i_i32[1] = initialEdgeValues.m128i_i32[1];
		edgesAcumulatorForPoint1.m128i_i32[2] = initialEdgeValues.m128i_i32[2];
		edgesAcumulatorForPoint1.m128i_i32[3] = initialEdgeValues.m128i_i32[3];
						 
		//Keep adding the A coefficient every time the X changes for points 2, 3 and 4.
		edgesAcumulatorForPoint2 = _mm_add_epi32(edgesAcumulatorForPoint1, convexhullEdgeA);
		edgesAcumulatorForPoint3 = _mm_add_epi32(edgesAcumulatorForPoint2, convexhullEdgeA);
		edgesAcumulatorForPoint4 = _mm_add_epi32(edgesAcumulatorForPoint3, convexhullEdgeA);

		//The X depth increment for the four points.
		xIncrementsMultiplier.m128_f32[0] = 0;
		xIncrementsMultiplier.m128_f32[1] = xIncrement;
		xIncrementsMultiplier.m128_f32[2] = xIncrement * 2.0f;
		xIncrementsMultiplier.m128_f32[3] = xIncrement * 3.0f;

		xIncrements = _mm_set1_ps(xIncrement * 4);

		//Set the initial depth for the first value.
		valuesX = _mm_set1_ps(initialDepth);

		//Then add nothing to the first value, xIncrement for the second, twice xIncrement for the third and so on.
		valuesX = _mm_add_ps(valuesX, xIncrementsMultiplier);

		//Iterate for each pixel in the tile.

		
		
		for( y = y1 ; y <= y2 ; y++ )
		{
			//Do four depth values at a time.
			for( x = x1 ; x <= x2; x +=4 )
			{

				//Load four existing depth values at once.
				depthValues = _mm_load_ps(DepthBufferTilePtr);

				#if defined USE_SSE4 //if SSE4 blend instruction is available.

					//The edge pass mask is the coverage mask obtained by testing the four points at a time against the convex hull.
					//Do OR of all the four edges values so if there is at least one edge with negative value, the result will negative and the point will be outside the hull.
					//Do it for four points at a time.
					edgePassMask.m128_i32[0] = ((edgesAcumulatorForPoint1.m128i_i32[0] | edgesAcumulatorForPoint1.m128i_i32[1] | edgesAcumulatorForPoint1.m128i_i32[2] | edgesAcumulatorForPoint1.m128i_i32[3]) >= 0 ) ? 0x80000000 : 0x0;
					edgePassMask.m128_i32[1] = ((edgesAcumulatorForPoint2.m128i_i32[0] | edgesAcumulatorForPoint2.m128i_i32[1] | edgesAcumulatorForPoint2.m128i_i32[2] | edgesAcumulatorForPoint2.m128i_i32[3]) >= 0 ) ? 0x80000000 : 0x0;
					edgePassMask.m128_i32[2] = ((edgesAcumulatorForPoint3.m128i_i32[0] | edgesAcumulatorForPoint3.m128i_i32[1] | edgesAcumulatorForPoint3.m128i_i32[2] | edgesAcumulatorForPoint3.m128i_i32[3]) >= 0 ) ? 0x80000000 : 0x0;
					edgePassMask.m128_i32[3] = ((edgesAcumulatorForPoint4.m128i_i32[0] | edgesAcumulatorForPoint4.m128i_i32[1] | edgesAcumulatorForPoint4.m128i_i32[2] | edgesAcumulatorForPoint4.m128i_i32[3]) >= 0 ) ? 0x80000000 : 0x0;

					//Calculate the minimum values as if all the points passed the edge test.
					minValues = _mm_min_ps(depthValues, valuesX);

					//Blend the new depth values that passed the edge test and the old depth values that didn't pass the test and were in the depthbuffer before.
					depthValues = _mm_blendv_ps(depthValues, minValues, edgePassMask);

				#else

					//Do OR of all the four edges values so if there is at least one edge with negative value, the result will negative and the point will be outside the hull.
					//Do it for four points at a time.
					edgePassMask.m128_i32[0] = ((edgesAcumulatorForPoint1.m128i_i32[0] | edgesAcumulatorForPoint1.m128i_i32[1] | edgesAcumulatorForPoint1.m128i_i32[2] | edgesAcumulatorForPoint1.m128i_i32[3]) >= 0 ) ? 0xFFFFFFFF : 0x0;
					edgePassMask.m128_i32[1] = ((edgesAcumulatorForPoint2.m128i_i32[0] | edgesAcumulatorForPoint2.m128i_i32[1] | edgesAcumulatorForPoint2.m128i_i32[2] | edgesAcumulatorForPoint2.m128i_i32[3]) >= 0 ) ? 0xFFFFFFFF : 0x0;
					edgePassMask.m128_i32[2] = ((edgesAcumulatorForPoint3.m128i_i32[0] | edgesAcumulatorForPoint3.m128i_i32[1] | edgesAcumulatorForPoint3.m128i_i32[2] | edgesAcumulatorForPoint3.m128i_i32[3]) >= 0 ) ? 0xFFFFFFFF : 0x0;
					edgePassMask.m128_i32[3] = ((edgesAcumulatorForPoint4.m128i_i32[0] | edgesAcumulatorForPoint4.m128i_i32[1] | edgesAcumulatorForPoint4.m128i_i32[2] | edgesAcumulatorForPoint4.m128i_i32[3]) >= 0 ) ? 0xFFFFFFFF : 0x0;

					//Calculate the minimum values as if all the points passed the edge test. 
					//Equivalent to assigning (newValue < depthBufferValue) ? newValue : depthBufferValue
					minValues = _mm_min_ps(depthValues, valuesX);
					
					//Set the minimum values only for the points that passed the edge test.
					minValues = _mm_and_ps(minValues, edgePassMask);

					//Negate the edge test result mask so the values that didn't pass the test keep with the same depth buffer values.
					depthValues = _mm_andnot_ps(edgePassMask, depthValues); 
					
					//Merge the new depth values that passed the edge test and the old depth values that didn't pass the test and were in the depthbuffer before.
					depthValues = _mm_or_ps(minValues, depthValues);

				#endif

				//Store the four depth values at once.
				_mm_store_ps(DepthBufferTilePtr, depthValues);
				//_mm_stream_ps(DepthBufferTilePtr, depthValues);
				
				//Change depth by adding the X increment.
				valuesX = _mm_add_ps(valuesX, xIncrements);

				//Increment current pixel position.
				DepthBufferTilePtr += 4;

				//Update the edge acculumator every time the column changes. Add edge A coefficient.
				edgesAcumulatorForPoint1 = _mm_add_epi32(edgesAcumulatorForPoint1, convexhullEdgeAIncrements);
				edgesAcumulatorForPoint2 = _mm_add_epi32(edgesAcumulatorForPoint2, convexhullEdgeAIncrements);
				edgesAcumulatorForPoint3 = _mm_add_epi32(edgesAcumulatorForPoint3, convexhullEdgeAIncrements);
				edgesAcumulatorForPoint4 = _mm_add_epi32(edgesAcumulatorForPoint4, convexhullEdgeAIncrements);
				
			}

			//Change depth by adding the y increment. Add edge B coefficient.
			valueY += yIncrement;

			valuesX = _mm_set1_ps(initialDepth + valueY);
			valuesX = _mm_add_ps(valuesX, xIncrementsMultiplier);

			//Update the edge accumulators everytime the row changes.
			edgeAccumulatorY = _mm_add_epi32(edgeAccumulatorY, convexhullEdgeB);

			edgesAcumulatorForPoint1 = _mm_add_epi32(initialEdgeValues, edgeAccumulatorY);
			edgesAcumulatorForPoint2 = _mm_add_epi32(edgesAcumulatorForPoint1, convexhullEdgeA);
			edgesAcumulatorForPoint3 = _mm_add_epi32(edgesAcumulatorForPoint2, convexhullEdgeA);
			edgesAcumulatorForPoint4 = _mm_add_epi32(edgesAcumulatorForPoint3, convexhullEdgeA);
		}
	}
	
		}//End of OpenMP parallel scope
}

inline float depthAtPoint(const float depthPoint1, const float depthPoint2, const float depthPoint3, const int x1, const int x2, const int y1, const int y2, const int posx, const int posy) 
{
	//Interpolate the depth based on three extreme points
	//  depthP1 ________ Depth P2
	//         |        |
	//         |        |
	//         |________|
	//  depthP3


	 return depthPoint1 + (depthPoint2-depthPoint1)*((float) (posx - x1) / (float)(x2-x1)) + (depthPoint3 - depthPoint1)*((float) (posy - y1) / (float)(y2-y1));
}


bool TiledRasterizer::checkBoundingBoxVisibility(const int x1, const int x2,
												 const int y1, const int y2, 
												 const float depth) 
{

	int convexHullBoundingBox[2][2];

	convexHullBoundingBox[0][0] = x1;
	convexHullBoundingBox[0][1] = y1;

	convexHullBoundingBox[1][0] = x2;
	convexHullBoundingBox[1][1] = y2;

	int xTileStart, xTileEnd;
	int yTileStart, yTileEnd;


	testTiles.clear();
	
	//Get the list of tiles that are either inside the bounding box or intersecting its limits.
	getListOfPotentialTilesInsideBoundingBox(testTiles, convexHullBoundingBox, depthBuffer->tiles, Tile::size, depthBuffer->width);

	Tile *currentTile;

	//Iterate every tile to find a non pixel level solution.
	for( unsigned int c = 0 ; c < testTiles.size() ; c++ ) 
	{
		currentTile = testTiles[c];

		//If tile is not initialized then there is no then it is visible.
		if(currentTile->status == Tile::NotInitialized )
			return true;
	}
	
	for( unsigned int c = 0 ; c < testTiles.size() ; c++ ) 
	{
		currentTile = testTiles[c];

		xTileStart = clampTo(x1, currentTile->extremePoints[0][0],currentTile->extremePoints[2][0]);
		xTileEnd   = clampTo(x2, currentTile->extremePoints[0][0],currentTile->extremePoints[2][0]);

		yTileStart = clampTo(y1, currentTile->extremePoints[0][1],currentTile->extremePoints[2][1]);
		yTileEnd   = clampTo(y2, currentTile->extremePoints[0][1],currentTile->extremePoints[2][1]);
			
		

			if( currentTile->status == Tile::CompletelyCoveredNotRasterized )
			{

				//If the rect is exactly the same size and it is in the same position as the tile, use the minimum depth.
				if( x1 == xTileStart && x2 == xTileEnd	&& 
					y1 == yTileStart && y2 == yTileEnd)
				{
					
					if(depth <= currentTile->minDepth)
						return true;
			
				}else
				{
					float tileDepthAtPoint;

					//Get the depth at both the topleft and bottomright conerns of the occludee Rect and check if one of them is in front of the plane formed by the covered tile.
					tileDepthAtPoint = depthAtPoint( currentTile->depthPoints[0], currentTile->depthPoints[1], 
											     currentTile->depthPoints[3],
											     x1, x2, y1, y2, xTileStart, yTileStart);

					//If occludee rect is closer to the viewpoint, then it is visible.
					if ( depth < tileDepthAtPoint)
						return true;

					tileDepthAtPoint = depthAtPoint( currentTile->depthPoints[0], currentTile->depthPoints[1], 
											      currentTile->depthPoints[3],
											      x1, x2, y1, y2, xTileEnd, yTileEnd);

					//If occludee rect is closer to the viewpoint, then it is visible.
					if ( depth < tileDepthAtPoint)
							return true;

				}
			
			}

			//If cannot resolve the occlusion by tile, go to pixel level test.
			//For every pixel inside the bounding box test the depth against the one stored in the depth buffer.
			if( currentTile->status == Tile::Rasterized) 
			{

				for(int y = yTileStart ; y < yTileEnd; y++ ) 
				{
					//Optimized for linear depthbuffer access.
					float *depthPointer = &(depthBuffer->buffer[ getBufferPositionOffset(xTileStart, y, Tile::size,depthBuffer->width)]);

					for(int x = xTileStart ; x < xTileEnd; x++ ) 
					{
						//If at least one depth map point is behind the occludee depth, then it is considered visible.
						if( depth <= *depthPointer ){
								return true;
						}

						depthPointer++;
					}
				}
			}
		}
			
	
	return false;
}


//Rasterizes the fully covered and not rasterized tiles just for debugging.
void TiledRasterizer::tempRasterFullyCoveredNotRasterized()
{
	
	
	for( unsigned int c = 0 ; c <  depthBuffer->tiles.size() ; c++ ) 
	{

			Tile  *currentTile  = depthBuffer->tiles[c];
		
			int x1 = currentTile->extremePoints[0][0];
			int x2 = currentTile->extremePoints[2][0];
			int y1 = currentTile->extremePoints[0][1];
			int y2 = currentTile->extremePoints[2][1];

			if (currentTile->status == Tile::CompletelyCoveredNotRasterized )
			{
						
					const float xIncrement  = ( currentTile->depthPoints[1] - currentTile->depthPoints[0]) / (Tile::size - 1);
					const float yIncrement  = ( currentTile->depthPoints[3] - currentTile->depthPoints[0]) / (Tile::size - 1);


					float valueX = currentTile->depthPoints[0];
					float valueY = 0.0f;
						
					for( int y = y1 ; y <= y2 ; y++ )
					{
						for( int x = x1 ; x <= x2; x++ )
						{

							//if( x <= x1 + (x2 - x1)/2 && y <= y1 + (y2 - y1)/2 )
							//	value = currentTile->depthPoints[0];
							//
							//if( x <= x1 + (x2 - x1)/2 && y >  y1 + (y2 - y1)/2 )
							//	value = currentTile->depthPoints[3];

							//if( x > x1 + (x2 - x1)/2 && y <=  y1 + (y2 - y1)/2 )
							//	value = currentTile->depthPoints[1];

							//if( x > x1 + (x2 - x1)/2 && y >  y1 + (y2 - y1)/2 )
							//	value = currentTile->depthPoints[2];

							depthBuffer->setValue(x, y, valueX);

							//if( x == x1 || y == y1  || ((x - x1) == (y-y1)) || ((x2 -x) == (y-y1)) )
							//{
							//	depthBuffer->setValue(x, y, 0.0f);
							//}
							valueX += xIncrement;
						}
							
						valueY += yIncrement;
						valueX = currentTile->depthPoints[0] + valueY;
					}
			}
	}
}
