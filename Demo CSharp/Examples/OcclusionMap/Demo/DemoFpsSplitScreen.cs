using System;
using System.Collections.Generic;
using System.Text;
using TgcViewer.Example;
using TgcViewer;
using Microsoft.DirectX.Direct3D;
using System.Drawing;
using Microsoft.DirectX;
using TgcViewer.Utils.Modifiers;
using TgcViewer.Utils.TgcSceneLoader;
using TgcViewer.Utils.TgcGeometry;
using Examples.OcclusionMap.DLL;
using TgcViewer.Utils._2D;
using TgcViewer.Utils.Terrain;
using TgcViewer.Utils;

namespace Examples.OcclusionMap.Demo
{
    /// <summary>
    /// DemoFpsSplitScreen
    /// </summary>
    public class DemoFpsSplitScreen : TgcExample
    {
        List<Occluder> occluders;
        List<Occluder> enabledOccluders;
        OcclusionDll occlusionDll;
        OcclusionViewport occlusionViewport;
        TgcSprite depthBufferSprite;
        List<Occludee> occludees;
        TgcSkyBox skyBox;
        Viewport leftViewport;
        Viewport rightViewport;

        //Datos de modifiers
        bool currentDrawAllTiles;
        bool currentOptimized;
        int currentTileSize;
        float currentBufferSize;


        public override string getCategory()
        {
            return "Demo";
        }

        public override string getName()
        {
            return "FPS SplitScreen";
        }

        public override string getDescription()
        {
            return "FPS SplitScreen.";
        }

        public override void init()
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            //Pasar a modo render customizado
            GuiController.Instance.CustomRenderEnabled = true;


            //Cargar ciudad
            TgcSceneLoader loader = new TgcSceneLoader();
            TgcScene scene = loader.loadSceneFromFile(GuiController.Instance.ExamplesMediaDir + "ModelosTgc\\CiudadGrande\\CiudadGrande-TgcScene.xml");

            //Separar occluders del resto
            occluders = new List<Occluder>();
            occludees = new List<Occludee>();
            enabledOccluders = new List<Occluder>();
            for (int i = 0; i < scene.Meshes.Count; i++)
            {
                TgcMesh mesh = scene.Meshes[i];
                if (mesh.Layer == "Occluders")
                {
                    Occluder oc = new Occluder();
                    oc.Mesh = mesh;
                    occluders.Add(oc); 
                }
                else
                {
                    occludees.Add(new Occludee(mesh));
                }
            }

            //Viewport para DepthBuffer
            leftViewport = new Viewport();
            leftViewport.X = 0;
            leftViewport.Y = 0;
            leftViewport.Width = d3dDevice.Viewport.Width / 2;
            leftViewport.Height = d3dDevice.Viewport.Height;
            leftViewport.MinZ = d3dDevice.Viewport.MinZ;
            leftViewport.MaxZ = d3dDevice.Viewport.MaxZ;

            //Viewport para FPS
            rightViewport = new Viewport();
            rightViewport.X = d3dDevice.Viewport.Width / 2;
            rightViewport.Y = 0;
            rightViewport.Width = d3dDevice.Viewport.Width / 2;
            rightViewport.Height = d3dDevice.Viewport.Height;
            rightViewport.MinZ = d3dDevice.Viewport.MinZ;
            rightViewport.MaxZ = d3dDevice.Viewport.MaxZ;

            //Crear matriz de proyeccion para el nuevo tamaño a la mitada
            float aspectRatio = (float)rightViewport.Width / rightViewport.Height;
            d3dDevice.Transform.Projection = Matrix.PerspectiveFovLH(TgcD3dDevice.fieldOfViewY, aspectRatio, TgcD3dDevice.zNearPlaneDistance, TgcD3dDevice.zFarPlaneDistance);


