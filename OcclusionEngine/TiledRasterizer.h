/** ******************************************************************************* **
 * GIGC - Grupo de Investigación de Gráficos por Computadora - UTN FRBA - Argentina
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *  **
 * <copyright> copyright (c) 2012 </copyright>	
 * <autor> Leandro Roberto Barbagallo </autor>
 * <license href="http://gigc.codeplex.com/license/">Ms-PL</license>
 ********************************************************************************** */

#pragma once
#include "TiledDepthBuffer.h"
#include "Occluder.h"

class TiledRasterizer
{
public:
	
	//Creates a Rasterizer over a depth buffer.
	TiledRasterizer(TiledDepthBuffer *depthBuffer);

	//Rasterizes a triangle with a fixed depth.
	void rasterizeConvexHull(const Occluder);

	~TiledRasterizer(void);


	bool checkBoundingBoxVisibility(const int x1, const int x2, const int y1, const int y2, const float depth);

	void tempRasterFullyCoveredNotRasterized();

	int numberOfThreads;

	int tilesDefferredFromRasterization;

private:

	TiledDepthBuffer *depthBuffer;

	//List of potential tiles to render.
	std::vector<Tile*> completelyInsideOccluderTiles;
	std::vector<Tile*> borderOccluderTiles;

	//Potential tiles inside the ocludee bounding box.
	std::vector<Tile*> testTiles;
};
