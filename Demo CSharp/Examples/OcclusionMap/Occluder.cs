using System;
using System.Collections.Generic;
using System.Text;
using TgcViewer.Utils.TgcSceneLoader;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using TgcViewer;
using System.Drawing;
using TgcViewer.Utils.TgcGeometry;

namespace Examples.OcclusionMap
{
    /// <summary>
    /// Occluder
    /// </summary>
    public class Occluder
    {

        TgcMesh mesh;
        /// <summary>
        /// Mesh
        /// </summary>
        public TgcMesh Mesh
        {
            get { return mesh; }
            set { mesh = value; }
        }

        bool enabled;
        /// <summary>
        /// Habilitado
        /// </summary>
        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        List<OccluderQuad> projectedQuads;
        /// <summary>
        /// Caras visibles proyectadas del Occluder
        /// </summary>
        public List<OccluderQuad> ProjectedQuads
        {
            get { return projectedQuads; }
        }


        Vector3[] boxVerts = new Vector3[8];
        List<OccluderQuad> candidateQuads;

        public Occluder()
        {
            projectedQuads = new List<OccluderQuad>();
            candidateQuads = new List<OccluderQuad>();
        }


        public void dispose()
        {
            mesh.dispose();
        }

        /// <summary>
        /// Calcular los Quads proyectados del BoundingBox del Occluder que se ven en pantalla
        /// </summary>
        public void computeProjectedQuads(Vector3 cameraPos, OcclusionViewport viewport)
        {
            /*
             0 ---- 1
             |      |   Top-face
             2 ---- 3
             
             
             4 ---- 5
             |      |   Bottom-face
             6 ---- 7
             */

            //Calcular los 8 vertices del AABB
            Vector3 min = mesh.BoundingBox.PMin;
            Vector3 max = mesh.BoundingBox.PMax;
            boxVerts[0] = new Vector3(min.X, max.Y, min.Z);
            boxVerts[1] = new Vector3(max.X, max.Y, min.Z);
            boxVerts[2] = new Vector3(min.X, max.Y, max.Z);
            boxVerts[3] = new Vector3(max.X, max.Y, max.Z);
            boxVerts[4] = new Vector3(min.X, min.Y, min.Z);
            boxVerts[5] = new Vector3(max.X, min.Y, min.Z);
            boxVerts[6] = new Vector3(min.X, min.Y, max.Z);
            boxVerts[7] = new Vector3(max.X, min.Y, max.Z);


            //Testear visibilidad contra las 6 caras y la camara (similar a Back-Face culling)
            projectedQuads.Clear();
            candidateQuads.Clear();
            Vector3 n;
            Vector3 l;

            //Up
            n = new Vector3(0, 1, 0);
            l = boxVerts[0] - cameraPos;
            if (Vector3.Dot(n, l) < 0)
            {
                //Up face - ClockWise
                candidateQuads.Add(new OccluderQuad(boxVerts[0], boxVerts[1], boxVerts[3], boxVerts[2]));
            }

            //Down
            n = new Vector3(0, -1, 0);
            l = boxVerts[4] - cameraPos;
            if (Vector3.Dot(n, l) < 0)
            {
                //Down face - ClockWise
                candidateQuads.Add(new OccluderQuad(boxVerts[4], boxVerts[6], boxVerts[7], boxVerts[5]));
            }

            //Front
            n = new Vector3(0, 0, 1);
            l = boxVerts[2] - cameraPos;
            if (Vector3.Dot(n, l) < 0)
            {
                //Front face - ClockWise
                candidateQuads.Add(new OccluderQuad(boxVerts[2], boxVerts[3], boxVerts[7], boxVerts[6]));
            }

            //Back
            n = new Vector3(0, 0, -1);
            l = boxVerts[0] - cameraPos;
            if (Vector3.Dot(n, l) < 0)
            {
                //Back face - ClockWise
                candidateQuads.Add(new OccluderQuad(boxVerts[0], boxVerts[4], boxVerts[5], boxVerts[1]));
            }

            //Right
            n = new Vector3(1, 0, 0);
            l = boxVerts[1] - cameraPos;
            if (Vector3.Dot(n, l) < 0)
            {
                //Right face - ClockWise
                candidateQuads.Add(new OccluderQuad(boxVerts[1], boxVerts[5], boxVerts[7], boxVerts[3]));
            }

            //Left
            n = new Vector3(-1, 0, 0);
            l = boxVerts[0] - cameraPos;
            if (Vector3.Dot(n, l) < 0)
            {
                //Left face - ClockWise
                candidateQuads.Add(new OccluderQuad(boxVerts[0], boxVerts[2], boxVerts[6], boxVerts[4]));
            }

            
            //Ver si quedo algun Quad visible
            if (candidateQuads.Count > 0)
            {

                //DEBUG
                //candidateQuads.RemoveAt(1);



                //Proyectar quads visibles
                for (int i = 0; i < candidateQuads.Count; i++)
                {
                    //Proyectar y clippear Quad (descartar si tiene menos tamaño que un triángulo)
                    OccluderQuad quad = candidateQuads[i];
                    Vector3[] projPoints;
                    if (viewport.projectQuad_Clipping(quad.Points, out projPoints))
                    {
                        //Tesselar poligono resultante y se van guardando los definitivos en la lista global: projectedQuads
                        //tessellatePolygonInQuads(projPoints, projectedQuads);
                        tessellatePolygonInTriangles(projPoints, projectedQuads);
                        

                        //DEBUG
                        //projectedQuads.RemoveAt(0);
                        //projectedQuads.RemoveAt(projectedQuads.Count - 1);
                    }
                }



                /* VERSION ANTERIOR FUNCIONANDO
                 
                //Proyectar quads visibles
                int count = projectedQuads.Count;
                for (int i = 0; i < count; i++)
                {
                    OccluderQuad quad = projectedQuads[i];
                    Vector3[] projPoints = viewport.projectQuad(quad.Points);

                    //4 vertices, no hay clipping
                    if (projPoints.Length == 4)
                    {
                        quad.set4ProjectedPoints(projPoints);
                        if (!quad.validateSize())
                        {
                            projectedQuads.RemoveAt(i);
                            i--;
                            count--;
                        }
                    }
                    //3 vertices
                    else if (projPoints.Length == 3)
                    {
                        quad.set3ProjectedPoints(projPoints);
                        if (!quad.validateSize())
                        {
                            projectedQuads.RemoveAt(i);
                            i--;
                            count--;
                        }
                    }
                    //5 vertices, hay que partir en dos
                    else if (projPoints.Length == 5)
                    {
                        //1 quad con los primeros 4 vertices
                        quad.set4ProjectedPoints(projPoints);
                        if (!quad.validateSize())
                        {
                            projectedQuads.RemoveAt(i);
                            i--;
                            count--;
                        }

                        //1 triangulo con los ultimos 3 vertices
                        OccluderQuad triQuad = new OccluderQuad(projPoints[3], projPoints[4], projPoints[0]);
                        if (triQuad.validateSize())
                        {
                            projectedQuads.Add(triQuad);
                        }
                    }
                    //Occluder degenerado, descartar
                    else
                    {
                        projectedQuads.RemoveAt(i);
                        i--;
                        count--;
                    }
                }
                */


                /* VERSION SIMPLE
               OccluderQuad quad = projectedQuads[0];
               projectedQuads.Clear();
               projectedQuads.Add(quad);
               quad.project(viewport);
               */

            }
            
        }

