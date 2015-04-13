#include "StdAfx.h"
#include "OcclusionEngine.h"
#include "Utils.h"



OcclusionEngine::OcclusionEngine(int pBufferWidth, int pBufferHeight, OcclusionEngineOptions options)
{
	int tileSize = clampTo(options.tileSize, MIN_TILE_SIZE, MAX_TILE_SIZE );

	if( tileSize % MIN_TILE_SIZE != 0 )
		tileSize = ((int)(tileSize / MIN_TILE_SIZE) + 1) * MIN_TILE_SIZE;

	drawAllTiles = options.drawAllTiles;
	engineMode = (EOcclusionEngineMode)options.engineMode;
	
	depthBuffer = NULL;
	tiledDepthBuffer = NULL;

	bufferWidth = pBufferWidth;
	bufferHeight = pBufferHeight;

	if( engineMode == NormalRasterization ) 
	{
		//Create the depth buffer.
		depthBuffer = new DepthBuffer(bufferWidth, bufferHeight);

		//Create a rasterizer.
		rasterizer = new Rasterizer(depthBuffer);
	}

	if( engineMode == Optimized ) 
	{
		tiledDepthBuffer = new TiledDepthBuffer(bufferWidth, bufferHeight, tileSize);
		tiledRasterizer = new TiledRasterizer(tiledDepthBuffer);

		tiledRasterizer->numberOfThreads = options.numberOfThreads;
	}
}

OcclusionEngine::~OcclusionEngine(void)
{
	dispose();
}

void OcclusionEngine::dispose()
{
	if( depthBuffer != NULL) {
		depthBuffer->dispose();
		depthBuffer = NULL;
	}

	if( tiledDepthBuffer != NULL) {
		tiledDepthBuffer->dispose();
		tiledDepthBuffer = NULL;
	}

}

bool OcclusionEngine::addOccluders(const OccluderData occludersData[], const int numberOfOccluders)
{

    if( occludersData == NULL)
		return false;

    if( numberOfOccluders <= 0)
		return false;

	std::vector<Occluder> occluders;

	occluders.reserve(numberOfOccluders);
	for( int occ = 0 ; occ < numberOfOccluders ; occ++) {

		Occluder::OccluderPoint points[MAX_POINTS_PER_OCCLUDER] = {0};

		for ( int i = 0 ;  i < occludersData[occ].numberOfPoints ; i++ )
		{
			points[i].x = occludersData[occ].points[i].x;
			points[i].y = occludersData[occ].points[i].y;
			points[i].depth = occludersData[occ].points[i].depth;
		}
		
		occluders.push_back(Occluder( points, occludersData[occ].numberOfPoints));
	}

	

	addOccluders((Occluder*)(&occluders.at(0)), numberOfOccluders);

	
	
	return true;
}


void OcclusionEngine::addOccluders( const Occluder occluderArray[], int numberOfOccluders )
{


	if( engineMode == NormalRasterization ) 
	{
		for( int i = 0 ; i < numberOfOccluders ; i++ ) 
		{

			const Occluder *currentOccluder;

			currentOccluder = &(occluderArray[i]);
			int triangleIndexOffset;

			//Create triangles based on occluder triangle strip.
			for( int t = 0 ; t < currentOccluder->numberOfPoints - 2 ; t++ ) {
			
				triangleIndexOffset = t + 1;

				//Rasterize every triangle that forms the occluder convex hull.
				rasterizer->rasterizeTriangle(  currentOccluder->points[0].x, currentOccluder->points[0].y,
												currentOccluder->points[triangleIndexOffset].x, currentOccluder->points[triangleIndexOffset].y,
												currentOccluder->points[triangleIndexOffset + 1].x, currentOccluder->points[triangleIndexOffset + 1].y,
												clampTo(currentOccluder->points[0].depth,0.0f,1.0f), //Clamp depth between 0 and 1 inclusive.
												clampTo(currentOccluder->points[triangleIndexOffset].depth,0.0f,1.0f),
												clampTo(currentOccluder->points[triangleIndexOffset + 1].depth,0.0f,1.0f) );

			}
		}
	}

	if( engineMode == Optimized ) 
	{

		int totalTilesDefferedFromRasterization = 0;

		for( int i = 0 ; i < numberOfOccluders ; i++ ) 
		{
			tiledRasterizer->rasterizeConvexHull(occluderArray[i]);

			totalTilesDefferedFromRasterization += tiledRasterizer->tilesDefferredFromRasterization;
		}

		if( drawAllTiles )
			tiledRasterizer->tempRasterFullyCoveredNotRasterized();

		//printf("Tiles avoided: %d\n", totalTilesDefferedFromRasterization);
	}

	
	
}

