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
using TgcViewer.Utils.Input;
using TgcViewer.Utils;
using TgcViewer.Utils.Interpolation;

namespace Examples.OcclusionMap.Demo
{
    /// <summary>
    /// DemoThirdPersonFrustumFinal
    /// </summary>
    public class DemoThirdPersonFrustumFinal : TgcExample
    {
        const float REMOTE_MESH_MOVEMENT_SPEED = 400f;
        const float REMOTE_MESH_ROTATE_SPEED = 3f;
        const float CAMERA_ZOOM_SPEED = 80f;
        const float CAMERA_ROTATION_SPEED = 0.1f;
        const float DESTROY_MESH_DOWN_SPEED = 50f;
        const float DESTROY_MESH_ROTATE_SPEED = 0.1f;


        List<Occluder> occluders;
        List<Occluder> enabledOccluders;
        OcclusionDll occlusionDll;
        OcclusionViewport occlusionViewport;
        List<Occludee> occludees;
        TgcSkyBox skyBox;
        TgcFrustum frustum;
        TgcMesh remoteMesh;
        float remoteMeshOrigAngle;
        List<TgcMesh> frustumCulledMeshes;
        List<TgcMesh> occlusionCulledMeshes;
        Viewport thirdPersonViewport;
        Viewport fpsViewport;
        Matrix thirdPersonProj;
        Matrix fpsProj;
        List<TgcMesh> destroyingMeshes;
        TgcSprite depthBufferSprite;
        TgcSprite fpsViewportBorder;


        public override string getCategory()
        {
            return "Demo";
        }

        public override string getName()
        {
            return "3rd person Frustum - Final";
        }

        public override string getDescription()
        {
            return "3rd person Frustum - Final";
        }

        public override void init()
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;


            //Pasar a modo render customizado
            GuiController.Instance.CustomRenderEnabled = true;


            //Cargar ciudad
            TgcSceneLoader loader = new TgcSceneLoader();
            TgcScene scene = loader.loadSceneFromFile(GuiController.Instance.ExamplesMediaDir + "ModelosTgc\\CiudadGrande\\CiudadGrande-TgcScene.xml");

            //Separar occluders y occludees
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
            frustumCulledMeshes = new List<TgcMesh>();
            occlusionCulledMeshes = new List<TgcMesh>();

            //Relacionar occluders con occludees
            List<Occluder> sceneOccluders = new List<Occluder>();
            sceneOccluders.AddRange(occluders);
            foreach (Occludee occludee in occludees)
            {
                occludee.addRelatedOccluders(sceneOccluders);
            }


            //Crear Occlusion Dll
            occlusionViewport = new OcclusionViewport(d3dDevice.Viewport.Width / 4, d3dDevice.Viewport.Height / 4);
            occlusionDll = new OcclusionDll(occlusionViewport.D3dViewport.Width, occlusionViewport.D3dViewport.Height);
            occlusionDll.clear();
            occlusionDll.fillDepthBuffer();

            //DepthBuffer
            depthBufferSprite = new TgcSprite();
            depthBufferSprite.Position = new Vector2(0, d3dDevice.Viewport.Height - occlusionViewport.D3dViewport.Height);
            depthBufferSprite.Texture = occlusionDll.DepthBufferTexture;
            

            //Modifiers
            GuiController.Instance.Modifiers.addBoolean("doOcclusion", "doOcclusion", true);
            GuiController.Instance.Modifiers.addBoolean("showOcclusionCull", "showOcclusionCull", true);
            GuiController.Instance.Modifiers.addBoolean("occludedMeshes", "occludedMeshes", false);
            GuiController.Instance.Modifiers.addBoolean("showFrustumCull", "showFrustumCull", false);
            GuiController.Instance.Modifiers.addBoolean("depthBuffer", "depthBuffer", false);
            GuiController.Instance.Modifiers.addBoolean("occluders", "occluders", false);
            GuiController.Instance.Modifiers.addBoolean("showFirstPerson", "showFirstPerson", true);
            

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