            //Modifiers
            GuiController.Instance.Modifiers.addBoolean("doOcclusion", "doOcclusion", true);
            GuiController.Instance.Modifiers.addBoolean("showDepthBuffer", "showDepthBuffer", true);
            GuiController.Instance.Modifiers.addBoolean("DrawAllTiles", "DrawAllTiles", true);
            GuiController.Instance.Modifiers.addBoolean("Optimized", "Optimized", true);
            GuiController.Instance.Modifiers.addInterval("TileSize", new string[] { "4", "8", "16", "32", "64", "128" }, 2);
            GuiController.Instance.Modifiers.addInterval("BufferSize", new string[] { "1", "0.5", "0.25", "0.125"}, 2);


            //Camara
            GuiController.Instance.FpsCamera.Enable = true;
            GuiController.Instance.FpsCamera.setCamera(new Vector3(-465.5077f, 20.0006f, 441.59f), new Vector3(-466.4288f, 20.3778f, 441.4932f));


            //Crear SkyBox
            skyBox = new TgcSkyBox();
            skyBox.Center = new Vector3(0, 0, 0);
            skyBox.Size = new Vector3(10000, 10000, 10000);
            string texturesPath = GuiController.Instance.ExamplesMediaDir + "ModelosTgc\\SkyBoxCiudad\\";
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Up, texturesPath + "Up.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Down, texturesPath + "Down.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Left, texturesPath + "Left.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Right, texturesPath + "Right.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Front, texturesPath + "Back.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Back, texturesPath + "Front.jpg");
            skyBox.updateValues();


            //Inicializar datos de modifiers
            currentDrawAllTiles = false;
            currentOptimized = false;
            currentTileSize = -1;
            currentBufferSize = -1;
        }


        public override void render(float elapsedTime)
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            //Crear Occlusion Dll
            updateSoftwareRasterizer();

            //Actualizar visibilidad de meshes
            updateVisibility(d3dDevice, GuiController.Instance.FpsCamera.Position, GuiController.Instance.Frustum);

            //Dibujar escena
            drawScene(d3dDevice);

