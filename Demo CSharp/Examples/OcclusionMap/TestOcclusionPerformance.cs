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

namespace Examples.OcclusionMap
{
    /// <summary>
    /// TestOcclusionPerformance
    /// </summary>
    public class TestOcclusionPerformance : TgcExample
    {
        List<TgcMesh> meshes;
        List<Occluder> occluders;
        List<Occluder> enabledOccluders;
        OcclusionDll occlusionDll;
        OcclusionViewport viewport;
        TgcSprite depthBufferSprite;
        List<Occludee.BoundingBox2D> occludees;
        List<CameraPosition> positions;


        public override string getCategory()
        {
            return "OcclusionMap";
        }

        public override string getName()
        {
            return "Test Occlusion Performance";
        }

        public override string getDescription()
        {
            return "Test Occlusion Performance.";
        }

        public override void init()
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            //Cargar posiciones de camara
            positions = new List<CameraPosition>();
            positions.Add(new CameraPosition(new Vector3(-804.5816f, 20f, -181.6737f), new Vector3(-803.6071f, 20.017f, -181.4498f)));
            positions.Add(new CameraPosition(new Vector3(-548.5646f, 20f, -943.239f), new Vector3(-548.015f, 19.9858f, -942.4037f)));
            positions.Add(new CameraPosition(new Vector3(-305.9934f, 20f, -856.2088f), new Vector3(-305.2585f, 20.0871f, -855.5363f)));
            positions.Add(new CameraPosition(new Vector3(-0.5903f, 20f, -59.9561f), new Vector3(-1.4264f, 20.0509f, -59.4097f)));
            positions.Add(new CameraPosition(new Vector3(577.9235f, 20f, -146.8293f), new Vector3(576.9952f, 20.1112f, -146.4744f)));
            positions.Add(new CameraPosition(new Vector3(334.1155f, 20f, 587.5468f), new Vector3(333.3409f, 19.9915f, 586.9146f)));
            positions.Add(new CameraPosition(new Vector3(948.5464f, 19.9999f, 912.2582f), new Vector3(947.798f, 19.991f, 911.595f)));
            positions.Add(new CameraPosition(new Vector3(-405.8195f, 19.9999f, 876.9702f), new Vector3(-405.2039f, 20.038f, 876.183f)));



            //Cargar ciudad
            TgcSceneLoader loader = new TgcSceneLoader();
            TgcScene scene = loader.loadSceneFromFile(GuiController.Instance.ExamplesMediaDir + "ModelosTgc\\Ciudad\\Ciudad-TgcScene.xml");

