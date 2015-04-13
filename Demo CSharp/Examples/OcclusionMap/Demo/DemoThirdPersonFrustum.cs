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
using TgcViewer.Utils.Shaders;

namespace Examples.OcclusionMap
{
    /// <summary>
    /// DemoThirdPersonFrustum
    /// </summary>
    public class DemoThirdPersonFrustum : TgcExample
    {
        const float REMOTE_MESH_MOVEMENT_SPEED = 400f;
        const float REMOTE_MESH_ROTATE_SPEED = 3f;
        const float CAMERA_ZOOM_SPEED = 80f;
        const float CAMERA_ROTATION_SPEED = 0.1f;


        List<TgcMesh> meshes;
        List<Occluder> occluders;
        List<Occluder> enabledOccluders;
        OcclusionDll occlusionDll;
        OcclusionViewport viewport;
        TgcSprite depthBufferSprite;
        List<Occludee.BoundingBox2D> occludees;
        TgcSkyBox skyBox;
        TgcFrustum frustum;
        TgcMesh remoteMesh;
        float remoteMeshOrigAngle;
        List<TgcMesh> frustumCulledMeshes;
        List<TgcMesh> occlusionCulledMeshes;


        public override string getCategory()
        {
            return "Demo";
        }

        public override string getName()
        {
            return "3rd person Frustum";
        }

        public override string getDescription()
        {
            return "3rd person Frustum";
        }

        public override void init()
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            //Cargar ciudad
            TgcSceneLoader loader = new TgcSceneLoader();
            TgcScene scene = loader.loadSceneFromFile(GuiController.Instance.ExamplesMediaDir + "ModelosTgc\\CiudadGrande\\CiudadGrande-TgcScene.xml");

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
            frustumCulledMeshes = new List<TgcMesh>();
            occlusionCulledMeshes = new List<TgcMesh>();

            //Crear Occlusion Dll
            viewport = new OcclusionViewport(d3dDevice.Viewport.Width / 4, d3dDevice.Viewport.Height / 4);
            occlusionDll = new OcclusionDll(viewport.D3dViewport.Width, viewport.D3dViewport.Height);
            occlusionDll.clear();
            occlusionDll.fillDepthBuffer();
            occludees = new List<Occludee.BoundingBox2D>();

            //DepthBuffer
            depthBufferSprite = new TgcSprite();
            depthBufferSprite.Position = new Vector2(0, viewport.D3dViewport.Height);
            depthBufferSprite.Texture = occlusionDll.DepthBufferTexture;


            //Modifiers
            GuiController.Instance.Modifiers.addBoolean("doOcclusion", "doOcclusion", true);
            GuiController.Instance.Modifiers.addBoolean("showOcclusionCull", "showOcclusionCull", true);
            GuiController.Instance.Modifiers.addBoolean("showFrustumCull", "showFrustumCull", false);
            GuiController.Instance.Modifiers.addBoolean("showOccluders", "showOccluders", false);
            GuiController.Instance.Modifiers.addBoolean("showMeshes", "showMeshes", true);
            GuiController.Instance.Modifiers.addBoolean("showDepthBuffer", "showDepthBuffer", false);
            

            GuiController.Instance.UserVars.addVar("visbleMeshes");
            GuiController.Instance.UserVars.addVar("visibleFrustum");
            GuiController.Instance.UserVars.addVar("selOccluder", "-");
            GuiController.Instance.UserVars.addVar("selMesh", "-");
            

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


            //FrustumMesh
            frustum = new TgcFrustum();
            frustum.Effect = ShaderUtils.loadEffect(GuiController.Instance.ExamplesMediaDir + "Shaders\\AlphaBlending.fx");
            frustum.Effect.Technique = "OnlyColorTechnique";
            frustum.AlphaBlendingValue = 0.2f;
            frustum.Color = Color.Red;
            frustum.updateMesh(new Vector3(0, 20, 0), new Vector3(0, 20, 1));
            d3dDevice.RenderState.ReferenceAlpha = 0;


            //Cargar mesh para manejar el Frustum remoto
            remoteMesh = loader.loadSceneFromFile(GuiController.Instance.ExamplesMediaDir + "OcclusionMap\\Auto\\Auto-TgcScene.xml").Meshes[0];
            remoteMesh.Position = new Vector3(0, 20, 0);
            remoteMeshOrigAngle = FastMath.PI;
            remoteMesh.rotateY(remoteMeshOrigAngle);
            remoteMesh.move(0, -20, 0);


            //Camara
            GuiController.Instance.ThirdPersonCamera.Enable = true;
            GuiController.Instance.ThirdPersonCamera.setCamera(remoteMesh.Position, 820, -675);
            GuiController.Instance.ThirdPersonCamera.TargetDisplacement = new Vector3(0, 1000, 0);

        }


        public override void render(float elapsedTime)
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;


            //Mover el remoteMesh
            Vector3 cameraPos = updateRemoteMesh(elapsedTime);