            //Dibujar depthBuffer
            drawDepthBuffer(d3dDevice);
        }

        /// <summary>
        /// Crear rasterizer con nuevos parametros, si es que cambio algo
        /// </summary>
        public void updateSoftwareRasterizer()
        {
            //Variables de modifiers
            bool drawAllTiles = (bool)GuiController.Instance.Modifiers["DrawAllTiles"];
            bool optimized = (bool)GuiController.Instance.Modifiers["Optimized"];
            int tileSize = int.Parse((string)GuiController.Instance.Modifiers["TileSize"]);
            float bufferSize = TgcParserUtils.parseFloat((string)GuiController.Instance.Modifiers["BufferSize"]);

            //Ver si cambio alguna
            if (currentDrawAllTiles != drawAllTiles || optimized != currentOptimized || tileSize != currentTileSize || bufferSize != currentBufferSize)
            {
                currentDrawAllTiles = drawAllTiles;
                currentOptimized = optimized;
                currentTileSize = tileSize;
                currentBufferSize = bufferSize;

                //Dispose de lo anterior
                if (occlusionDll != null)
                {
                    occlusionDll.dispose();
                    depthBufferSprite.dispose();
                }

                //Crear DLL
                occlusionViewport = new OcclusionViewport((int)(rightViewport.Width * bufferSize), (int)(rightViewport.Height * currentBufferSize));
                occlusionDll = new OcclusionDll(occlusionViewport.D3dViewport.Width, occlusionViewport.D3dViewport.Height, currentDrawAllTiles, currentTileSize, currentOptimized ? OcclusionDll.ENGINE_MODE_OPTIMIZED : OcclusionDll.ENGINE_MODE_NORMAL);
                occlusionDll.clear();
                occlusionDll.fillDepthBuffer();

                //DepthBuffer
                depthBufferSprite = new TgcSprite();
                depthBufferSprite.Position = new Vector2(0, 0);
                depthBufferSprite.Texture = occlusionDll.DepthBufferTexture;
                Vector2 scale = new Vector2((float)rightViewport.Width / occlusionViewport.D3dViewport.Width, (float)rightViewport.Height / occlusionViewport.D3dViewport.Height);
                depthBufferSprite.Scaling = scale;
            }
        }


        /// <summary>
        /// Dibujar depthBuffer
        /// </summary>
        private void drawDepthBuffer(Device d3dDevice)
        {
            d3dDevice.BeginScene();
            d3dDevice.Viewport = leftViewport;
            d3dDevice.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);


            bool showDepthBuffer = (bool)GuiController.Instance.Modifiers["showDepthBuffer"];
            if (showDepthBuffer)
            {
                occlusionDll.fillDepthBuffer();
                GuiController.Instance.Drawer2D.beginDrawSprite();
                depthBufferSprite.render();
                GuiController.Instance.Drawer2D.endDrawSprite();
            }

            d3dDevice.EndScene();


            //FPS
            GuiController.Instance.Text3d.drawText("FPS: " + HighResolutionTimer.Instance.FramesPerSecond, 0, 0, Color.Yellow);
        }

        /// <summary>
        /// Dibujar la escena visible
        /// </summary>
        private void drawScene(Device d3dDevice)
        {
            d3dDevice.BeginScene();
            d3dDevice.Viewport = rightViewport;
            d3dDevice.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.LightBlue, 1.0f, 0);

            

            //Dibujar los habilitados
            //Opacas
            foreach (Occludee o in occludees)
            {
                if (o.Mesh.Enabled && !o.Mesh.AlphaBlendEnable)
                {
                    o.Mesh.render();
                }
            }

            //Skybox
            skyBox.render();

            //Alpha
            foreach (Occludee o in occludees)
            {
                if (o.Mesh.Enabled && o.Mesh.AlphaBlendEnable)
                {
                    o.Mesh.render();
                }
            }

            d3dDevice.EndScene();
        }


        /// <summary>
        /// Actualizar visibilidad de meshes, segun Occlusion
        /// </summary>
        private void updateVisibility(Device d3dDevice, Vector3 cameraPos, TgcFrustum frustum)
        {
            occlusionViewport.View = d3dDevice.Transform.View;

            //Calcular Occluders Silhouette 2D, previamente hacer FrustumCulling del Occluder
            enabledOccluders.Clear();
            int visibleQuads = 0;
            foreach (Occluder o in occluders)
            {
                //FrustumCulling
                if (TgcCollisionUtils.classifyFrustumAABB(frustum, o.Mesh.BoundingBox) == TgcCollisionUtils.FrustumResult.OUTSIDE)
                {
                    o.Enabled = false;
                }
                else
                {
                    //Proyectar occluder
                    o.computeProjectedQuads(cameraPos, occlusionViewport);
                    if (o.ProjectedQuads.Count == 0)
                    {
                        o.Enabled = false;
                    }
                    else
                    {
                        o.Enabled = true;
                        enabledOccluders.Add(o);
                        visibleQuads += o.ProjectedQuads.Count;
                    }
                }
            }



            //Enviar todos los occluders habilitados a la DLL
            occlusionDll.clear();
            occlusionDll.convertAndAddOccluders(enabledOccluders, visibleQuads);



            //Calcular visibilidad de meshes
            bool doOcclusion = (bool)GuiController.Instance.Modifiers["doOcclusion"];
            if (doOcclusion)
            {
                foreach (Occludee o in occludees)
                {
                    //FrustumCulling
                    TgcMesh mesh = o.Mesh;
                    if (TgcCollisionUtils.classifyFrustumAABB(frustum, mesh.BoundingBox) == TgcCollisionUtils.FrustumResult.OUTSIDE)
                    {
                        mesh.Enabled = false;
                    }
                    //Occlusion
                    else
                    {
                        //Chequear visibilidad de AABB proyectado del mesh contra DLL
                        if (o.project(occlusionViewport))
                        {
                            mesh.Enabled = true;
                        }
                        else
                        {
                            mesh.Enabled = occlusionDll.convertAndTestOccludee(o.Box2D);
                        }
                    }
                }
            }
            else
            {
                foreach (Occludee o in occludees)
                {
                    o.Mesh.Enabled = true;
                }
            }
        }



        public override void close()
        {
            foreach (Occludee o in occludees)
            {
                o.dispose();
            }
            foreach (Occluder o in occluders)
            {
                o.dispose();
            }

            occlusionDll.dispose();
            skyBox.dispose();
        }

    }
}
