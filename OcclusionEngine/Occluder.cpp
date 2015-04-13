#include "StdAfx.h"
#include "Occluder.h"


Occluder::Occluder(const OccluderPoint p_points[MAX_POINTS_PER_OCCLUDER], const int p_numberOfPoints )
{
	numberOfPoints = p_numberOfPoints;
	
	for( int c = 0 ; c < MAX_POINTS_PER_OCCLUDER ; c++ ) {
		points[c].x = p_points[c].x;
		points[c].y = p_points[c].y;
		points[c].depth = p_points[c].depth;
	}
}


Occluder::~Occluder(void)
{

}