void adjustBoundingBox( Occludee::OccludeeAABB &boundingBox, const int width, const int height )
{

	boundingBox.xMin = clampTo(boundingBox.xMin, 0,  width - 1);
	boundingBox.yMin = clampTo(boundingBox.yMin, 0,  height - 1);

	boundingBox.xMax = clampTo(boundingBox.xMax, 0,  width - 1);
	boundingBox.yMax = clampTo(boundingBox.yMax, 0,  height- 1);
	
	if( boundingBox.xMin > boundingBox.xMax)
		boundingBox.xMin = boundingBox.xMax;

	if( boundingBox.yMin > boundingBox.yMax)
		boundingBox.yMin = boundingBox.yMax;
	
}

bool OcclusionEngine::testOccludeeVisibility( Occludee occludee )
{

	float depth = occludee.depth;

	//Clip occludee bounding box to buffer size limits.
	adjustBoundingBox( occludee.boundingBox, bufferWidth, bufferHeight);

	if( engineMode == Optimized ) 
	{


		//If one pixel is closer to the viewer, then it is not completelly occluded.
		return tiledRasterizer->checkBoundingBoxVisibility(occludee.boundingBox.xMin,
															occludee.boundingBox.xMax, 
															occludee.boundingBox.yMin, 
															occludee.boundingBox.yMax,
															occludee.depth);
	}

	if( engineMode == NormalRasterization ) 
	{
		//For every pixel inside the bounding box test the depth against the one stored in the depth buffer.
		for(int y = occludee.boundingBox.yMin ; y < occludee.boundingBox.yMax; y++ ) 
		{
			for(int x = occludee.boundingBox.xMin ; x < occludee.boundingBox.xMax; x++ ) 
			{
				//If one pixel is closer to the viewer, then it is not completelly occluded.
				if( depth <= depthBuffer->getValue(x,y) )
					return true;

			}
		}

	}

	//All pixels inside the occludee bounding box are behind the occludeers.
	return false;
}


float OcclusionEngine::getDepthBufferPixel(const int x, const int y)
{
	int clampedX, clampedY;

	clampedX = clampTo(x, 0, bufferWidth - 1);
	clampedY = clampTo(y, 0, bufferHeight - 1);

	if( engineMode == Optimized ) 
	{
		return tiledDepthBuffer->getValue(clampedX, clampedY);
	}

	if( engineMode == NormalRasterization ) 
	{
		return depthBuffer->getValue(clampedX, clampedY);
	}

	return 0;
}

bool OcclusionEngine::testOccludeeVisibility(const OccludeeData occludee)
{

	Occludee::OccludeeAABB bbox;
	bbox.xMax = occludee.boundingBox.xMax;
	bbox.xMin = occludee.boundingBox.xMin;
	bbox.yMax = occludee.boundingBox.yMax;
	bbox.yMin = occludee.boundingBox.yMin;

	return testOccludeeVisibility( Occludee( bbox, occludee.depth) );
}

void OcclusionEngine::clear()
{

	if( engineMode == NormalRasterization ) 
	{
		depthBuffer->clear();
	}

	if( engineMode == Optimized ) 
	{
		//Could be optimized by just setting tile status and not clearing the actual depth values.
		tiledDepthBuffer->clear();
	}
}

DepthBuffer *OcclusionEngine::getDepthBuffer() 
{
	if( engineMode == NormalRasterization ) 
	{
		return depthBuffer;
	}
	
	return NULL;
}
