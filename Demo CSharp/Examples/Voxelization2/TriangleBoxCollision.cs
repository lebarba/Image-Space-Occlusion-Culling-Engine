using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX;
using TgcViewer.Utils.TgcGeometry;

namespace Examples.Voxelization2
{
    public class TriangleBoxCollision
    {

        /// <summary>
        /// /// Indica si un Triangulo colisiona con un BoundingBox
        /// Basado en:
        /// http://fileadmin.cs.lth.se/cs/Personal/Tomas_Akenine-Moller/code/tribox3.txt
        /// </summary>
        /// <param name="vert0">Vertice 0 del triángulo</param>
        /// <param name="vert1">Vertice 1 del triángulo</param>
        /// <param name="vert2">Vertice 2 del triángulo</param>
        /// <param name="aabb">BoundingBox</param>
        /// <returns>True si hay colisión.</returns>
        public static bool testTriangleAABB(Vector3d vert0, Vector3d vert1, Vector3d vert2, TgcBoundingBoxd aabb)
        {
            /*   use separating axis theorem to test overlap between triangle and box need to test for overlap in these directions:
            *    1) the {x,y,z}-directions (actually, since we use the AABB of the triangle we do not even need to test these)
            *    2) normal of the triangle
            *    3) crossproduct(edge from tri, {x,y,z}-directin) this gives 3x3=9 more tests 
            */

            //Compute box center and boxhalfsize (if not already given in that format)
            Vector3d boxcenter = aabb.boxcenter;
            Vector3d boxhalfsize = aabb.boxhalfsize;

            //Aux vars
            Vector3d[] triverts = new Vector3d[3] { vert0, vert1, vert2 };
            double min, max, p0, p1, p2, rad, fex, fey, fez;

            //move everything so that the boxcenter is in (0,0,0)
            Vector3d v0 = triverts[0] - boxcenter;
            Vector3d v1 = triverts[1] - boxcenter;
            Vector3d v2 = triverts[2] - boxcenter;

            //compute triangle edges
            Vector3d e0 = v1 - v0; //tri edge 0
            Vector3d e1 = v2 - v1; //tri edge 1
            Vector3d e2 = v0 - v2; //tri edge 2


            //Bullet 3:
            //test the 9 tests first (this was faster)

            //edge 0
            fex = Abs(e0.X);
            fey = Abs(e0.Y);
            fez = Abs(e0.Z);

            //AXISTEST_X01
            p0 = e0.Z * v0.Y - e0.Y * v0.Z;
            p2 = e0.Z * v2.Y - e0.Y * v2.Z;
            if (p0 < p2) { min = p0; max = p2; } else { min = p2; max = p0; }
            rad = fez * boxhalfsize.Y + fey * boxhalfsize.Z;
            if (min > rad || max < -rad) return false;

            //AXISTEST_Y02
            p0 = -e0.Z * v0.X + e0.X * v0.Z;
            p2 = -e0.Z * v2.X + e0.X * v2.Z;
            if (p0 < p2) { min = p0; max = p2; } else { min = p2; max = p0; }
            rad = fez * boxhalfsize.X + fex * boxhalfsize.Z;
            if (min > rad || max < -rad) return false;

            //AXISTEST_Z12
            p1 = e0.Y * v1.X - e0.X * v1.Y;
            p2 = e0.Y * v2.X - e0.X * v2.Y;
            if (p2 < p1) { min = p2; max = p1; } else { min = p1; max = p2; }
            rad = fey * boxhalfsize.X + fex * boxhalfsize.Y;
            if (min > rad || max < -rad) return false;


            //edge 1
            fex = Abs(e1.X);
            fey = Abs(e1.Y);
            fez = Abs(e1.Z);

            //AXISTEST_X01
            p0 = e1.Z * v0.Y - e1.Y * v0.Z;
            p2 = e1.Z * v2.Y - e1.Y * v2.Z;
            if (p0 < p2) { min = p0; max = p2; } else { min = p2; max = p0; }
            rad = fez * boxhalfsize.Y + fey * boxhalfsize.Z;
            if (min > rad || max < -rad) return false;

            //AXISTEST_Y02
            p0 = -e1.Z * v0.X + e1.X * v0.Z;
            p2 = -e1.Z * v2.X + e1.X * v2.Z;
            if (p0 < p2) { min = p0; max = p2; } else { min = p2; max = p0; }
            rad = fez * boxhalfsize.X + fex * boxhalfsize.Z;
            if (min > rad || max < -rad) return false;

            //AXISTEST_Z0
            p0 = e1.Y * v0.X - e1.X * v0.Y;
            p1 = e1.Y * v1.X - e1.X * v1.Y;
            if (p0 < p1) { min = p0; max = p1; } else { min = p1; max = p0; }
            rad = fey * boxhalfsize.X + fex * boxhalfsize.Y;
            if (min > rad || max < -rad) return false;

            //edge 2
            fex = Abs(e2.X);
            fey = Abs(e2.Y);
            fez = Abs(e2.Z);

            //AXISTEST_X2
            p0 = e2.Z * v0.Y - e2.Y * v0.Z;
            p1 = e2.Z * v1.Y - e2.Y * v1.Z;
            if (p0 < p1) { min = p0; max = p1; } else { min = p1; max = p0; }
            rad = fez * boxhalfsize.Y + fey * boxhalfsize.Z;
            if (min > rad || max < -rad) return false;

            //AXISTEST_Y1
            p0 = -e2.Z * v0.X + e2.X * v0.Z;
            p1 = -e2.Z * v1.X + e2.X * v1.Z;
            if (p0 < p1) { min = p0; max = p1; } else { min = p1; max = p0; }
            rad = fez * boxhalfsize.X + fex * boxhalfsize.Z;
            if (min > rad || max < -rad) return false;

            //AXISTEST_Z12
            p1 = e2.Y * v1.X - e2.X * v1.Y;
            p2 = e2.Y * v2.X - e2.X * v2.Y;
            if (p2 < p1) { min = p2; max = p1; } else { min = p1; max = p2; }
            rad = fey * boxhalfsize.X + fex * boxhalfsize.Y;
            if (min > rad || max < -rad) return false;


            //Bullet 1:
            /*  first test overlap in the {x,y,z}-directions
             *  find min, max of the triangle each direction, and test for overlap in
             *  that direction -- this is equivalent to testing a minimal AABB around
             *  the triangle against the AABB 
             */
            //test in X-direction
            findMinMax(v0.X, v1.X, v2.X, ref min, ref max);
            if (min > boxhalfsize.X || max < -boxhalfsize.X) return false;

            //test in Y-direction
            findMinMax(v0.Y, v1.Y, v2.Y, ref min, ref max);
            if (min > boxhalfsize.Y || max < -boxhalfsize.Y) return false;

            //test in Z-direction
            findMinMax(v0.Z, v1.Z, v2.Z, ref min, ref max);
            if (min > boxhalfsize.Z || max < -boxhalfsize.Z) return false;



            //Bullet 2:
            /*  test if the box intersects the plane of the triangle
            *  compute plane equation of triangle: normal*x+d=0 
            */
            Vector3d normal = Vector3d.Cross(e0, e1);
            if (!testTriangleAABB_planeBoxOverlap(toArray(normal), toArray(v0), toArray(boxhalfsize))) return false;


            //box and triangle overlaps
            return true;
        }