            //Cargar mesh para manejar el Frustum remoto
            remoteMesh = loader.loadSceneFromFile(GuiController.Instance.ExamplesMediaDir + "OcclusionMap\\Auto\\Auto-TgcScene.xml").Meshes[0];
            remoteMesh.Position = new Vector3(0, 0, 0);
            remoteMeshOrigAngle = FastMath.PI;
            remoteMesh.rotateY(remoteMeshOrigAngle);

            //FrustumMesh
            Vector3 lookAt = remoteMesh.Position + new Vector3(0, 20, 1);
            frustum = new TgcFrustum();
            frustum.Effect = ShaderUtils.loadEffect(GuiController.Instance.ExamplesMediaDir + "Shaders\\AlphaBlending.fx");
            frustum.Effect.Technique = "OnlyColorTechnique";
            frustum.AlphaBlendingValue = 0.3f;
            frustum.Color = Color.Red;
            frustum.updateMesh(remoteMesh.Position + new Vector3(0, 20, 0), lookAt);
            d3dDevice.RenderState.ReferenceAlpha = 0;


            //Camara en tercera persona
            GuiController.Instance.ThirdPersonCamera.Enable = true;
            GuiController.Instance.ThirdPersonCamera.setCamera(remoteMesh.Position, 700, -750);
            GuiController.Instance.ThirdPersonCamera.TargetDisplacement = new Vector3(0, 800, 0);


            //Viewport para tercera persona (es el original)
            thirdPersonViewport = d3dDevice.Viewport;
            thirdPersonProj = d3dDevice.Transform.Projection;

            //Viewport para primera persona
            fpsViewport = new Viewport();
            fpsViewport.Width = thirdPersonViewport.Width / 3;
            fpsViewport.Height = thirdPersonViewport.Height / 2;
            fpsViewport.X = thirdPersonViewport.Width - fpsViewport.Width;
            fpsViewport.Y = thirdPersonViewport.Height - fpsViewport.Height;
            fpsViewport.MinZ = occlusionViewport.D3dViewport.MinZ;
            fpsViewport.MaxZ = occlusionViewport.D3dViewport.MaxZ;
            float aspectRatio = (float)fpsViewport.Width / fpsViewport.Height;
            fpsProj = Matrix.PerspectiveFovLH(TgcD3dDevice.fieldOfViewY, aspectRatio, TgcD3dDevice.zNearPlaneDistance, TgcD3dDevice.zFarPlaneDistance);

            //Borde para Viewport en primera persona
            fpsViewportBorder = new TgcSprite();
            fpsViewportBorder.Position = new Vector2(fpsViewport.X - 5, fpsViewport.Y - 5);
            fpsViewportBorder.Texture = TgcTexture.createTexture(GuiController.Instance.ExamplesMediaDir + "ModelosTgc\\Imagenes\\Recuadro.png");
            fpsViewportBorder.Scaling = new Vector2((float)fpsViewport.Width / fpsViewportBorder.Texture.Width, (float)fpsViewport.Height / (fpsViewportBorder.Texture.Height - 10));

            //Animacion de edificios que se destruyen
            destroyingMeshes = new List<TgcMesh>();

        }


        public override void render(float elapsedTime)
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            //Mover el remoteMesh
            Vector3 fpsPos;
            Vector3 fpsLookAt;
            updateRemoteMesh(elapsedTime, out fpsPos, out fpsLookAt);

            //Actualizar visibilidad de meshes
            updateVisibility(fpsPos);

            //Animar edificios que se estan destruyendo
            animateDestroyingMeshes();

            //Dibujar escena vista desde arriba en Tercera Persona
            drawThirdPersonScene(d3dDevice);

