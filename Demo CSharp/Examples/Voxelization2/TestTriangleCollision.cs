using System;
using System.Collections.Generic;
using System.Text;
using TgcViewer.Example;
using TgcViewer;
using Microsoft.DirectX.Direct3D;
using System.Drawing;
using Microsoft.DirectX;
using TgcViewer.Utils.Modifiers;
using Examples.Shaders;
using TgcViewer.Utils.TgcGeometry;
using TgcViewer.Utils.TgcSceneLoader;
using TgcViewer.Utils.Shaders;
using Examples.OcclusionMap;

namespace Examples.Voxelization2
{
    /// <summary>
    /// TestTriangleCollision
    /// </summary>
    public class TestTriangleCollision : TgcExample
    {

        TgcMeshShader mesh;
        TgcTriangleArray triangleArray;
        Effect effect;
        Voxel[, ,] voxels;
        VoxelMesh[, ,] voxelMeshes;
        TgcPickingRay pickingRay;

        public override string getCategory()
        {
            return "Voxelization2";
        }

        public override string getName()
        {
            return "Test Triangle Collision";
        }

        public override string getDescription()
        {
            return "Test Triangle Collision.";
        }

        public override void init()
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            //Shader de AlphaBlending
            effect = ShaderUtils.loadEffect(GuiController.Instance.ExamplesMediaDir + "Shaders\\AlphaBlending.fx");
            d3dDevice.RenderState.ReferenceAlpha = 0;

            //Cargar mesh
            string initialMeshFile = GuiController.Instance.ExamplesMediaDir + "OcclusionMap\\Edificio8\\Edificio8-TgcScene.xml";
            TgcSceneLoader loader = new TgcSceneLoader();
            loader.MeshFactory = new CustomMeshShaderFactory();
            mesh = (TgcMeshShader)loader.loadSceneFromFile(initialMeshFile).Meshes[0];
            mesh.Effect = effect;
            mesh.AlphaBlendEnable = true;

            //Triangulos
            pickingRay = new TgcPickingRay();
            triangleArray = TgcTriangleArray.fromMesh(mesh);
            for (int i = 0; i < triangleArray.Triangles.Count; i++)
			{
                TgcTriangle t = triangleArray.Triangles[i];
                t.Color = Utils.getRandomBlueColor();
                t.updateValues();
			}
            triangleArray.setEnabled(false);
            triangleArray.Triangles[4391].Enabled = true;
            



            //Voxelizar
            Vector3 meshSize = mesh.BoundingBox.calculateSize();
            Vector3 voxelSize = new Vector3(14.3195f, 58.56668f, 14.302f);

            //Crear matriz de voxels
            int voxelsCountX = (int)(meshSize.X / voxelSize.X);
            int voxelsCountY = (int)(meshSize.Y / voxelSize.Y) + 1;
            int voxelsCountZ = (int)(meshSize.Z / voxelSize.Z);
            voxels = new Voxel[voxelsCountX, voxelsCountY, voxelsCountZ];

            //Crear surface voxels
            createSurfaceVoxels(voxelSize, mesh.BoundingBox.PMin);


            //Crear meshes de voxels para debug
            voxelMeshes = new VoxelMesh[voxels.GetLength(0), voxels.GetLength(1), voxels.GetLength(2)];
            for (int i = 0; i < voxels.GetLength(0); i++)
            {
                for (int j = 0; j < voxels.GetLength(1); j++)
                {
                    for (int k = 0; k < voxels.GetLength(2); k++)
                    {
                        Voxel voxel = voxels[i, j, k];
                        VoxelMesh voxelMesh;
                        if (voxel.type == Voxel.VoxelType.Surface)
                        {
                            voxelMesh = new VoxelMesh();
                            voxelMesh.Color = Utils.getRandomRedColor();
                            voxelMesh.PMin = voxel.aabb.PMin;
                            voxelMesh.PMax = voxel.aabb.PMax;
                            voxelMesh.Effect = effect;
                            voxelMesh.updateValues();
                            voxelMeshes[i, j, k] = voxelMesh;
                        }
                    }
                }
            }




            //Camera
            GuiController.Instance.FpsCamera.Enable = true;
            GuiController.Instance.FpsCamera.setCamera(new Vector3(79.9665f, 1080.519f, -236.1962f), new Vector3(79.6561f, 1080.496f, -235.2459f));

            GuiController.Instance.Modifiers.addBoolean("showMesh", "showMesh", true);
            GuiController.Instance.Modifiers.addFloat("meshAlpha", 0, 1, 1);
            GuiController.Instance.Modifiers.addBoolean("showVoxels", "showVoxels", false);
            GuiController.Instance.Modifiers.addBoolean("showTriangles", "showTriangles", false);

            GuiController.Instance.UserVars.addVar("triIndex");
            GuiController.Instance.UserVars.addVar("voxelIndex");
        }