        /// <summary>
        /// Utilizado por testTriangleAABB.
        /// Indica si un Box colisiona con un plano.
        /// Adaptado especificamente a la forma que lo utiliza testTriangleAABB.
        /// </summary>
        /// <param name="normal">normal del plano</param>
        /// <param name="vert">un punto del plano</param>
        /// <param name="maxbox">????</param>
        /// <returns>true si hay colision</returns>
        private static bool testTriangleAABB_planeBoxOverlap(double[] normal, double[] vert, double[] maxbox)
        {
            int q;
            double[] vmin = new double[3];
            double[] vmax = new double[3];
            double v;

            for (q = 0; q <= 2; q++)
            {
                v = vert[q];
                if (normal[q] > 0.0f)
                {
                    vmin[q] = -maxbox[q] - v;
                    vmax[q] = maxbox[q] - v;
                }
                else
                {
                    vmin[q] = maxbox[q] - v;
                    vmax[q] = -maxbox[q] - v;
                }
            }

            if (dot(normal, vmin) > 0.0f) return false;
            if (dot(normal, vmax) >= 0.0f) return true;

            return false;
        }

        public static double Abs(double n)
        {
            return Math.Abs(n);
        }

        /// <summary>
        /// Devuelve el menor y mayor valor de los tres
        /// </summary>
        public static void findMinMax(double x0, double x1, double x2, ref double min, ref double max)
        {
            min = max = x0;
            if (x1 < min) min = x1;
            if (x1 > max) max = x1;
            if (x2 < min) min = x2;
            if (x2 > max) max = x2;
        }


        public static double dot(double[] v1, double[] v2)
        {
            return v1[0] * v2[0] + v1[1] * v2[1] + v1[2] * v2[2];
        }

        public static double[] toArray(Vector3d v)
        {
            return new double[] { v.X, v.Y, v.Z };
        }

    }

    public struct Vector3d
    {
        public double X;
        public double Y;
        public double Z;

        public Vector3d(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3d(Vector3 v)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
        }

        public static Vector3d operator +(Vector3d v1, Vector3d v2)
        {
            return new Vector3d(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
        }

        public static Vector3d operator -(Vector3d v1, Vector3d v2)
        {
            return new Vector3d(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
        }

        public static Vector3d operator *(Vector3d v1, double s)
        {
            return new Vector3d(v1.X * s, v1.Y * s, v1.Z * s);
        }

        public static Vector3d operator /(Vector3d v1, double s)
        {
            return new Vector3d(v1.X / s, v1.Y / s, v1.Z / s);
        }

        public static Vector3d Cross(Vector3d v1, Vector3d v2)
        {
            return new Vector3d(
                v1.Y * v2.Z - v2.Y * v1.Z,
                v2.X * v1.Z - v1.X * v2.Z,
                v1.X * v2.Y - v2.X * v1.Y
                );
        }
    }

    public struct TgcBoundingBoxd
    {
        public Vector3d PMin;
        public Vector3d PMax;
        public Vector3d boxcenter;
        public Vector3d boxhalfsize;

        public TgcBoundingBoxd(TgcBoundingBox b)
        {
            PMin = new Vector3d(b.PMin);
            PMax = new Vector3d(b.PMax);
            boxhalfsize = (PMax - PMin) / 2;
            boxcenter = PMin + boxhalfsize;
        }
    }
}
