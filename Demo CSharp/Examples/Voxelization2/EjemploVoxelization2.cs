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
using TgcViewer.Utils.Shaders;
using Examples.OcclusionMap;
using Examples.Shaders;

namespace Examples.Voxelization2
{
    /// <summary>
    /// EjemploVoxelization2
    /// </summary>
    public class EjemploVoxelization2 : TgcExample
    {

        TgcMeshShader mesh;
        string currentPath;
        Voxel[, ,] voxels;
        VoxelMesh[, ,] voxelMeshes;
        List<TgcBoundingBox> conservativeAABBs;
        List<TgcBox> aabbBoxes;
        Effect effect;
        TgcTriangleArray triangleArray;


        public override string getCategory()
        {
            return "Voxelization2";
        }

        public override string getName()
        {
            return "AABB Voxelization2";
        }

        public override string getDescription()
        {
            return "Voxelization2";
        }

        public override void init()
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            //Malla default
            string initialMeshFile = GuiController.Instance.ExamplesMediaDir + "OcclusionMap\\Edificio8\\Edificio8-TgcScene.xml";

            //Shader de AlphaBlending
            effect = ShaderUtils.loadEffect(GuiController.Instance.ExamplesMediaDir + "Shaders\\AlphaBlending.fx");
            d3dDevice.RenderState.ReferenceAlpha = 0;

            //Modifiers
            currentPath = null;
            GuiController.Instance.Modifiers.addFile("Mesh", initialMeshFile, "-TgcScene.xml |*-TgcScene.xml");
            GuiController.Instance.Modifiers.addBoolean("showMesh", "showMesh", true);
            GuiController.Instance.Modifiers.addFloat("meshAlpha", 0, 1, 1);
            GuiController.Instance.Modifiers.addBoolean("showAABB", "showAABB", true);
            GuiController.Instance.Modifiers.addInt("aabbCount", 0, 20, 20);
            GuiController.Instance.Modifiers.addBoolean("showSurface", "showSurface", false);
            GuiController.Instance.Modifiers.addBoolean("showInner", "showInner", false);
            GuiController.Instance.Modifiers.addFloat("surfaceAlpha", 0, 1, 1);
            GuiController.Instance.Modifiers.addBoolean("showTessellate", "showTessellate", false);

            //Camera
            GuiController.Instance.FpsCamera.Enable = true;

            //UserVars
            GuiController.Instance.UserVars.addVar("aabbCount");
            GuiController.Instance.UserVars.addVar("triIndex");
            
        }

