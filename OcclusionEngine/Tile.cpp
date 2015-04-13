#include "StdAfx.h"
#include "Tile.h"

int Tile::size;

Tile::Tile(int pxMin, int pxMax, int pyMin, int pyMax)
{


	// Extreme values.
	//  0________1
	//  |        |
	//  |        |
	// 3|________|2

	extremePoints[0][0] = pxMin;
	extremePoints[0][1] = pyMin;

	extremePoints[1][0] = pxMax;
	extremePoints[1][1] = pyMin;

	extremePoints[2][0] = pxMax;
	extremePoints[2][1] = pyMax;

	extremePoints[3][0] = pxMin;
	extremePoints[3][1] = pyMax;

	//Set initial variables.
	reset();
}
	

Tile::~Tile(void)
{

}

void Tile::reset() 
{
	minDepth = 1.0f;

	status = previousStatus = Tile::NotInitialized;

	depthPoints[0] = 0.0f;
	depthPoints[1] = 0.0f;
	depthPoints[2] = 0.0f;
	depthPoints[3] = 0.0f;
}