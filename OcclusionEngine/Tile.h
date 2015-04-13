/** ******************************************************************************* **
 * GIGC - Grupo de Investigación de Gráficos por Computadora - UTN FRBA - Argentina
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *  **
 * <copyright> copyright (c) 2012 </copyright>	
 * <autor> Leandro Roberto Barbagallo </autor>
 * <license href="http://gigc.codeplex.com/license/">Ms-PL</license>
 ********************************************************************************** */

#pragma once

class Tile
{
public:

	enum ETileStatus
	{
		NotInitialized,
		CompletelyCoveredNotRasterized,
		Rasterized
	};

	//The tile size.
	static int size;

	//The four coner points.
	int extremePoints[4][2]; 

	//The depth of the extreme points.
	float depthPoints[4];

	//float maxDepth;
	float minDepth;
	
	ETileStatus status;
	ETileStatus previousStatus;
	
	Tile(int xMin, int xMax, int yMin, int yMax);
	~Tile(void);

	void reset();
};