            //Update camara
            updateCamera();

            
            //Calcular Occluders Silhouette 2D, previamente hacer FrustumCulling del Occluder
            enabledOccluders.Clear();
            frustumCulledMeshes.Clear();
            occlusionCulledMeshes.Clear();
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
            bool doOcclusion = (bool)GuiController.Instance.Modifiers["doOcclusion"];
            occludees.Clear();
            int visibleFrustum = 0;
            if (doOcclusion)
            {
                foreach (TgcMesh mesh in meshes)
                {
                    //FrustumCulling
                    if (TgcCollisionUtils.classifyFrustumAABB(frustum, mesh.BoundingBox) == TgcCollisionUtils.FrustumResult.OUTSIDE)
                    {
                        mesh.Enabled = false;
                        frustumCulledMeshes.Add(mesh);
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
                            if (!mesh.Enabled)
                            {
                                occlusionCulledMeshes.Add(mesh);
                            }
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

            //Dibujar los ocultos por Occlusion culling
            bool showOcclusionCull = (bool)GuiController.Instance.Modifiers["showOcclusionCull"];
            if (showOcclusionCull)
            {
                foreach (TgcMesh mesh in occlusionCulledMeshes)
                {
                    mesh.BoundingBox.setRenderColor(Color.Yellow);
                    mesh.BoundingBox.render();
                }
            }

            //Dibujar los ocultos por Frustum culling
            bool showFrustumCull = (bool)GuiController.Instance.Modifiers["showFrustumCull"];
            if (showFrustumCull)
            {
                foreach (TgcMesh mesh in frustumCulledMeshes)
                {
                    mesh.BoundingBox.setRenderColor(Color.LightBlue);
                    mesh.BoundingBox.render();
                }
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

            //Dibujar Frustum y remoteMesh
            frustum.updateVolume(viewport.View, d3dDevice.Transform.Projection);
            frustum.render();
            remoteMesh.render();


            //Dibujar depthBuffer
            bool showDepthBuffer = (bool)GuiController.Instance.Modifiers["showDepthBuffer"];
            if (showDepthBuffer)
            {
                occlusionDll.fillDepthBuffer();
                GuiController.Instance.Drawer2D.beginDrawSprite();
                depthBufferSprite.render();
                GuiController.Instance.Drawer2D.endDrawSprite();
            }



            GuiController.Instance.UserVars["visbleMeshes"] = visibleMeshes + "/" + meshes.Count;
            GuiController.Instance.UserVars["visibleFrustum"] = visibleFrustum.ToString();

        }

        

        /// <summary>
        /// Movimiento del remoteMesh
        /// </summary>
        private Vector3 updateRemoteMesh(float elapsedTime)
        {
            float moveForward = 0f;
            float rotation = 0f;

            //Forward
            if (GuiController.Instance.D3dInput.keyDown(Microsoft.DirectX.DirectInput.Key.W))
            {
                moveForward = -REMOTE_MESH_MOVEMENT_SPEED;
            }
            //Backward
            if (GuiController.Instance.D3dInput.keyDown(Microsoft.DirectX.DirectInput.Key.S))
            {
                moveForward = REMOTE_MESH_MOVEMENT_SPEED;
            }

            //Rotate Left
            if (GuiController.Instance.D3dInput.keyDown(Microsoft.DirectX.DirectInput.Key.A))
            {
                rotation = -REMOTE_MESH_ROTATE_SPEED;
            }
            //Rotate Right
            if (GuiController.Instance.D3dInput.keyDown(Microsoft.DirectX.DirectInput.Key.D))
            {
                rotation = REMOTE_MESH_ROTATE_SPEED;
            }

            //Rotar y mover
            float amountToRotate = rotation * elapsedTime;
            remoteMesh.rotateY(amountToRotate);
            remoteMesh.moveOrientedY(moveForward * elapsedTime);

            //Calcular Look y View
            Vector3 cameraPos = remoteMesh.Position + new Vector3(0, 10, 0);
            float currentAngle = remoteMesh.Rotation.Y - remoteMeshOrigAngle;
            Vector3 remoteMeshLookAtVec = new Vector3(FastMath.Sin(currentAngle), 0, FastMath.Cos(currentAngle));
            Vector3 remoteMeshLookAt = cameraPos + remoteMeshLookAtVec * 10f;
            viewport.View = Matrix.LookAtLH(cameraPos, remoteMeshLookAt, new Vector3(0, 1, 0));

            //Actualizar mesh de Frustum
            if (moveForward != 0f || rotation != 0f)
            {
                frustum.updateMesh(cameraPos, remoteMeshLookAt);
            }

            //Actualizar camara
            GuiController.Instance.ThirdPersonCamera.Target = cameraPos;
            GuiController.Instance.ThirdPersonCamera.rotateY(amountToRotate);

            return cameraPos;
        }

        /// <summary>
        /// Actualizar camara
        /// </summary>
        private void updateCamera()
        {
            //Zoom con rueda del mouse
            if (GuiController.Instance.D3dInput.WheelPos != 0)
            {
                GuiController.Instance.ThirdPersonCamera.OffsetHeight += CAMERA_ZOOM_SPEED * GuiController.Instance.D3dInput.WheelPos * -1;
            }

            //Rotar segun movimiento del mouse en X
            if (GuiController.Instance.D3dInput.buttonDown(TgcViewer.Utils.Input.TgcD3dInput.MouseButtons.BUTTON_LEFT))
            {
                GuiController.Instance.ThirdPersonCamera.rotateY(GuiController.Instance.D3dInput.XposRelative * CAMERA_ROTATION_SPEED);
            }

            //Cambiar alejamiento segun movimiento del mouse en Y
            if (GuiController.Instance.D3dInput.buttonDown(TgcViewer.Utils.Input.TgcD3dInput.MouseButtons.BUTTON_LEFT))
            {
                GuiController.Instance.ThirdPersonCamera.OffsetForward += GuiController.Instance.D3dInput.YposRelative * 10;
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
            frustum.dispose();
        }

    }
}
