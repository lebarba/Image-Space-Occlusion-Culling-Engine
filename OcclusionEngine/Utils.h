/** ******************************************************************************* **
 * GIGC - Grupo de Investigación de Gráficos por Computadora - UTN FRBA - Argentina
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *  **
 * <copyright> copyright (c) 2012 </copyright>	
 * <autor> Leandro Roberto Barbagallo </autor>
 * <license href="http://gigc.codeplex.com/license/">Ms-PL</license>
 ********************************************************************************** */
#pragma once
#include "stdafx.h"

inline int clampTo( const int value, const int min, const int max)
{
	if( value < min)
		return min;

	if (value > max)
		return max;
	
	return value;
}

inline float clampTo( const float value, const float min, const float max)
{
	if( value < min)
		return min;

	if (value > max)
		return max;
	
	return value;
}


inline float lerp(const float a, const float b, const float t) 
{
	return a + t*(b-a);
}