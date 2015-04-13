using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using TgcViewer;
using System.Drawing;
using TgcViewer.Utils.TgcSceneLoader;

namespace Examples.OcclusionMap.DLL
{
    /// <summary>
    /// Interfaz con la DLL de Software Occlusion en C++
    /// </summary>
    public class OcclusionDll
    {

        #region PInvoke

        const int OCCLUDERS_MAX_POINTS = 4;
        public const int ENGINE_MODE_NORMAL = 1;
        public const int ENGINE_MODE_OPTIMIZED = 2;


        [DllImport("lib\\OcclusionEngine.dll")]
        public static extern IntPtr InitializeOcclusionEngine(
            int width,
            int height,
            OcclusionEngineOptions options
            );


        [DllImport("lib\\OcclusionEngine.dll")]
        public static extern void clearOcclusionEngine(
            IntPtr occlusionHandle
            );


        [DllImport("lib\\OcclusionEngine.dll")]
        public static extern void disposeOcclusionEngine(
            IntPtr occlusionHandle
            );

        [DllImport("lib\\OcclusionEngine.dll")]
        public static extern bool addOccluders(
            IntPtr occlusionHandle,
            OccluderData[] occludersData,
            int numberOfOccluders
            );

        [DllImport("lib\\OcclusionEngine.dll")]
        public static extern bool testOccludeeVisibility(
            IntPtr occlusionHandle,
            OccludeeData occludee
            );

        [DllImport("lib\\OcclusionEngine.dll")]
        public static extern float getDepthBufferPixel(
            IntPtr occlusionHandle,
            int x,
            int y
            );


        public struct OcclusionEngineOptions
        {
            public int engineMode;
            public int tileSize;
            public int numberOfThreads;
            public bool drawAllTiles;
        };

        public struct OccluderPoint
        {
            public int x;
            public int y;
            public float depth;
        }