        /// <summary>
        /// Dividir el poligono que puede tener entre 4 y 10 vertices en N Quads y Triangulos.
        /// </summary>
        public void tessellatePolygonInQuads(Vector3[] p, List<OccluderQuad> occluderQuads)
        {
            OccluderQuad q;

            //1 solo Quad
            if (p.Length == 4)
            {
                q = new OccluderQuad(p[0], p[1], p[2], p[3]);
                addValidQuad(q, occluderQuads);
            }
            //1 solo Triangulo
            else if (p.Length == 3)
            {
                q = new OccluderQuad(p[0], p[1], p[2]);
                addValidTriangle(q, occluderQuads);
            }
            //Tessellate
            else
            {
                //Primer quad
                q = new OccluderQuad(p[0], p[1], p[2], p[3]);
                addValidQuad(q, occluderQuads);

                int last = 3;
                while (true)
                {
                    //Triangulo
                    if (last + 2 == p.Length)
                    {
                        q = new OccluderQuad(p[0], p[last], p[last + 1]);
                        addValidTriangle(q, occluderQuads);
                        break;
                    }
                    //Quad
                    else
                    {
                        q = new OccluderQuad(p[0], p[last], p[last + 1], p[last + 2]);
                        addValidQuad(q, occluderQuads);
                        last += 2;

                        //Salir si con el ultimo Quad completamos todo el poligono
                        if (last + 1 == p.Length)
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Dividir el poligono que puede tener entre 4 y 10 vertices en N Triangulos.
        /// </summary>
        public void tessellatePolygonInTriangles(Vector3[] p, List<OccluderQuad> occluderQuads)
        {
            OccluderQuad q;

            //Primer triangulo
            q = new OccluderQuad(p[0], p[1], p[2]);
            addValidTriangle(q, occluderQuads);
            int last = 2;

            while (last < p.Length - 1)
            {
                q = new OccluderQuad(p[0], p[last], p[last + 1]);
                addValidTriangle(q, occluderQuads);
                last++;
            }
        }

        /// <summary>
        /// Agrega un OccluderQuad a la lista solo si es un Quad valido (no degenerado)
        /// </summary>
        public void addValidQuad(OccluderQuad q, List<OccluderQuad> occluderQuads)
        {
            if (q.validateSize())
            {
                //Chequear que los vertices (0, 1, 2) no esten alineados en X o en Y
                if ((q.Points[0].X == q.Points[1].X && q.Points[1].X == q.Points[2].X) || 
                    (q.Points[0].Y == q.Points[1].Y && q.Points[1].Y == q.Points[2].Y))
                {
                    //Degenera en un triangulo (0, 2, 3)
                    addValidTriangle(new OccluderQuad(q.Points[0], q.Points[2], q.Points[3]), occluderQuads);
                }
                //Chequear que los vertices (1, 2, 3) no esten alineados en X o en Y
                else if ((q.Points[1].X == q.Points[2].X && q.Points[2].X == q.Points[3].X) ||
                    (q.Points[1].Y == q.Points[2].Y && q.Points[2].Y == q.Points[3].Y))
                {
                    //Degenera en un triangulo (1, 3, 0)
                    addValidTriangle(new OccluderQuad(q.Points[1], q.Points[3], q.Points[0]), occluderQuads);
                }
                //Chequear que los vertices (2, 3, 0) no esten alineados en X o en Y
                else if ((q.Points[2].X == q.Points[3].X && q.Points[3].X == q.Points[0].X) ||
                    (q.Points[2].Y == q.Points[3].Y && q.Points[3].Y == q.Points[0].Y))
                {
                    //Degenera en un triangulo (2, 0, 1)
                    addValidTriangle(new OccluderQuad(q.Points[2], q.Points[0], q.Points[1]), occluderQuads);
                }
                //Chequear que los vertices (3, 0, 1) no esten alineados en X o en Y
                else if ((q.Points[3].X == q.Points[0].X && q.Points[0].X == q.Points[1].X) ||
                    (q.Points[3].Y == q.Points[0].Y && q.Points[0].Y == q.Points[1].Y))
                {
                    //Degenera en un triangulo (3, 1, 2)
                    addValidTriangle(new OccluderQuad(q.Points[3], q.Points[1], q.Points[2]), occluderQuads);
                }
                else
                {
                    //Esta ok
                    occluderQuads.Add(q);
                }
            }
        }

        /// <summary>
        /// Agrega un OccluderQuad a la lista solo si es un Triangulo valido (no degenerado)
        /// </summary>
        public void addValidTriangle(OccluderQuad q, List<OccluderQuad> occluderQuads)
        {
            if (q.validateSize())
            {
                occluderQuads.Add(q);
            }
        }

        /// <summary>
        /// Cara de un Occluder, como Quad de 4 vertices
        /// </summary>
        public struct OccluderQuad
        {
            Vector3[] points;
            /// <summary>
            /// Los 4 vertices del Quad en ClockWise order
            /// </summary>
            public Vector3[] Points
            {
                get { return points; }
            }

            /// <summary>
            /// Crear Quad con sus 4 vertices 3D en ClockWise order (sin proyectar aun)
            /// </summary>
            public OccluderQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
            {
                this.points = new Vector3[] { v1, v2, v3, v4 };
            }

            /// <summary>
            /// Crear Quad degenerado en un triangulo con 3 vertices ya proyectados, en ClockWise order
            /// </summary>
            public OccluderQuad(Vector3 v1, Vector3 v2, Vector3 v3)
            {
                this.points = new Vector3[] { v1, v2, v3 };
            }

            /// <summary>
            /// Cargar 4 puntos ya proyectados
            /// </summary>
            public void set4ProjectedPoints(Vector3[] projPoints)
            {
                Array.Copy(projPoints, this.points, 4);
            }

            /// <summary>
            /// Cargar 3 puntos ya proyectados.
            /// El quad degenera en un triangulo
            /// </summary>
            public void set3ProjectedPoints(Vector3[] projPoints)
            {
                this.points = new Vector3[3];
                Array.Copy(projPoints, this.points, 3);
            }

            /// <summary>
            /// Indica si el BoundingBox 2D del Quad tiene el tamaño minimo para ser considerado
            /// </summary>
            public bool validateSize()
            {
                if (points.Length == 4)
                {
                    //Rect2D del Quad
                    float minX = FastMath.Min(points[0].X, points[1].X, points[2].X, points[3].X);
                    float minY = FastMath.Min(points[0].Y, points[1].Y, points[2].Y, points[3].Y);
                    float maxX = FastMath.Max(points[0].X, points[1].X, points[2].X, points[3].X);
                    float maxY = FastMath.Max(points[0].Y, points[1].Y, points[2].Y, points[3].Y);

                    if (maxX - minX < 1f) return false;
                    if (maxY - minY < 1f) return false;
                    return true;
                }
                else if(points.Length == 3)
                {
                    //Rect2D del Triangulo
                    float minX = FastMath.Min(points[0].X, points[1].X, points[2].X);
                    float minY = FastMath.Min(points[0].Y, points[1].Y, points[2].Y);
                    float maxX = FastMath.Max(points[0].X, points[1].X, points[2].X);
                    float maxY = FastMath.Max(points[0].Y, points[1].Y, points[2].Y);

                    if (maxX - minX < 1f) return false;
                    if (maxY - minY < 1f) return false;
                    return true;
                }

                return false;

                /*
                //Buscar Rect2D
                float minX = float.MaxValue;
                float minY = float.MaxValue;
                float maxX = float.MinValue;
                float maxY = float.MinValue;
                foreach (Vector3 p in points)
                {
                    if (p.X < minX) minX = p.X;
                    if (p.Y < minY) minY = p.Y;
                    if (p.X > maxX) maxX = p.X;
                    if (p.Y > maxY) maxY = p.Y;
                }

                if (maxX - minX < 0.01f) return false;
                if (maxY - minY < 0.01f) return false;
                return true;
                 */
            }

            public override string ToString()
            {
                if (points.Length == 3)
                {
                    return "0: " + TgcParserUtils.printVector3(points[0]) + ", 1: " + TgcParserUtils.printVector3(points[1]) +
                        ", 2: " + TgcParserUtils.printVector3(points[2]);
                }
                else
                {
                    return "0: " + TgcParserUtils.printVector3(points[0]) + ", 1: " + TgcParserUtils.printVector3(points[1]) +
                        ", 2: " + TgcParserUtils.printVector3(points[2]) + ", 3: " + TgcParserUtils.printVector3(points[3]);
                }
            }

        }


    }
}
