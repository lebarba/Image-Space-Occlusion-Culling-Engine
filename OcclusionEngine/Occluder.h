/** ******************************************************************************* **
 * GIGC - Grupo de Investigación de Gráficos por Computadora - UTN FRBA - Argentina
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *  **
 * <copyright> copyright (c) 2012 </copyright>	
 * <autor> Leandro Roberto Barbagallo </autor>
 * <license href="http://gigc.codeplex.com/license/">Ms-PL</license>
 ********************************************************************************** */

#pragma once
#include "OcclusionEngineLib.h"

class Occluder
{
public:

	
	struct OccluderPoint
	{
		//Occluder vertex point in image space. Make sure it is within screen bounds.
		int x; 
		int y;

		float depth;
	};
	
	Occluder(const OccluderPoint points[], const int numberOfPoints);
	~Occluder();


	//List of occluder convex hull points ordered clockwise. Uses Triangle Fan ordering taking first point as central vertex.
	OccluderPoint points[MAX_POINTS_PER_OCCLUDER];
	
	//Number of occluder convex hull points.
	int numberOfPoints;


};