        public struct OccluderData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = OCCLUDERS_MAX_POINTS)]
            public OccluderPoint[] points;
            public int numberOfPoints;
        }

        public struct OccludeeAABB
        {
            public int xMin;
            public int xMax;
            public int yMin;
            public int yMax;
        };

        public struct OccludeeData
        {
            public OccludeeAABB boundingBox;
            public float depth;
        };

        #endregion



        IntPtr occlusionHandle;
        int width;
        int height;

        TgcTexture depthBufferTexture;
        /// <summary>
        /// Textura que representa el DepthBuffer
        /// </summary>
        public TgcTexture DepthBufferTexture
        {
            get { return depthBufferTexture; }
        }

        public OcclusionDll(int width, int height) : 
            this(width, height, true)
        {
        }

        public OcclusionDll(int width, int height, bool drawAllTiles) :
            this(width, height, drawAllTiles, 16, ENGINE_MODE_OPTIMIZED)
        {
        }

        /// <summary>
        /// Crear Rasterizer
        /// </summary>
        /// <param name="width">ancho del depthBuffer</param>
        /// <param name="height">alto del depthBuffer</param>
        /// <param name="drawAllTiles">En true desactiva lazy tile rasterization</param>
        /// <param name="tileSize">tamaño de tile (4,8,16,32,64,128)</param>
        public OcclusionDll(int width, int height, bool drawAllTiles, int tileSize, int engineMode)
        {
            OcclusionEngineOptions options = new OcclusionEngineOptions();
            options.numberOfThreads = 1;
            options.engineMode = engineMode;
            options.drawAllTiles = drawAllTiles;
            options.tileSize = tileSize;

            this.width = width;
            this.height = height;
            occlusionHandle = OcclusionDll.InitializeOcclusionEngine(width, height, options);
        }

        public void dispose()
        {
            OcclusionDll.disposeOcclusionEngine(occlusionHandle);
        }

        public void clear()
        {
            OcclusionDll.clearOcclusionEngine(occlusionHandle);
        }

        public bool addOccluders(OccluderData[] occluders)
        {
            return OcclusionDll.addOccluders(occlusionHandle, occluders, occluders.Length);
        }

        public bool testOccludeeVisibility(OccludeeData occludee)
        {
            return OcclusionDll.testOccludeeVisibility(occlusionHandle, occludee);
        }

        public float getDepthBufferPixel(int x, int y)
        {
            return OcclusionDll.getDepthBufferPixel(occlusionHandle, x, y);
        }

        /// <summary>
        /// Enviar occluders a la DLL, previa conversion de formato
        /// </summary>
        public void convertAndAddOccluders(List<Occluder> occluders, int occludersQuadCount)
        {
            //Convertir a estructura de DLL
            OccluderData[] dllOccluders = new OccluderData[occludersQuadCount];
            int quadCount = 0;
            foreach (Occluder occluder in occluders)
            {
                //Se crea un occluder por cada quad visible
                foreach (Occluder.OccluderQuad quad in occluder.ProjectedQuads)
                {
                    OccluderData dllOccluder = new OccluderData();
                    dllOccluder.numberOfPoints = quad.Points.Length;

                    //Siempre creamos el array de 4, aunque haya 3 vertices
                    dllOccluder.points = new OccluderPoint[OCCLUDERS_MAX_POINTS];

                    //Convertir puntos del quad
                    for (int i = 0; i < dllOccluder.numberOfPoints; i++)
                    {
                        Vector3 v = quad.Points[i];
                        OccluderPoint p = new OccluderPoint();
                        p.x = (int)v.X;
                        p.y = (int)v.Y;
                        p.depth = v.Z;

                        dllOccluder.points[i] = p;
                    }

                    dllOccluders[quadCount] = dllOccluder;
                    quadCount++;
                }
            }

            //invocar dll
            this.addOccluders(dllOccluders);
        }

        /// <summary>
        /// Enviar Occludee a la DLL, previa conversion de formato.
        /// </summary>
        /// <param name="meshBox2D">Occludee</param>
        /// <returns>True si el Occludee es visible</returns>
        public bool convertAndTestOccludee(Occludee.BoundingBox2D meshBox2D)
        {
            //Convertir a estructura de dll
            OccludeeData ocludeeDll = new OccludeeData();
            ocludeeDll.depth = meshBox2D.depth;

            ocludeeDll.boundingBox = new OccludeeAABB();
            ocludeeDll.boundingBox.xMin = (int)meshBox2D.min.X;
            ocludeeDll.boundingBox.yMin = (int)meshBox2D.min.Y;
            ocludeeDll.boundingBox.xMax = (int)meshBox2D.max.X;
            ocludeeDll.boundingBox.yMax = (int)meshBox2D.max.Y;

            //Invocar dll
            return this.testOccludeeVisibility(ocludeeDll);
        }

        /// <summary>
        /// Llenar el textura DepthBuffer preguntando cada valor a la DLL
        /// </summary>
        public void fillDepthBuffer()
        {
            //Crear textura
            createTexture();

            //Leer depthBuffer
            GraphicsStream stream = depthBufferTexture.D3dTexture.LockRectangle(0, LockFlags.None);
            uint[,] data = new uint[this.height, this.width];
            for (int i = 0; i < this.height; i++)
            {
                for (int j = 0; j < this.width; j++)
                {
                    data[i, j] = (uint)getColorFromDepthBufer(j, i).ToArgb();
                }
            }

            //Actualizar textura
            stream.Write(data);
            depthBufferTexture.D3dTexture.UnlockRectangle(0);
        }

        /// <summary>
        /// Obtener color de Z del DepthBuffer
        /// </summary>
        private Color getColorFromDepthBufer(int x, int y)
        {
            //0: near plane
            //1: muy lejos

            /*
            int z = (int)((1f - getDepthBufferPixel(x, y)) * 255f);
            if (z < 0) z = 0;
            if (z > 255) z = 255;
            
            return Color.FromArgb(z, z, z);
            */

            /*
            //Blanco y negro, sin importar la distancia
            float z = getDepthBufferPixel(x, y);
            if (z < 1f)
            {
                return Color.White;
            }
            return Color.Black;
            */

            
            float z = getDepthBufferPixel(x, y);
            if (z >= 0f && z <= 1f) z = 1f - z;
            else if (z < 0) z = 1f;
            else if (z > 1f) z = 0f;
            else z = 0f;

            int zInt = (int)(z * 255f * 100);
            if (zInt > 255) zInt = 255;
            return Color.FromArgb(zInt, zInt, zInt);


        }


        /// <summary>
        /// Llenar el textura DepthBuffer preguntando cada valor a la DLL.
        /// Tambier dibujar arriba la proyeccion de los occludees
        /// </summary>
        public void fillDepthBuffer(List<Occludee.BoundingBox2D> occludees)
        {
            //Crear textura
            createTexture();
            
            //Leer depthBuffer
            GraphicsStream stream = depthBufferTexture.D3dTexture.LockRectangle(0, LockFlags.None);
            uint[,] data = new uint[this.height, this.width];
            for (int i = 0; i < this.height; i++)
            {
                for (int j = 0; j < this.width; j++)
                {
                    data[i, j] = (uint)getColorFromDepthBufer(j, i).ToArgb();
                }
            }

            //Dibujar arriba los occludee
            uint occludeeVisibleColor = (uint)Color.Red.ToArgb();
            uint occludeeHiddenColor = (uint)Color.Blue.ToArgb();
            foreach (Occludee.BoundingBox2D o in occludees)
            {
                int initX = (int)o.min.X;
                int endX = (int)o.max.X;
                int initY = (int)o.min.Y;
                int endY = (int)o.max.Y;
                uint color = o.visible ? occludeeVisibleColor : occludeeHiddenColor;

                //Rasterizar occludee
                for (int i = initY; i <= endY; i++)
                {
                    for (int j = initX; j <= endX; j++)
                    {
                        data[i, j] = color;
                    }
                }

            }

            
            stream.Write(data);
            depthBufferTexture.D3dTexture.UnlockRectangle(0);
        }

        /// <summary>
        /// Crear textura
        /// </summary>
        private void createTexture()
        {
            if (depthBufferTexture == null)
            {
                Texture t = new Texture(GuiController.Instance.D3dDevice, this.width, this.height, 1, Usage.None, Format.A8R8G8B8, Pool.Managed);
                depthBufferTexture = TgcTexture.createTexture(GuiController.Instance.D3dDevice, "depthBufferTexture", "depthBufferTexture", t);
            }
        }

    }
}
