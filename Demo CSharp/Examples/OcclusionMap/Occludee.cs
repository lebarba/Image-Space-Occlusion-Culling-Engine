using System;
using System.Collections.Generic;
using System.Text;
using TgcViewer.Utils.TgcSceneLoader;
using TgcViewer.Utils.TgcGeometry;
using Microsoft.DirectX;

namespace Examples.OcclusionMap
{

    /// <summary>
    /// Occlude
    /// </summary>
    public class Occludee
    {
        TgcMesh mesh;
        /// <summary>
        /// Mesh
        /// </summary>
        public TgcMesh Mesh
        {
            get { return mesh; }
        }

        BoundingBox2D box2D;
        /// <summary>
        /// Rect 2D en pantalla
        /// </summary>
        public BoundingBox2D Box2D
        {
            get { return box2D; }
        }

        List<Occluder> occluders;
        /// <summary>
        /// Occluders del Occlude
        /// </summary>
        public List<Occluder> Occluders
        {
            get { return occluders; }
        }

        /// <summary>
        /// Crear nuevo occludee
        /// </summary>
        public Occludee(TgcMesh mesh)
        {
            this.mesh = mesh;
            this.occluders = new List<Occluder>();
        }

        /// <summary>
        /// Agrega todos los occluders que estan completamente adentro del boundingBox del mesh del Occludee.
        /// Va quitando de la lista los occluders que se utilizan
        /// </summary>
        /// <param name="sceneOccluders">Todos los occluders no relacionados del escenario</param>
        public void addRelatedOccluders(List<Occluder> sceneOccluders)
        {
            Vector3 size = mesh.BoundingBox.calculateSize();

            for (int i = 0; i < sceneOccluders.Count; i++)
            {
                Occluder oc = sceneOccluders[i];
                TgcCollisionUtils.BoxBoxResult r = TgcCollisionUtils.classifyBoxBox(oc.Mesh.BoundingBox, mesh.BoundingBox);
                if(r == TgcCollisionUtils.BoxBoxResult.Encerrando)
                {
                    this.occluders.Add(oc);
                    sceneOccluders.RemoveAt(i);
                    i--;
                }
                else if (r == TgcCollisionUtils.BoxBoxResult.Atravesando)
                {
                    Vector3 ocSize = oc.Mesh.BoundingBox.calculateSize();
                    if (ocSize.X < size.X && ocSize.Y < size.Y && ocSize.Z < size.Z)
                    {
                        this.occluders.Add(oc);
                        sceneOccluders.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        /// <summary>
        /// Proyectar BoundingBox del Occludee a un rectangulo 2D
        /// </summary>
        /// <param name="viewport"></param>
        /// <param name="box2D">Rectangulo 2D proyectado</param>
        /// <returns>True si debe considerarse visible por ser algun caso degenerado</returns>
        public bool project(OcclusionViewport viewport)
        {
            return Occludee.projectBoundingBox(mesh.BoundingBox, viewport, out this.box2D);
        }

        /// <summary>
        /// Proyectar AABB a 2D
        /// </summary>
        /// <param name="box3d">BoundingBox 3D</param>
        /// <param name="viewport">Viewport</param>
        /// <param name="box2D">Rectangulo 2D proyectado</param>
        /// <returns>True si debe considerarse visible por ser algun caso degenerad</returns>
        public static bool projectBoundingBox(TgcBoundingBox box3d, OcclusionViewport viewport, out Occludee.BoundingBox2D box2D)
        {
            box2D = new Occludee.BoundingBox2D();

            //Proyectar los 8 puntos, sin dividir aun por W
            Vector3[] corners = box3d.computeCorners();
            Matrix m = viewport.View * viewport.Projection;
            Vector3[] projVertices = new Vector3[corners.Length];
            int width = viewport.D3dViewport.Width;
            int height = viewport.D3dViewport.Height;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector4 pOut = Vector3.Transform(corners[i], m);
                if (pOut.W < 0) return true;
                projVertices[i] = viewport.toScreenSpace(pOut, width, height);
            }

            //Buscar los puntos extremos
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            float minDepth = float.MaxValue;
            foreach (Vector3 v in projVertices)
            {
                if (v.X < min.X)
                {
                    min.X = v.X;
                }
                if (v.Y < min.Y)
                {
                    min.Y = v.Y;
                }
                if (v.X > max.X)
                {
                    max.X = v.X;
                }
                if (v.Y > max.Y)
                {
                    max.Y = v.Y;
                }

                if (v.Z < minDepth)
                {
                    minDepth = v.Z;
                }
            }


            //Clamp
            if (min.X < 0f) min.X = 0f;
            if (min.Y < 0f) min.Y = 0f;
            if (max.X >= width) max.X = width - 1;
            if (max.Y >= height) max.Y = height - 1;

            //Control de tamaño minimo
            if (max.X - min.X < 1f) return true;
            if (max.Y - min.Y < 1f) return true;

            //Cargar valores de box2D
            box2D.min = min;
            box2D.max = max;
            box2D.depth = minDepth;
            return false;
        }



        public override string ToString()
        {
            return mesh.Name + ", Occluders count:" + occluders.Count + ", Box2D: " + box2D.ToString();
        }

        /// <summary>
        /// Liberar recursos
        /// </summary>
        public void dispose()
        {
            mesh.dispose();
            occluders.Clear();
        }


        /// <summary>
        /// BoundingBox 2D
        /// </summary>
        public struct BoundingBox2D
        {
            public Vector2 min;
            public Vector2 max;
            public float depth;
            public bool visible;

            public override string ToString()
            {
                return "Min(" + TgcParserUtils.printFloat(min.X) + ", " + TgcParserUtils.printFloat(min.Y) + "), Max(" + TgcParserUtils.printFloat(max.X) + ", " + TgcParserUtils.printFloat(max.Y) + "), Z: " + TgcParserUtils.printFloat(depth);
            }
        }


    }
}