        /// <summary>
        /// Cargar nuevo mesh para voxelizar
        /// </summary>
        private void loadMesh(string path)
        {
            //Limpiar mesh anterior
            disposeMesh();

            //Cargar mesh
            TgcSceneLoader loader = new TgcSceneLoader();
            loader.MeshFactory = new CustomMeshShaderFactory();
            mesh = (TgcMeshShader)loader.loadSceneFromFile(path).Meshes[0];
            mesh.Effect = effect;
            mesh.AlphaBlendEnable = true;

            //Voxelizar
            OccluderVoxelizer voxelizer = new OccluderVoxelizer();
            voxels = voxelizer.voxelizeMesh(mesh);

            //Crear AABBs
            conservativeAABBs = voxelizer.buildConservativesAABB(voxels);

            //Crear boxes para dibujar los AAABBs
            aabbBoxes = new List<TgcBox>();
            foreach (TgcBoundingBox aabb in conservativeAABBs)
            {
                aabbBoxes.Add(TgcBox.fromExtremes(aabb.PMin, aabb.PMax, Color.GreenYellow));
            }

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
                        switch (voxel.type)
                        {
                            case Voxel.VoxelType.Surface:
                                voxelMesh = new VoxelMesh();
                                voxelMesh.Color = Utils.getRandomRedColor();
                                voxelMesh.PMin = voxel.aabb.PMin;
                                voxelMesh.PMax = voxel.aabb.PMax;
                                voxelMesh.Effect = effect;
                                voxelMesh.updateValues();
                                voxelMeshes[i, j, k] = voxelMesh;
                                break;

                            case Voxel.VoxelType.Inner:
                                voxelMesh = new VoxelMesh();
                                voxelMesh.Color = Utils.getRandomBlueColor();
                                voxelMesh.PMin = voxel.aabb.PMin;
                                voxelMesh.PMax = voxel.aabb.PMax;
                                voxelMesh.Effect = effect;
                                voxelMesh.updateValues();
                                voxelMeshes[i, j, k] = voxelMesh;
                                break;

                            case Voxel.VoxelType.Empty:
                                break;
                        }
                    }
                }
            }



            GuiController.Instance.UserVars["aabbCount"] = conservativeAABBs.Count.ToString();




            //Triangulos teselados
            triangleArray = new TgcTriangleArray();
            foreach (OccluderVoxelizer.Triangle t in voxelizer.tessellatedTriangles)
            {
                TgcTriangle tri = new TgcTriangle();
                tri.A = t.a;
                tri.B = t.b;
                tri.C = t.c;
                tri.Color = Utils.getRandomRedColor();
                tri.updateValues();
                triangleArray.Triangles.Add(tri);
            }
              
        }

        


        /// <summary>
        /// Dibujar todo
        /// </summary>
        public override void render(float elapsedTime)
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            //Ver si hay que cargar nuevo mesh
            string selectedPath = (string)GuiController.Instance.Modifiers["Mesh"];
            if (currentPath == null || currentPath != selectedPath)
            {
                currentPath = selectedPath;
                loadMesh(currentPath);
            }


            //Dibujar conservatibeAABB
            bool showAABB = (bool)GuiController.Instance.Modifiers["showAABB"];
            int aabbCount = (int)GuiController.Instance.Modifiers["aabbCount"];
            if (showAABB)
            {
                for (int i = 0; i < conservativeAABBs.Count; i++)
                {
                    if (i < aabbCount)
                    {
                        conservativeAABBs[i].render();
                        aabbBoxes[i].render();
                    }
                }
            }

            //Dibujar InnerVoxels
            bool showInner = (bool)GuiController.Instance.Modifiers["showInner"];
            effect.Technique = "NoTextureTechnique";
            if (showInner)
            {
                for (int i = 0; i < voxelMeshes.GetLength(0); i++)
                {
                    for (int j = 0; j < voxelMeshes.GetLength(1); j++)
                    {
                        for (int k = 0; k < voxelMeshes.GetLength(2); k++)
                        {
                            if (voxels[i, j, k].type == Voxel.VoxelType.Inner)
                            {
                                voxelMeshes[i, j, k].render();
                                //voxels[i, j, k].aabb.render();
                            }
                        }
                    }
                }
            }
            

            //Dibujar SurfaceVoxels
            bool showSurface = (bool)GuiController.Instance.Modifiers["showSurface"];
            float surfaceAlpha = (float)GuiController.Instance.Modifiers["surfaceAlpha"];
            effect.Technique = "NoTextureTechnique";
            if (showSurface)
            {
                for (int i = 0; i < voxelMeshes.GetLength(0); i++)
                {
                    for (int j = 0; j < voxelMeshes.GetLength(1); j++)
                    {
                        for (int k = 0; k < voxelMeshes.GetLength(2); k++)
                        {
                            if (voxels[i, j, k].type == Voxel.VoxelType.Surface)
                            {
                                voxelMeshes[i, j, k].AlphaBlendingValue = surfaceAlpha;
                                voxelMeshes[i, j, k].render();
                            }
                        }
                    }
                }
            }


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



            //Teselado de triangulos
            bool showTessellate = (bool)GuiController.Instance.Modifiers["showTessellate"];
            if (showTessellate)
            {
                TgcTriangle tri;
                int triIndex;
                if (GuiController.Instance.D3dInput.buttonPressed(TgcViewer.Utils.Input.TgcD3dInput.MouseButtons.BUTTON_RIGHT) && triangleArray.pickTriangle(out tri, out triIndex))
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
            
            

        }





        public override void close()
        {
            disposeMesh();
        }

        /// <summary>
        /// Liberar todo
        /// </summary>
        private void disposeMesh()
        {
            if (mesh != null)
            {
                mesh.dispose();
                mesh = null;

                for (int i = 0; i < voxels.GetLength(0); i++)
                {
                    for (int j = 0; j < voxels.GetLength(1); j++)
                    {
                        for (int k = 0; k < voxels.GetLength(2); k++)
                        {
                            voxels[i, j, k].dispose();
                        }
                    }
                }
                voxels = null;

                for (int i = 0; i < voxelMeshes.GetLength(0); i++)
                {
                    for (int j = 0; j < voxelMeshes.GetLength(1); j++)
                    {
                        for (int k = 0; k < voxelMeshes.GetLength(2); k++)
                        {
                            if (voxelMeshes[i, j, k] != null)
                            {
                                voxelMeshes[i, j, k].dispose();
                            }
                        }
                    }
                }
                voxelMeshes = null;

                for (int i = 0; i < conservativeAABBs.Count; i++)
                {
                    conservativeAABBs[i].dispose();
                    aabbBoxes[i].dispose();
                }
                conservativeAABBs = null;
                aabbBoxes = null;

                if (triangleArray != null)
                {
                    triangleArray.dispose();
                }
                triangleArray = null;
            }
        }

    }
}