            //Viewport en primera persona
            bool showFirstPerson = (bool)GuiController.Instance.Modifiers["showFirstPerson"];
            if (showFirstPerson)
            {
                drawFpsPersonScene(d3dDevice, fpsPos, fpsLookAt);
            }

        }

        



        /// <summary>
        /// Dibujar escena vista desde arriba en Tercera Persona
        /// </summary>
        private void drawThirdPersonScene(Device d3dDevice)
        {
            d3dDevice.BeginScene();
            d3dDevice.Viewport = thirdPersonViewport;
            d3dDevice.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.LightBlue, 1.0f, 0);
            d3dDevice.Transform.Projection = thirdPersonProj;


            //FPS
            GuiController.Instance.Text3d.drawText("FPS: " + HighResolutionTimer.Instance.FramesPerSecond, 0, 0, Color.LawnGreen);

            //Frustum %
            float fruPercent = (float)frustumCulledMeshes.Count / occludees.Count * 100;
            GuiController.Instance.Text3d.drawText("Frustum cull: " + string.Format("{0:0.##}", fruPercent) + "%", 0, 20, Color.LawnGreen);

            //Occlusion %
            float occPercent = (float)occlusionCulledMeshes.Count / occludees.Count * 100;
            GuiController.Instance.Text3d.drawText("Occlusion cull: " + string.Format("{0:0.##}", occPercent) + "%", 0, 40, Color.LawnGreen);



            //Dibujar los habilitados
            //Opacas
            foreach (Occludee o in occludees)
            {
                if (o.Mesh.Enabled && !o.Mesh.AlphaBlendEnable)
                {
                    o.Mesh.render();
                }
            }

            //Edificios destruyendose
            foreach (TgcMesh mesh in destroyingMeshes)
            {
                mesh.render();
            }

            //Skybox
            skyBox.render();

            //Remote mesh
            remoteMesh.render();

            //Alpha
            foreach (Occludee o in occludees)
            {
                if (o.Mesh.Enabled && o.Mesh.AlphaBlendEnable)
                {
                    o.Mesh.render();
                }
            }

            //Dibujar los ocultos por Occlusion culling
            bool showOcclusionCull = (bool)GuiController.Instance.Modifiers["showOcclusionCull"];
            bool occludedMeshes = (bool)GuiController.Instance.Modifiers["occludedMeshes"];
            if (showOcclusionCull)
            {
                foreach (TgcMesh mesh in occlusionCulledMeshes)
                {
                    mesh.BoundingBox.setRenderColor(Color.Yellow);
                    mesh.BoundingBox.render();

                    if (occludedMeshes)
                    {
                        mesh.Enabled = true;
                        mesh.render();
                    }
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
            bool showOccluders = (bool)GuiController.Instance.Modifiers["occluders"];
            if (showOccluders)
            {
                foreach (Occluder o in occluders)
                {
                    if (o.Enabled)
                    {
                        o.Mesh.render();
                    }
                }
            }


            //Dibujar Frustum
            frustum.updateVolume(occlusionViewport.View, d3dDevice.Transform.Projection);
            frustum.render();


            d3dDevice.EndScene();


            //Dibujar depthBuffer
            bool depthBuffer = (bool)GuiController.Instance.Modifiers["depthBuffer"];
            if (depthBuffer)
            {
                occlusionDll.fillDepthBuffer();
                GuiController.Instance.Drawer2D.beginDrawSprite();
                depthBufferSprite.render();
                GuiController.Instance.Drawer2D.endDrawSprite();
            }


            //Dibujar borde para viewport FPS
            bool showFirstPerson = (bool)GuiController.Instance.Modifiers["showFirstPerson"];
            if (showFirstPerson)
            {
                GuiController.Instance.Drawer2D.beginDrawSprite();
                fpsViewportBorder.render();
                GuiController.Instance.Drawer2D.endDrawSprite();
            }


        }

        /// <summary>
        /// Dibujar toda la escena desde la camara en primera persona
        /// </summary>
        private void drawFpsPersonScene(Device d3dDevice, Vector3 fpsPos, Vector3 fpsLookAt)
        {
            d3dDevice.BeginScene();
            d3dDevice.Viewport = fpsViewport;
            d3dDevice.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.LightBlue, 1.0f, 0);
            d3dDevice.Transform.Projection = fpsProj;
            d3dDevice.Transform.View = Matrix.LookAtLH(fpsPos + new Vector3(0, 40, 0), fpsLookAt + new Vector3(0, 40, 0), new Vector3(0, 1, 0));


            //Dibujar los habilitados
            //Opacas
            foreach (Occludee o in occludees)
            {
                if (o.Mesh.Enabled && !o.Mesh.AlphaBlendEnable)
                {
                    o.Mesh.render();
                }
            }

            //Edificios destruyendose
            foreach (TgcMesh mesh in destroyingMeshes)
            {
                mesh.render();
            }

            //Skybox
            skyBox.render();

            //Remote mesh
            //remoteMesh.render();

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
        private void updateVisibility(Vector3 cameraPos)
        {
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
            foreach (Occludee o in occludees)
            {
                //FrustumCulling
                TgcMesh mesh = o.Mesh;
                if (TgcCollisionUtils.classifyFrustumAABB(frustum, mesh.BoundingBox) == TgcCollisionUtils.FrustumResult.OUTSIDE)
                {
                    mesh.Enabled = false;
                    frustumCulledMeshes.Add(mesh);
                }
                //Occlusion
                else
                {
                    if (doOcclusion)
                    {
                        //Chequear visibilidad de AABB proyectado del mesh contra DLL
                        if (o.project(occlusionViewport))
                        {
                            mesh.Enabled = true;
                        }
                        else
                        {
                            mesh.Enabled = occlusionDll.convertAndTestOccludee(o.Box2D);
                            if (!mesh.Enabled)
                            {
                                occlusionCulledMeshes.Add(mesh);
                            }
                        }
                    }
                    else
                    {
                        mesh.Enabled = true;
                    }
                }
            }
        }

        

        /// <summary>
        /// Movimiento del remoteMesh
        /// </summary>
        private void updateRemoteMesh(float elapsedTime, out Vector3 fpsPos, out Vector3 fpsLookAt)
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
            fpsPos = remoteMesh.Position + new Vector3(0, 10, 0);
            float currentAngle = remoteMesh.Rotation.Y - remoteMeshOrigAngle;
            Vector3 remoteMeshLookAtVec = new Vector3(FastMath.Sin(currentAngle), 0, FastMath.Cos(currentAngle));
            fpsLookAt = fpsPos + remoteMeshLookAtVec * 10f;
            occlusionViewport.View = Matrix.LookAtLH(fpsPos, fpsLookAt, new Vector3(0, 1, 0));

            //Actualizar mesh de Frustum
            if (moveForward != 0f || rotation != 0f)
            {
                frustum.updateMesh(fpsPos, fpsLookAt);
            }

            //Actualizar camara
            GuiController.Instance.ThirdPersonCamera.Target = fpsPos;
            GuiController.Instance.ThirdPersonCamera.rotateY(amountToRotate);




            //Destruir edificios
            destroyBuilding(fpsPos, fpsLookAt);

        }

        /// <summary>
        /// Destruir edificios
        /// </summary>
        private void destroyBuilding(Vector3 fpsPos, Vector3 fpsLookAt)
        {
            if (GuiController.Instance.D3dInput.keyDown(Microsoft.DirectX.DirectInput.Key.Space))
            {
                //Obtener occludee de choque
                Occludee minOccludee = null;
                foreach (Occludee o in occludees)
                {
                    if (o.Occluders.Count > 0)
                    {
                        if (TgcCollisionUtils.testAABBAABB(o.Mesh.BoundingBox, remoteMesh.BoundingBox))
                        {
                            minOccludee = o;
                            break;
                        }
                    }
               }

                if (minOccludee == null)
                    return;

                //Quitar occludee destruido y sus occluders relacionados
                occludees.Remove(minOccludee);
                foreach (Occluder occluder in minOccludee.Occluders)
                {
                    occluders.Remove(occluder);
                }
                frustumCulledMeshes.Remove(minOccludee.Mesh);
                occlusionCulledMeshes.Remove(minOccludee.Mesh);

                //Agregar a lista de animacion
                destroyingMeshes.Add(minOccludee.Mesh);
            }
        }

        /// <summary>
        /// Animar edificios que se estan destruyendo
        /// </summary>
        private void animateDestroyingMeshes()
        {
            for (int i = 0; i < destroyingMeshes.Count; i++)
            {
                TgcMesh mesh = destroyingMeshes[i];

                mesh.move(0, -DESTROY_MESH_DOWN_SPEED, 0);
                //mesh.rotateY(DESTROY_MESH_ROTATE_SPEED);

                if (mesh.BoundingBox.PMax.Y < 0)
                {
                    destroyingMeshes.RemoveAt(i);
                    i--;
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
            frustum.dispose();
            frustumCulledMeshes.Clear();
            occlusionCulledMeshes.Clear();
        }

    }
}
