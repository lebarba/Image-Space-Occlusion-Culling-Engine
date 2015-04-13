/** ******************************************************************************* **
 * GIGC - Grupo de Investigación de Gráficos por Computadora - UTN FRBA - Argentina
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *  **
 * <copyright> copyright (c) 2012 </copyright>	
 * <autor> Leandro Roberto Barbagallo </autor>
 * <license href="http://gigc.codeplex.com/license/">Ms-PL</license>
 ********************************************************************************** */

#pragma once

#include "Occluder.h"
#include "Occludee.h"
#include "Rasterizer.h"
#include "TiledRasterizer.h"
#include "TiledDepthBuffer.h"
#include "OcclusionEngineLib.h"


class OcclusionEngine : public IOcclusionCulling
{
public:
	

	//Initialize the Occlusion Map Engine with a specified screen size.
	OcclusionEngine(const int bufferSizeX, const int bufferSizeY,  OcclusionEngineOptions options);

	//Set a list of occluders.
	void addOccluders( const Occluder occluders[], int numberOfOccluders);

	bool addOccluders(const OccluderData occludersData[], const int numberOfOccluders);
	
	//test if an occludee is visible or not.
	bool testOccludeeVisibility(Occludee occludee);

	bool testOccludeeVisibility(OccludeeData occludee);

	float getDepthBufferPixel(const int x, const int y);

	//Clear the Occlusion Engine buffer.
	void clear();

	void dumpToBitmap();

	DepthBuffer *getDepthBuffer();

	void dispose();

	~OcclusionEngine(void);

private:
	
	bool checkBufferSize(const int width, const int height);
	void InitiateDepthBuffer(const int width, const int height);

	DepthBuffer *depthBuffer;
	Rasterizer *rasterizer;

	int bufferWidth, bufferHeight;

	EOcclusionEngineMode engineMode;

	TiledDepthBuffer *tiledDepthBuffer;
	TiledRasterizer *tiledRasterizer;

	bool drawAllTiles;

	
};

