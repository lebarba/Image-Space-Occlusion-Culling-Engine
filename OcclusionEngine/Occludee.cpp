#include "StdAfx.h"
#include "Occludee.h"


Occludee::Occludee(const OccludeeAABB bbox, float p_depth)
{
	boundingBox = bbox;
	depth = p_depth;
}


Occludee::~Occludee(void)
{
}
