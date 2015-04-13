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

namespace Examples.OcclusionMap
{
    /// <summary>
    /// Occlusion2D
    /// </summary>
    public class EjemploOcclusionDll : TgcExample
    {
        List<TgcMesh> meshes;
        List<Occluder> occluders;
        List<Occluder> enabledOccluders;
        OcclusionDll occlusionDll;
        OcclusionViewport viewport;
        TgcSprite depthBufferSprite;
        List<Occludee.BoundingBox2D> occludees;
        TgcSkyBox skyBox;
        TgcPickingRay pickingRay = new TgcPickingRay();

        public override string getCategory()
        {
            return "OcclusionMap";
        }

        public override string getName()
        {
            return "OcclusionDLL";
        }

        public override string getDescription()
        {
            return "OcclusionDLL.";
        }

        public override void init()
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            //Cargar ciudad
            TgcSceneLoader loader = new TgcSceneLoader();
            //TgcScene scene = loader.loadSceneFromFile(GuiController.Instance.ExamplesMediaDir + "ModelosTgc\\Ciudad\\Ciudad-TgcScene.xml");
            //TgcScene scene = loader.loadSceneFromFile(GuiController.Instance.ExamplesMediaDir + "ModelosTgc\\CiudadGrande\\CiudadGrande-TgcScene.xml");
            TgcScene scene = loader.loadSceneFromFile(GuiController.Instance.ExamplesMediaDir + "ModelosTgc\\CiudadGrandeCerrada\\CiudadGrandeCerrada-TgcScene.xml");


            //Separar occluders del resto
            meshes = new List<TgcMesh>();
            occluders = new List<Occluder>();
            enabledOccluders = new List<Occluder>();
            for (int i = 0; i < scene.Meshes.Count; i++)
            {
                TgcMesh mesh = scene.Meshes[i];
                if (mesh.Layer == "Occluders")
                {
                    /*
                    if (mesh.Name == "Occluder1B023" || mesh.Name == "Occluder1A023")
                    {
                        Occluder oc = new Occluder();
                        oc.Mesh = mesh;
                        occluders.Add(oc); 
                    }
                    */

                    
                    Occluder oc = new Occluder();
                    oc.Mesh = mesh;
                    occluders.Add(oc); 
                     
                }
                else
                {
                    /*
                    if (mesh.Name == "Edificio106" || mesh.Name == "Plane028")
                    {
                        meshes.Add(mesh);
                    }
                    */

                    meshes.Add(mesh);
                }
            }

            //Crear Occlusion Dll
            viewport = new OcclusionViewport(d3dDevice.Viewport.Width / 4, d3dDevice.Viewport.Height / 4);
            //viewport = new OcclusionViewport(295, 185);
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
            GuiController.Instance.Modifiers.addBoolean("showOccluders", "showOccluders", false);
            GuiController.Instance.Modifiers.addBoolean("showMeshes", "showMeshes", true);
            GuiController.Instance.Modifiers.addBoolean("showDepthBuffer", "showDepthBuffer", false);
            GuiController.Instance.Modifiers.addBoolean("showHidden", "showHidden", false);

            GuiController.Instance.UserVars.addVar("visbleMeshes");
            GuiController.Instance.UserVars.addVar("visibleFrustum");
            

            //Camara
            GuiController.Instance.FpsCamera.Enable = true;
            //GuiController.Instance.FpsCamera.setCamera(new Vector3(0, 20, 0), new Vector3(0, 20, 1));
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


            GuiController.Instance.UserVars.addVar("selOccluder", "-");
            GuiController.Instance.UserVars.addVar("selMesh", "-");


            /*
            //DEBUG
            for (int i = 0; i < occluders.Count; i++)
            {
                GuiController.Instance.Modifiers.addBoolean("occ_" + i, occluders[i].Mesh.Name, true);
            }
            */

        }


        public override void render(float elapsedTime)
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            viewport.View = d3dDevice.Transform.View;



            /*
            //DEBUG
            Vector3 cameraPos = GuiController.Instance.FpsCamera.Position;
            enabledOccluders.Clear();
            int visibleQuads = 0;
            for (int i = 0; i < occluders.Count; i++)
			{
                Occluder o = occluders[i];

                bool active = (bool)GuiController.Instance.Modifiers["occ_" + i];
                if (!active)
                    continue;


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
            */











            
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



            
            //DEBUG: 
            //enabledOccluders[0].ProjectedQuads.RemoveAt(3);
            //enabledOccluders[0].ProjectedQuads.RemoveAt(2);
            //enabledOccluders[0].ProjectedQuads.RemoveAt(1);
            //enabledOccluders[0].ProjectedQuads.RemoveAt(0);
            //enabledOccluders.RemoveAt(1);
            


            //Enviar todos los occluders habilitados a la DLL
            occlusionDll.clear();
            occlusionDll.convertAndAddOccluders(enabledOccluders, visibleQuads);



