#include "stdafx.h"
#include "Rasterizer.h"
#include "DepthBuffer.h"
#include "Utils.h"

Rasterizer::Rasterizer(DepthBuffer *p_DepthBuffer)
{
	depthBuffer = p_DepthBuffer;
}

Rasterizer::~Rasterizer(void)
{
}

//Given three 2D points non-aligned points returns the three edge coefficients.
void calculateEdgeCoefficients( int edges[3][3], 
								const int x1, const int y1,
								const int x2, const int y2,
								const int x3, const int y3)
{
	
	//Find edge equations for Edge 0
	edges[0][0] = y2 - y1;
	edges[0][1] = x1 - x2;
	edges[0][2] = -x1 * y2 + y1 * x2; 

	//Test if third point is in the positive side of the half plane.
	if( edges[0][0] * x3 + edges[0][1] * y3 + edges[0][2] < 0 ) 
	{
		edges[0][0] *= -1;
		edges[0][1] *= -1;
		edges[0][2] *= -1;
	}

	//Find edge equations for Edge 1
	edges[1][0] = y3 - y2;
	edges[1][1] = x2 - x3;
	edges[1][2] = -x2 * y3 + y2 * x3;

	//Test if first point is in the positive side of the half plane.
	if( edges[1][0] * x1 + edges[1][1] * y1 + edges[1][2] < 0 ) 
	{
		edges[1][0] *= -1;
		edges[1][1] *= -1;
		edges[1][2] *= -1;
	}

	//Find edge equations for Edge 2
	edges[2][0] = y1 - y3;
	edges[2][1] = x3 - x1;
	edges[2][2] = -x3 * y1 + y3 * x1;

	//Test if second point is in the positive side of the half plane.
	if( edges[2][0] * x2 + edges[2][1] * y2 + edges[2][2] < 0 ) 
	{
		edges[2][0] *= -1;
		edges[2][1] *= -1;
		edges[2][2] *= -1;
	}

}

float getTriangleArea( int edges[3][3])
{

	//Test is triangle area is zero or negative.
	return (edges[0][2] + edges[1][2] + edges[2][2])*0.5f;
}

void calculateBoundingBox(	int boundingBox[2][2],
							const int x1, const int y1,
							const int x2, const int y2,
							const int x3, const int y3,
							const int width, const int height) 
{

	//Initialize with one of the three values already and make one less comparison.

	//Minimum X.
	boundingBox[0][0] = x1;
	//Minimum Y.
	boundingBox[0][1] = y1;
	//Maximum X.
	boundingBox[1][0] = x3;
	//Maximum Y.
	boundingBox[1][1] = y3;

	
	//Find Minimum X.
	if( x2 < boundingBox[0][0])
		boundingBox[0][0] = x2;

	if( x3 < boundingBox[0][0])
		boundingBox[0][0] = x3;


	//Find Minimum Y.
	if( y2 < boundingBox[0][1])
		boundingBox[0][1] = y2;

	if( y3 < boundingBox[0][1])
		boundingBox[0][1] = y3;


	//Find Maximum X.
	if( x1 > boundingBox[1][0])
		boundingBox[1][0] = x1;

	if( x2 > boundingBox[1][0])
		boundingBox[1][0] = x2;

	
	//Find Maximum Y.
	if( y1 > boundingBox[1][1])
		boundingBox[1][1] = y1;

	if( y2 > boundingBox[1][1])
		boundingBox[1][1] = y2;

	//Clip bounding box to buffer limits.
	boundingBox[0][0] = clampTo(boundingBox[0][0], 0, width);
	boundingBox[1][0] = clampTo(boundingBox[1][0], 0, width);

	boundingBox[0][1] = clampTo(boundingBox[0][1], 0, height);
	boundingBox[1][1] = clampTo(boundingBox[1][1], 0, height);

}


inline float interpolateDepth( const int x, int y,
						const int x1, const int y1,
						const int x2, const int y2,
						const int x3, const int y3,
						const float depthP1, const float depthP2, const float depthP3, 
						float triangleArea)

{

	float u, v, w;

	float Ax, Bx, Ay, By;
	float denominator =  0.5f / triangleArea;

	Ax = (float) (x2 - x3);
	Ay = (float) (y2 - y3);
	Bx = (float) (x - x3);
	By = (float) (y - y3);
	u = denominator * ( Ax*By - Bx*Ay );

	if( u < 0.0f)
		u *= -1;


	Ax = (float) (x1 - x3);
	Ay = (float) (y1 - y3);
	Bx = (float) (x - x3);
	By = (float) (y - y3);
	v = denominator * ( Ax*By - Bx*Ay );

	if( v < 0.0f)
		v *= -1;

	
	//u + v + w is supposed to be equal to 1.
	w = 1.0f - u - v;

	return u*depthP1  + v*depthP2  + w*depthP3;
}

void Rasterizer::rasterizeTriangle( const int x1, const int y1,
									const int x2, const int y2,
									const int x3, const int y3,
									float depthP1, float depthP2, float depthP3)
{

	

	int edges[3][3]; // edges[edgeNumber][Coefficient]
	int boundingBox[2][2]; 
	
	float xtest, ytest;

	float depth;
	float triangleArea;

	//Triangle setup.
	calculateEdgeCoefficients(edges, x1, y1,  x2,  y2,  x3,  y3);


	triangleArea = getTriangleArea(edges);

	//Test if trinagle has zero area and it is pointing towards the camera (backface cull).
	if ( triangleArea > 0.0f ) {

		//Calculate triangle bounding box.
		calculateBoundingBox(boundingBox, x1, y1, x2, y2, x3, y3, depthBuffer->width -1, depthBuffer->height -1);
	
		//iterate every fragment inside the triangle bounding box.
		for( int y = boundingBox[0][1] ; y < boundingBox[1][1] ; y++ )
		{

			for( int x = boundingBox[0][0] ; x < boundingBox[1][0] ; x++ )
			{

				xtest = x + 0.5f;
				ytest = y + 0.5f;

				//Test point inside triangle.
				if ( edges[0][0] * xtest +  edges[0][1] * ytest +  edges[0][2]  > 0 &&
					 edges[1][0] * xtest +  edges[1][1] * ytest +  edges[1][2]  > 0 &&
					 edges[2][0] * xtest +  edges[2][1] * ytest +  edges[2][2]  > 0  )
				{

					//Interpolate depth based on triangle three points.
					depth = interpolateDepth(x, y, x1, y1, x2, y2, x3, y3, depthP1, depthP2, depthP3, triangleArea);

					assert( depth >= 0.0f && depth <= 1.0f);

					//Test if fragment is closer than the deph stored in the buffer.
					if( depth < depthBuffer->getValue(x,y) )
					{
						//Set the depth value.
						depthBuffer->setValue(x,y,depth);
					}
				}
			}

		}
	}

}
