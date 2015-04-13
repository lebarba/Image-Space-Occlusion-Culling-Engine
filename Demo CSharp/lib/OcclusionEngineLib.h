/** ******************************************************************************* **
 * GIGC - Grupo de Investigación de Gráficos por Computadora - UTN FRBA - Argentina
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *  **
 * <copyright> copyright (c) 2012 </copyright>	
 * <autor> Leandro Roberto Barbagallo </autor>
 * <license href="http://gigc.codeplex.com/license/">Ms-PL</license>
 ********************************************************************************** */

#pragma once

#include "stdafx.h"

//The maximum number of points per occluder.
#define MAX_POINTS_PER_OCCLUDER 4

//The maximum number of edges per occluder
#define MAX_EDGES 4

//Minimum and maximum tile sizes
#define MAX_TILE_SIZE 16
#define MIN_TILE_SIZE 4

struct OccluderPoint
{
	//Occluder vertex point in image space. Can be out of screen bounds.
	int x; 
	int y;

	float depth; // Depth value between 0 and 1 (inclusive).
};
	
struct OccluderData
{
	//List of occluder convex hull points ordered counter-clockwise.
	OccluderPoint points[MAX_POINTS_PER_OCCLUDER];
		
	//Number of occluder convex hull points.
	int numberOfPoints;
};
	
struct OccludeeAABB
{
	int xMin;
	int xMax;
	int yMin;
	int yMax;
};
	
struct OccludeeData
{
	OccludeeAABB boundingBox;
	float depth;
};

enum EOcclusionEngineMode
{
	NormalRasterization = 1,
	Optimized = 2
};

struct OcclusionEngineOptions
{
	EOcclusionEngineMode engineMode;	//The type of occlusion engine to initialize.
	int tileSize;						//Non zero positive number multiple of 4 between  MIN_TILE_SIZE and MAX_TILE_SIZE
	int numberOfThreads;				//Number of threads to create. Value 0 for automatically assign of number of threads.
	bool drawAllTiles;					//Activate drawing of completely covered tiles.
};

struct IOcclusionCulling 
{
	virtual bool addOccluders(const OccluderData occludersData[], const int numberOfOccluders) = 0;
	virtual bool testOccludeeVisibility( OccludeeData occludee) = 0;
	virtual float getDepthBufferPixel(const int x, const int y) = 0;
	virtual void clear() = 0;
	virtual void dispose() = 0;
};




typedef IOcclusionCulling* OcclusionHandle;

EXTERN_C DllExport OcclusionHandle WINAPI InitializeOcclusionEngineDefault(int width, int height);
EXTERN_C DllExport OcclusionHandle WINAPI InitializeOcclusionEngine(int width, int height, OcclusionEngineOptions options);

EXTERN_C DllExport bool WINAPI addOccluders(OcclusionHandle handle, const OccluderData occludersData[], const int numberOfOccluders);
EXTERN_C DllExport bool WINAPI testOccludeeVisibility(OcclusionHandle handle,const OccludeeData occludee);
EXTERN_C DllExport float WINAPI getDepthBufferPixel(OcclusionHandle handle,const int x, const int y);
EXTERN_C DllExport void WINAPI clearOcclusionEngine(OcclusionHandle handle);
EXTERN_C DllExport void WINAPI disposeOcclusionEngine(OcclusionHandle handle);
