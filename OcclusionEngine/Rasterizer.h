/** ******************************************************************************* **
 * GIGC - Grupo de Investigación de Gráficos por Computadora - UTN FRBA - Argentina
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *  **
 * <copyright> copyright (c) 2012 </copyright>	
 * <autor> Leandro Roberto Barbagallo </autor>
 * <license href="http://gigc.codeplex.com/license/">Ms-PL</license>
 ********************************************************************************** */

#pragma once
#include "DepthBuffer.h"

class Rasterizer
{
public:
	
	//Creates a Rasterizer over a depth buffer.
	Rasterizer(DepthBuffer *depthBuffer);

	//Rasterizes a triangle with a fixed depth.
	void rasterizeTriangle(const int x1, const int y1,
						   const int x2, const int y2,
						   const int x3, const int y3,
						   const float depthP1, 
						   const float depthP2, 
						   const float depthP3);

	~Rasterizer(void);

private:

	DepthBuffer *depthBuffer;
};