            //Separar occluders del resto
            meshes = new List<TgcMesh>();
            occluders = new List<Occluder>();
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
                    meshes.Add(mesh);
                }
            }

            //Crear Occlusion Dll
            //viewport = new OcclusionViewport(256, 256);
            viewport = new OcclusionViewport(d3dDevice.Viewport.Width / 4, d3dDevice.Viewport.Height / 4);
            occlusionDll = new OcclusionDll(viewport.D3dViewport.Width, viewport.D3dViewport.Height);
            occlusionDll.clear();
            occlusionDll.fillDepthBuffer();
            occludees = new List<Occludee.BoundingBox2D>();

            //DepthBuffer
            depthBufferSprite = new TgcSprite();
            depthBufferSprite.Position = new Vector2(0, viewport.D3dViewport.Height);
            depthBufferSprite.Texture = occlusionDll.DepthBufferTexture;
            //depthBufferSprite.Scaling = new Vector2(0.25f, 0.25f);


            //Modifiers
            GuiController.Instance.Modifiers.addBoolean("doOcclusion", "doOcclusion", true);
            GuiController.Instance.Modifiers.addBoolean("showDepthBuffer", "showDepthBuffer", false);
            GuiController.Instance.Modifiers.addInterval("cameraPos", new string[] { "0", "1", "2", "3", "4", "5", "6", "7" }, 0);

            GuiController.Instance.UserVars.addVar("visibleFrustum");
            GuiController.Instance.UserVars.addVar("visibleOcclusion");
            GuiController.Instance.UserVars.addVar("metrica");
            

            //Camara
            GuiController.Instance.RotCamera.Enable = false;
            //GuiController.Instance.FpsCamera.Enable = true;
            //GuiController.Instance.FpsCamera.setCamera(new Vector3(-800, 20, -200), new Vector3(0, 0, 0));
        }


        public override void render(float elapsedTime)
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;


            //Poner camara en posicion actual
            int currentCameraPos = int.Parse((string)GuiController.Instance.Modifiers["cameraPos"]);
            GuiController.Instance.setCamera(positions[currentCameraPos].pos, positions[currentCameraPos].lookAt);




            //Activar o no Occlusion
            bool doOcclusion = (bool)GuiController.Instance.Modifiers["doOcclusion"];
            int visibleFrustum = 0;
            int visibleOcclusion = 0;



            //Con Occlusion
            if (doOcclusion)
            {
                //Calcular Occluders Silhouette 2D, previamente hacer FrustumCulling del Occluder
                Vector3 cameraPos = GuiController.Instance.FpsCamera.Position;
                enabledOccluders.Clear();
                int visibleQuads = 0;
                foreach (Occluder o in occluders)
                {
                    //FrustumCulling
                    if (TgcCollisionUtils.classifyFrustumAABB(GuiController.Instance.Frustum, o.Mesh.BoundingBox) == TgcCollisionUtils.FrustumResult.OUTSIDE)
                    {
                        o.Enabled = false;
                    }
                    else
                    {
                        //Proyectar occluder
                        o.computeProjectedQuads(cameraPos, viewport);
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
                occludees.Clear();
                foreach (TgcMesh mesh in meshes)
                {
                    //FrustumCulling
                    if (TgcCollisionUtils.classifyFrustumAABB(GuiController.Instance.Frustum, mesh.BoundingBox) == TgcCollisionUtils.FrustumResult.OUTSIDE)
                    {
                        mesh.Enabled = false;
                    }
                    //Occlusion
                    else
                    {
                        visibleFrustum++;

                        //Chequear visibilidad de AABB proyectado del mesh contra DLL
                        Occludee.BoundingBox2D meshBox2D;
                        if (Occludee.projectBoundingBox(mesh.BoundingBox, viewport, out meshBox2D))
                        {
                            mesh.Enabled = true;
                        }
                        else
                        {
                            mesh.Enabled = occlusionDll.convertAndTestOccludee(meshBox2D);
                            meshBox2D.visible = mesh.Enabled;
                            occludees.Add(meshBox2D);
                        }


                        visibleOcclusion += mesh.Enabled ? 1 : 0;
                    }
                }
            }

            //Sin Occlusion
            else
            {
                foreach (TgcMesh mesh in meshes)
                {
                    //FrustumCulling
                    if (TgcCollisionUtils.classifyFrustumAABB(GuiController.Instance.Frustum, mesh.BoundingBox) == TgcCollisionUtils.FrustumResult.OUTSIDE)
                    {
                        mesh.Enabled = false;
                    }
                    else
                    {
                        mesh.Enabled = true;
                        visibleFrustum++;
                        visibleOcclusion++;
                    }
                }
            }

            




            //Dibujar los habilitados
            foreach (TgcMesh mesh in meshes)
            {
                if (mesh.Enabled)
                {
                    mesh.render();
                }
            }



            //Dibujar depthBuffer
            bool showDepthBuffer = (bool)GuiController.Instance.Modifiers["showDepthBuffer"];
            if (showDepthBuffer)
            {
                occlusionDll.fillDepthBuffer();
                //occlusionDll.fillDepthBuffer(occludees);
                GuiController.Instance.Drawer2D.beginDrawSprite();
                depthBufferSprite.render();
                GuiController.Instance.Drawer2D.endDrawSprite();
            }



            //Mostrar resultados
            GuiController.Instance.UserVars["visibleFrustum"] = visibleFrustum.ToString();
            GuiController.Instance.UserVars["visibleOcclusion"] = visibleOcclusion.ToString();
            float metrica = (1f - ((float)visibleOcclusion / (float)visibleFrustum)) * 100f;
            GuiController.Instance.UserVars["metrica"] = metrica.ToString();

        }



        public override void close()
        {
            foreach (TgcMesh mesh in meshes)
            {
                mesh.dispose();
            }
            foreach (Occluder o in occluders)
            {
                o.dispose();
            }

            occlusionDll.dispose();
        }

    }

    /// <summary>
    /// Posicion de camara
    /// </summary>
    public class CameraPosition
    {
        public Vector3 pos;
        public Vector3 lookAt;

        public CameraPosition(Vector3 pos, Vector3 lookAt)
        {
            this.pos = pos;
            this.lookAt = lookAt;
        }
    }


}