            //Calcular visibilidad de meshes
            bool doOcclusion = (bool)GuiController.Instance.Modifiers["doOcclusion"];
            occludees.Clear();
            int visibleFrustum = 0;
            if (doOcclusion)
            {
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
                        /*
                        //Chequear visibilidad de AABB proyectado del mesh contra DLL
                        Utils.BoundingBox2D meshBox2D;
                        if (Utils.projectBoundingBox(mesh.BoundingBox, viewport, out meshBox2D))
                        {
                            mesh.Enabled = occlusionDll.convertAndTestOccludee(meshBox2D);
                            meshBox2D.visible = mesh.Enabled;
                            occludees.Add(meshBox2D);
                        }
                        else
                        {
                            mesh.Enabled = false;
                        }
                        */

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
                    }
                }
            }
            else
            {
                foreach (TgcMesh mesh in meshes)
                {
                    mesh.Enabled = true;
                }
            }




            //Dibujar los habilitados
            bool showMeshes = (bool)GuiController.Instance.Modifiers["showMeshes"];
            int visibleMeshes = 0;
            if (showMeshes)
            {
                //Opacas
                foreach (TgcMesh mesh in meshes)
                {
                    if (mesh.Enabled && !mesh.AlphaBlendEnable)
                    {
                        mesh.render();
                        //mesh.BoundingBox.render();
                        visibleMeshes++;
                    }
                }

                //Skybox
                skyBox.render();

                //Alpha
                foreach (TgcMesh mesh in meshes)
                {
                    if (mesh.Enabled && mesh.AlphaBlendEnable)
                    {
                        mesh.render();
                        visibleMeshes++;
                    }
                }
            }

            //Dibujar los ocultos
            bool showHidden = (bool)GuiController.Instance.Modifiers["showHidden"];
            if (showHidden)
            {
                d3dDevice.RenderState.ZBufferEnable = false;
                foreach (TgcMesh mesh in meshes)
                {
                    if (!mesh.Enabled)
                    {
                        mesh.BoundingBox.render();
                    }
                }
                d3dDevice.RenderState.ZBufferEnable = true;
            }


            //Dibujar occluders
            bool showOccluders = (bool)GuiController.Instance.Modifiers["showOccluders"];
            if (showOccluders)
            {
                foreach (Occluder o in occluders)
                {
                    o.Mesh.render();
                    //o.Mesh.BoundingBox.render();
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


            //Picking
            pickingRay.updateRay();
            if (showOccluders) pickOccluder();
            if (showMeshes) pickMesh();


            GuiController.Instance.UserVars["visbleMeshes"] = visibleMeshes + "/" + meshes.Count;
            GuiController.Instance.UserVars["visibleFrustum"] = visibleFrustum.ToString();

        }




        
        /// <summary>
        /// Hacer clic sobre un occluder
        /// </summary>
        public void pickOccluder()
        {
            if (GuiController.Instance.D3dInput.buttonDown(TgcViewer.Utils.Input.TgcD3dInput.MouseButtons.BUTTON_RIGHT))
            {
                float minDist = float.MaxValue;
                Occluder minOcc = null;

                //Buscar menor interseccion
                foreach (Occluder o in occluders)
                {
                    Vector3 q;
                    if (TgcCollisionUtils.intersectRayAABB(pickingRay.Ray, o.Mesh.BoundingBox, out q))
                    {
                        float dist = (q - pickingRay.Ray.Origin).LengthSq();
                        if (dist < minDist)
                        {
                            minDist = dist;
                            minOcc = o;
                        }
                    }
                }

                if (minOcc != null)
                {
                    minOcc.Mesh.BoundingBox.render();
                    GuiController.Instance.UserVars["selOccluder"] = minOcc.Mesh.Name;
                }
            }
        }

        /// <summary>
        /// Hacer clic sobre un mesh
        /// </summary>
        public void pickMesh()
        {
            if (GuiController.Instance.D3dInput.buttonDown(TgcViewer.Utils.Input.TgcD3dInput.MouseButtons.BUTTON_RIGHT))
            {
                float minDist = float.MaxValue;
                TgcMesh minMesh = null;

                //Buscar menor interseccion
                foreach (TgcMesh m in meshes)
                {
                    Vector3 q;
                    if (TgcCollisionUtils.intersectRayAABB(pickingRay.Ray, m.BoundingBox, out q))
                    {
                        float dist = (q - pickingRay.Ray.Origin).LengthSq();
                        if (dist < minDist)
                        {
                            minDist = dist;
                            minMesh = m;
                        }
                    }
                }

                if (minMesh != null)
                {
                    minMesh.BoundingBox.render();
                    GuiController.Instance.UserVars["selMesh"] = minMesh.Name;
                }
            }
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
            skyBox.dispose();
        }

    }
}
