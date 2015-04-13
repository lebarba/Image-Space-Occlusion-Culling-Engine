using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Microsoft.DirectX;
using TgcViewer.Utils.TgcGeometry;
using Microsoft.DirectX.DirectInput;
using Microsoft.DirectX.Direct3D;
using TgcViewer;
using TgcViewer.Utils.TgcSceneLoader;

namespace Examples.OcclusionMap
{
    /// <summary>
    /// Utilidades generales
    /// </summary>
    public class Utils
    {
        static Random rand = new Random();

        /// <summary>
        /// Generar color aleatorio
        /// </summary>
        public static Color getRandomColor()
        {
            return Color.FromArgb(rand.Next(0, 256), rand.Next(0, 256), rand.Next(0, 256));
        }

        /// <summary>
        /// Generar color rojo aleatorio
        /// </summary>
        public static Color getRandomRedColor()
        {
            return Color.FromArgb(rand.Next(50, 256), 0, 0);
        }

        /// <summary>
        /// Generar color azul aleatorio
        /// </summary>
        public static Color getRandomBlueColor()
        {
            return Color.FromArgb(0, 0, rand.Next(50, 256));
        }

        /// <summary>
        /// Generar color verde aleatorio
        /// </summary>
        public static Color getRandomGreenColor()
        {
            return Color.FromArgb(0, rand.Next(50, 256), 0);
        }

        


        /*
        /// <summary>
        /// Proyectar AABB a 2D
        /// </summary>
        public static bool projectBoundingBox(TgcBoundingBox box3d, OcclusionViewport viewport, out Occludee.BoundingBox2D box2D)
        {
            box2D = new Occludee.BoundingBox2D();

            //Proyectar los 8 corners del BoundingBox
            Vector3[] projVertices = box3d.computeCorners();
            for (int i = 0; i < projVertices.Length; i++)
            {
                projVertices[i] = viewport.projectPointAndClip(projVertices[i]);
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


            //Descartar rect2D que sale fuera de pantalla
            int w = viewport.D3dViewport.Width;
            int h = viewport.D3dViewport.Height;
            if (min.X >= w || max.X < 0 || min.Y >= h || max.Y < 0)
            {
                return false;
            }


            //Clamp
            if (min.X < 0f) min.X = 0f;
            if (min.Y < 0f) min.Y = 0f;
            if (max.X >= w) max.X = w - 1;
            if (max.Y >= h) max.Y = h -1;

            //Cargar valores de box2D
            box2D.min = min;
            box2D.max = max;
            box2D.depth = minDepth;
            return true;
        }


        public static bool projectBoundingBox2(TgcBoundingBox box3d, OcclusionViewport viewport, out Occludee.BoundingBox2D box2D)
        {
            box2D = new Occludee.BoundingBox2D();

            //Proyectar los 8 corners del BoundingBox a View Space
            Vector3[] corners = box3d.computeCorners();
            Matrix view = viewport.View;
            Vector4[] pView = new Vector4[corners.Length];
            for (int i = 0; i < corners.Length; i++)
            {
                pView[i] = Vector3.Transform(corners[i], view);
                if (pView[i].Z < OcclusionViewport.nearPlaneDistance)
                {
                    return true;
                }
            }

            //Proyectar los 8 puntos a Screen space
            Vector3[] projVertices = new Vector3[pView.Length];
            Matrix proj = viewport.Projection;
            int width = viewport.D3dViewport.Width;
            int height = viewport.D3dViewport.Height;
            for (int i = 0; i < pView.Length; i++)
            {
                Vector4 pOut = Vector4.Transform(pView[i], proj);
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

            //Cargar valores de box2D
            box2D.min = min;
            box2D.max = max;
            box2D.depth = minDepth;
            return false;
        }
        */



        
    }
}
