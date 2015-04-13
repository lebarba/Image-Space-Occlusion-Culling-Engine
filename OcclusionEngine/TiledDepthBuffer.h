/** ******************************************************************************* **
 * GIGC - Grupo de Investigación de Gráficos por Computadora - UTN FRBA - Argentina
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *  **
 * <copyright> copyright (c) 2012 </copyright>	
 * <autor> Leandro Roberto Barbagallo </autor>
 * <license href="http://gigc.codeplex.com/license/">Ms-PL</license>
 ********************************************************************************** */

#pragma once
#include "Tile.h"

class TiledDepthBuffer
{

public:

	TiledDepthBuffer(const int width, const int height, int tileSize);
	~TiledDepthBuffer(void);

	int width;
	int height;
	
	int tileSize;

	void clear();
	void dispose();

	inline void setValue(const int x, const int y, const float depth) {
		//TODO: Find a better way to simplify and optimize this!
		int tileNum = (x / tileSize) + (y/tileSize) * (width/tileSize);
		int xTileLocal = x % tileSize; 
		int yTileLocal = y % tileSize;

		buffer[tileNum*tileSize*tileSize + yTileLocal*tileSize + xTileLocal] = depth;
	}

	inline float getValue(const int x, const int y) const {
		//TODO: Find a better way to simplify and optimize this!
		int tileNum = (x / tileSize) + (y/tileSize) * (width/tileSize);
		int xTileLocal = x % tileSize; 
		int yTileLocal = y % tileSize;

		return buffer[tileNum*tileSize*tileSize + yTileLocal*tileSize + xTileLocal];
	}

	std::vector<Tile*> tiles;

	ALIGNED16 float *buffer;

private:

	bool checkBufferSize(int &width, int &height, int tileSize);
	void initiateDepthBuffer(int width, int height, int tileSize);
};

