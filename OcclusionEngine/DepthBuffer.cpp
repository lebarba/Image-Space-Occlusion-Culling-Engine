#include "StdAfx.h"
#include "DepthBuffer.h"

DepthBuffer::DepthBuffer(int p_width, int p_height)
{

	width = p_width;
	height = p_height;

	//Check the buffer size before creating it.
	checkBufferSize(p_width, p_height);

	//Allocate space and fill with values.
	initiateDepthBuffer(p_width, p_height);

}

DepthBuffer::~DepthBuffer(void)
{
		dispose();
}

void DepthBuffer::dispose()
{
	if( buffer != NULL) {
		delete []  buffer;
		buffer = NULL;
	}
}

//Checks if the buffer dimensios are valid.
bool DepthBuffer::checkBufferSize(const int width, const int height)
{

	if( width <= 0 || height <= 0) 
		return false;


	return true;
}

void DepthBuffer::initiateDepthBuffer(int width, int height) {
	
	
	//Allocate space for the buffer.
	buffer = new float[width*height];
	
	//Fill it with zero value;
	clear();
}

void DepthBuffer::clear() 
{
	//Fill with initial value;
	for(int i = 0; i < width*height; i++)
		buffer[i] = 1.0f;
}
