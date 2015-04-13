/** ******************************************************************************* **
 * GIGC - Grupo de Investigación de Gráficos por Computadora - UTN FRBA - Argentina
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *  **
 * <copyright> copyright (c) 2012 </copyright>	
 * <autor> Leandro Roberto Barbagallo </autor>
 * <license href="http://gigc.codeplex.com/license/">Ms-PL</license>
 ********************************************************************************** */

#pragma once


//Represents an Occluder
class  Occludee
{
public:
	
	struct OccludeeAABB
	{
		int xMin;
		int xMax;
		int yMin;
		int yMax;
	};

	float depth;

	Occludee(OccludeeAABB ScreenAlignedboundingBox, float depth);
	~Occludee(void);

	OccludeeAABB boundingBox;

private:

};