        /// <summary>
        /// Crear surface voxels.
        /// Son los que colisionan contra los triangulos del mesh
        /// </summary>
        private void createSurfaceVoxels(Vector3 voxelSize, Vector3 initPos)
        {
            for (int i = 0; i < voxels.GetLength(0); i++)
            {
                for (int j = 0; j < voxels.GetLength(1); j++)
                {
                    for (int k = 0; k < voxels.GetLength(2); k++)
                    {
                        //Crear voxel
                        Voxel voxel = new Voxel();
                        voxels[i, j, k] = voxel;
                        voxel.i = i;
                        voxel.j = j;
                        voxel.k = k;
                        voxel.used = false;
                        voxel.density = 0;
                        Vector3 min = initPos + new Vector3(i * voxelSize.X, j * voxelSize.Y, k * voxelSize.Z);
                        Vector3 max = min + voxelSize;
                        voxel.aabb = new TgcBoundingBox(min, max);

                        //Testear colision contra todos los triangulos
                        bool collide = false;
                        foreach (TgcTriangle tri in triangleArray.Triangles)
                        {
                            if (tri.Enabled && TgcCollisionUtils.testTriangleAABB(tri.A, tri.B, tri.C, voxel.aabb))
                            {
                                collide = true;
                                break;
                            }
                        }

                        if (collide)
                        {
                            //Marcar como surface-voxel
                            voxel.type = Voxel.VoxelType.Surface;
                        }
                        else
                        {
                            voxel.aabb.setRenderColor(Color.Pink);
                            //Marcar como vacio
                            voxel.type = Voxel.VoxelType.Empty;
                        }

                    }
                }
            }
        }


        public override void render(float elapsedTime)
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;


   
            //Dibujar triangulos
            bool showTriangles = (bool)GuiController.Instance.Modifiers["showTriangles"];
            if (showTriangles)
            {
                TgcTriangle tri;
                int triIndex;
                if (GuiController.Instance.D3dInput.buttonDown(TgcViewer.Utils.Input.TgcD3dInput.MouseButtons.BUTTON_RIGHT) && triangleArray.pickTriangle(out tri, out triIndex))
                {
                    GuiController.Instance.UserVars["triIndex"] = triIndex.ToString();
                    Color lastColor = tri.Color;
                    tri.Color = Color.Yellow;
                    tri.updateValues();
                    triangleArray.render();
                    tri.Color = lastColor;
                    tri.updateValues();
                }
                else
                {
                    GuiController.Instance.UserVars["triIndex"] = "-";
                    triangleArray.render();
                }
            }


            //Voxel picking
            VoxelMesh selectedVoxel = null;
            if (GuiController.Instance.D3dInput.buttonDown(TgcViewer.Utils.Input.TgcD3dInput.MouseButtons.BUTTON_RIGHT))
            {
                selectedVoxel = pickVoxel();
            }


            //Dibujar SurfaceVoxels
            bool showVoxels = (bool)GuiController.Instance.Modifiers["showVoxels"];
            effect.Technique = "NoTextureTechnique";
            if (showVoxels)
            {
                for (int i = 0; i < voxelMeshes.GetLength(0); i++)
                {
                    for (int j = 0; j < voxelMeshes.GetLength(1); j++)
                    {
                        for (int k = 0; k < voxelMeshes.GetLength(2); k++)
                        {
                            if (voxels[i, j, k].type == Voxel.VoxelType.Surface)
                            {
                                voxelMeshes[i, j, k].AlphaBlendingValue = 1;

                                if (voxelMeshes[i, j, k].Equals(selectedVoxel))
                                {
                                    Color lastColor = voxelMeshes[i, j, k].Color;
                                    voxelMeshes[i, j, k].Color = Color.Green;
                                    voxelMeshes[i, j, k].updateValues();
                                    voxelMeshes[i, j, k].render();
                                    voxelMeshes[i, j, k].Color = lastColor;
                                    voxelMeshes[i, j, k].updateValues();
                                }
                                else
                                {
                                    voxelMeshes[i, j, k].render();
                                }
                            }
                            else
                            {
                                voxels[i, j, k].aabb.render();
                            }
                        }
                    }
                }
            }


            //voxels[11, 19, 7].aabb.render();


            //Dibujar mesh
            bool showMesh = (bool)GuiController.Instance.Modifiers["showMesh"];
            float meshAlpha = (float)GuiController.Instance.Modifiers["meshAlpha"];
            effect.SetValue("alphaValue", meshAlpha);
            effect.Technique = "DefaultTechnique";
            if (showMesh)
            {
                mesh.render();
                mesh.BoundingBox.render();
            }


        }

        private VoxelMesh pickVoxel()
        {
            pickingRay.updateRay();
            Vector3 segmentA = pickingRay.Ray.Origin;
            Vector3 segmentB = segmentA + Vector3.Scale(pickingRay.Ray.Direction, 10000f);
            float minDist = float.MaxValue;
            VoxelMesh minVoxelMesh = null;
            Voxel minVoxel = null;

            //Buscar la menor colision rayo-voxel
            for (int i = 0; i < voxelMeshes.GetLength(0); i++)
            {
                for (int j = 0; j < voxelMeshes.GetLength(1); j++)
                {
                    for (int k = 0; k < voxelMeshes.GetLength(2); k++)
                    {
                        if (voxels[i, j, k].type == Voxel.VoxelType.Surface)
                        {
                            Voxel voxel = voxels[i, j, k];
                            VoxelMesh voxelMesh = voxelMeshes[i, j, k];
                            Vector3 q;
                            if (TgcCollisionUtils.intersectRayAABB(pickingRay.Ray, voxel.aabb, out q))
                            {
                                float dist = (q - segmentA).Length();
                                if (dist < minDist)
                                {
                                    minDist = dist;
                                    minVoxelMesh = voxelMesh;
                                    minVoxel = voxel;
                                }
                            }
                        }
                    }
                }
            }

            if (minVoxel != null)
            {
                GuiController.Instance.UserVars["voxelIndex"] = "[" + minVoxel.i + ", " + minVoxel.j + ", " + minVoxel.k + "]";
            }
            else
            {
                GuiController.Instance.UserVars["voxelIndex"] = "-";
            }
            

            return minVoxelMesh;
        }

        public override void close()
        {
            mesh.dispose();
            triangleArray.dispose();
        }

    }
}
