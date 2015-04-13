#include "StdAfx.h"
#include "TiledDepthBuffer.h"

TiledDepthBuffer::TiledDepthBuffer(const int pwidth, const int pheight, int ptileSize)
{

	width = pwidth;
	height = pheight;

	tileSize = ptileSize;

	//Check the buffer size before creating it.
	checkBufferSize(width, height, ptileSize);

	//Allocate space and fill with values.
	initiateDepthBuffer(width, height, ptileSize);
}

TiledDepthBuffer::~TiledDepthBuffer()
{
	dispose();
}


void TiledDepthBuffer::clear()
{
	for( unsigned int c = 0 ; c < tiles.size() ; c++)
		tiles[c]->reset();

	for( int i = 0 ; i < width*height ; i++ )
		buffer[i] = 1.0f;
}

void TiledDepthBuffer::dispose()
{
	if( buffer != NULL) {
		_aligned_free(buffer);
		buffer = NULL;
	}
}

	
bool TiledDepthBuffer::checkBufferSize(int &pwidth, int &pheight, int ptileSize) 
{
	
	if( pwidth <= 0 || pheight <= 0) 
		return false;
	
	//Check if the width and height are multiple of the tile size.
	//Adjust size to be a multiple of the tile size.
	if( pwidth % ptileSize != 0 )
		pwidth = ((int) (pwidth/ ptileSize) + 1) * ptileSize;

	if( pheight % ptileSize != 0 )
		pheight = ((int) (pheight/ ptileSize) + 1) * ptileSize;

	return true;
}

void TiledDepthBuffer::initiateDepthBuffer(int pwidth, int pheight, int ptileSize) 
{
	int totalNumberOfTiles;

	//Get the total number of tiles of the whole screen.
	totalNumberOfTiles = (pwidth*pheight) / (ptileSize * ptileSize);

	//Assign the tile size.
	Tile::size = ptileSize;

	//Reserve memory for the total amount of tiles.
	tiles.reserve(totalNumberOfTiles);

	//Create an aligned buffer.
	buffer = (float *) (_aligned_malloc(pwidth*pheight * sizeof(float), 16));
	
	//Clear the depth buffer with the initial value.
	for( int i = 0 ; i < pwidth*pheight ; i++ )
		buffer[i] = 1.0f;


	//Create tiles and assign extreme coordinates to each one.
	for( int y = 0 ; y < pheight ; y += ptileSize )
	{
		for( int x = 0 ; x < pwidth ; x += ptileSize )
		{
			tiles.push_back( new Tile(x, x + ptileSize -1 , y, y + ptileSize -1));
		}
	}
}