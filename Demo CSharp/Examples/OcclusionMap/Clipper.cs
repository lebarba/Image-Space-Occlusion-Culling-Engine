using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX;

namespace Examples.OcclusionMap
{
    /// <summary>
    /// http://wwwx.cs.unc.edu/~sud/courses/236/a5/softgl_homoclip_smooth.cpp
    /// </summary>
    public class Clipper
    {

        // the clipping planes
        enum Planes { LEFT, RIGHT, BOTTOM, TOP, NEAR, FAR };


        /* finds the intersection point of polygon edge(v1,v2) agains clippingPlane
           returns new point in P, also interpolates color C of 2 vertices (C1, C2)
        */
        private static Vector4 intersect(Vector4 v1, Vector4 v2, Planes clippingPlane)
        {
            float t = 0;
            Vector4 P = new Vector4();
            // find the parameter of intersection
            // t = (v1_w-v1_x)/((v2_x - v1_x) - (v2_w - v1_w)) for x=w (RIGHT) plane
            // ... and similar cases
            switch (clippingPlane)
            {
                case Planes.LEFT: t = (-v1.W - v1.X) / (v2.X - v1.X + v2.W - v1.W); break;
                case Planes.RIGHT: t = (v1.W - v1.X) / (v2.X - v1.X - v2.W + v1.W); break;
                case Planes.BOTTOM: t = (-v1.W - v1.Y) / (v2.Y - v1.Y + v2.W - v1.W); break;
                case Planes.TOP: t = (v1.W - v1.Y) / (v2.Y - v1.Y - v2.W + v1.W); break;
                case Planes.NEAR: t = (-v1.W - v1.Z) / (v2.Z - v1.Z + v2.W - v1.W); break;
                case Planes.FAR: t = (v1.W - v1.Z) / (v2.Z - v1.Z - v2.W + v1.W); break;
            };
            // find the point
            P.X = v1.X + (v2.X - v1.X) * t;
            P.Y = v1.Y + (v2.Y - v1.Y) * t;
            P.Z = v1.Z + (v2.Z - v1.Z) * t;
            P.W = v1.W + (v2.W - v1.W) * t;

            return P;
        }

        /* Checks whether vertex v lies inside clipping plane
           in homogenous coords check -w < {x,y,z} < w;
        */
        private static bool inside(Vector4 v, Planes clippingPlane)
        {
            switch (clippingPlane)
            {
                case Planes.LEFT: return (v.X >= -v.W);
                case Planes.RIGHT: return (v.X <= v.W);
                case Planes.BOTTOM: return (v.Y >= -v.W);
                case Planes.TOP: return (v.Y <= v.W);
                case Planes.NEAR: return (v.Z >= -v.W);
                case Planes.FAR: return (v.Z <= v.W);
                default: return false;
            }
        }


        /* Clips a polygon in homogenous coordinates to a particular clipping plane.
           Takes in vertices of the polygon (InPts) and the clipping plane
           Puts the vertices of the clipped polygon in OutPts
           Returns number of points in clipped polygon
        */
        private static int ClipPolyToPlane(Vector4[] InPts, int NumInPts, Planes clippingPlane, ref Vector4[] OutPts)
        {
            Vector4 s, p;
            bool s_in, p_in;
            Vector4 newP;

            int i = 0; // index number of OutPts, # of vertices in OutPts = i div 4;
            int N = NumInPts;
            for (int j = 0; j < N; j++)
            {
                s = InPts[j];
                p = InPts[(j + 1) % N];

                s_in = inside(s, clippingPlane);
                p_in = inside(p, clippingPlane);
                // test if vertex is to be added to output vertices 
                if (s_in != p_in)  // edge crosses clipping plane
                {
                    // find point of intersection 
                    newP = intersect(s, p, clippingPlane);
                    // Add it to the output vertices
                    OutPts[i++] = newP;
                }
                if (p_in) // 2nd vertex is inside clipping volume, add it to output
                {
                    OutPts[i] = p;
                    i++;
                }
                // edge does not cross clipping plane and vertex outside clipping volume
                //  => do not add vertex
            }
            return i;
        }


        //----------------------------------------------------------------------------
        // Clips a 4D homogeneous polygon defined by the packed array of float InPts.
        // to the viewing frustum defined by w components of the verts. The clipped polygon is
        // put in OutPts (which must be a different location than InPts) and the number
        // of vertices in the clipped polygon is returned. InPts must have NumInPts*4
        // floats (enough to contain poly). Regular orthographic NDC clipping can be 
        // achieved by making the w coordinate of each point be 1. OutPts will be
        // allocated and return filled with the clipped polygon.
        //----------------------------------------------------------------------------
        public static int ClipPolyToFrustum_SMOOTH(Vector4[] InPts, out Vector4[] OutPts)
        {
            // each face of the frustum intersects some edge of the polygon
            //  => num of vertices in clipped polygon can increase by max 6(convex poly!)
            int NumInPts = InPts.Length;
            OutPts = new Vector4[NumInPts + 6];

            // CLIP InPts TO OutPts IN HOMOGENEOUS COORDS

            Vector4[] tempPts = new Vector4[(NumInPts + 6)];
            int i, NumOutPts;

            // init tempPts to InPts
            for (i = 0; i < NumInPts; i++) tempPts[i] = InPts[i];

            // clip against each plane, using tempPts to store intermediate result
            NumOutPts = ClipPolyToPlane(tempPts, NumInPts, Planes.LEFT, ref OutPts);
            NumOutPts = ClipPolyToPlane(OutPts, NumOutPts, Planes.RIGHT, ref tempPts);
            NumOutPts = ClipPolyToPlane(tempPts, NumOutPts, Planes.BOTTOM, ref OutPts);
            NumOutPts = ClipPolyToPlane(OutPts, NumOutPts, Planes.TOP, ref tempPts);
            NumOutPts = ClipPolyToPlane(tempPts, NumOutPts, Planes.NEAR, ref OutPts);
            NumOutPts = ClipPolyToPlane(OutPts, NumOutPts, Planes.FAR, ref tempPts);

            // tempPts has the output vertices
            for (i = 0; i < NumOutPts; i++) OutPts[i] = tempPts[i];


            return NumOutPts;
        }

    }
}
