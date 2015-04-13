/** ******************************************************************************* **
 * GIGC - Grupo de Investigación de Gráficos por Computadora - UTN FRBA - Argentina
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *  **
 * <copyright> copyright (c) 2012 </copyright>	
 * <autor> Leandro Roberto Barbagallo </autor>
 * <license href="http://gigc.codeplex.com/license/">Ms-PL</license>
 ********************************************************************************** */

#pragma once
#include "stdafx.h"

class DepthBuffer
{
public:

	DepthBuffer(const int width, const int height);
	~DepthBuffer(void);

	int width;
	int height;
	
	inline void setValue(const int x, const int y, const float depth) {
		buffer[y * width + x] = depth;
	}

	inline float getValue(const int x, const int y) const {
		return buffer[y * width + x];
	}

	void clear();

	void dispose();

private:

	ALIGNED16 float *buffer;

	bool checkBufferSize(int width, int height);
	void initiateDepthBuffer(int width, int height);

};

