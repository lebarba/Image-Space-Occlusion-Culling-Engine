#include "stdafx.h"
#include "OcclusionEngineLib.h"
#include "OcclusionEngine.h"

DllExport OcclusionHandle APIENTRY InitializeOcclusionEngineDefault(int width, int height)
{
	OcclusionEngineOptions options;
	options.drawAllTiles = false; //Do not draw covered tiles by default.
	options.engineMode = Optimized;
	options.numberOfThreads = 0; //Automatically assigned.
	options.tileSize = 16;		//Default Tile Size;
	return new OcclusionEngine(width, height, options);
}

DllExport OcclusionHandle APIENTRY InitializeOcclusionEngine(int width, int height, OcclusionEngineOptions options)
{
	return new OcclusionEngine(width, height, options);
}

DllExport bool APIENTRY  addOccluders(OcclusionHandle handle, const OccluderData occludersData[], const int numberOfOccluders) 
 {
	 return handle->addOccluders(occludersData, numberOfOccluders);
 }

DllExport bool APIENTRY testOccludeeVisibility(OcclusionHandle handle,const OccludeeData occludee)
 {
	 return handle->testOccludeeVisibility(occludee);
 }

DllExport float APIENTRY getDepthBufferPixel(OcclusionHandle handle,const int x, const int y)
 {
	 return handle->getDepthBufferPixel(x, y);
 }

DllExport void APIENTRY clearOcclusionEngine(OcclusionHandle handle)
{
	return handle->clear();
}

DllExport void APIENTRY disposeOcclusionEngine(OcclusionHandle handle)
{
	return handle->dispose();
}
