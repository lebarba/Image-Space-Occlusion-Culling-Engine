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
using Examples.Voxelization2;

namespace Examples.OcclusionMap.Demo
{
    /// <summary>
    /// DemoVoxelization
    /// </summary>
    public class DemoVoxelization : TgcExample
    {
        const int MAX_STEPS = 6;

        Effect effect;
        DemoMesh edificio1;
        DemoMesh edificio2;
        DemoMesh edificio3;
        TgcBox piso;
        int step;
        


        public override string getCategory()
        {
            return "Demo";
        }

        public override string getName()
        {
            return "Voxelization";
        }

        public override string getDescription()
        {
            return "Voxelization";
        }

        public override void init()
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            //Shader de AlphaBlending
            effect = ShaderUtils.loadEffect(GuiController.Instance.ExamplesMediaDir + "Shaders\\AlphaBlending.fx");
            d3dDevice.RenderState.ReferenceAlpha = 0;

            //Cargar edificio1
            edificio1 = new DemoMesh(GuiController.Instance.ExamplesMediaDir + "OcclusionMap\\Edificio1\\Edificio1-TgcScene.xml", effect);
            edificio1.move(new Vector3(0, 0, 0));

            //Cargar edificio2
            edificio2 = new DemoMesh(GuiController.Instance.ExamplesMediaDir + "OcclusionMap\\Edificio6\\Edificio6-TgcScene.xml", effect);
            edificio2.move(new Vector3(-300, 0, 0));

            //Cargar edificio3
            edificio3 = new DemoMesh(GuiController.Instance.ExamplesMediaDir + "OcclusionMap\\Edificio3\\Edificio3-TgcScene.xml", effect);
            edificio3.move(new Vector3(300, 0, 0));

            //Piso
            piso = TgcBox.fromSize(new Vector3(3000, 0.5f, 3000), TgcTexture.createTexture(GuiController.Instance.ExamplesMediaDir + "ModelosTgc\\CiudadGrande\\Textures\\Path.jpg"));
            piso.UVTiling = new Vector2(10, 10);
            piso.updateValues();

            //Camera
            GuiController.Instance.FpsCamera.Enable = true;
            GuiController.Instance.FpsCamera.setCamera(new Vector3(-21.6287f, 728.1927f, -979.9041f), new Vector3(-21.6341f, 727.844f, -978.9669f));


            //Modifiers
            GuiController.Instance.Modifiers.addFloat("meshAlpha", 0, 1, 1);
            step = 0;
        }


        /// <summary>
        /// Dibujar todo
        /// </summary>
        public override void render(float elapsedTime)
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            //Cambiar de paso
            if (GuiController.Instance.D3dInput.keyPressed(Microsoft.DirectX.DirectInput.Key.RightArrow))
            {
                step = step < MAX_STEPS ? step + 1 : MAX_STEPS;
            }
            else if (GuiController.Instance.D3dInput.keyPressed(Microsoft.DirectX.DirectInput.Key.LeftArrow))
            {
                step = step >=1 ? step - 1 : 0;
            }
            GuiController.Instance.Text3d.drawText("Step: " + step, GuiController.Instance.Panel3d.Width - 100, 2, Color.Yellow);


            //Alpha
            float meshAlpha = (float)GuiController.Instance.Modifiers["meshAlpha"];
            

            //Dibujar cada paso
            switch (step)
            {
                //Edificios solos
                case 0:
                    piso.render();
                    effect.Technique = "DefaultTechnique";
                    effect.SetValue("alphaValue", 1f);
                    edificio1.renderMesh(true);
                    edificio2.renderMesh(true);
                    edificio3.renderMesh(true);
                    break;


                //Surface voxels
                case 1:
                    piso.render();

                    effect.Technique = "NoTextureTechnique";
                    effect.SetValue("alphaValue", 1f);
                    edificio1.renderSurfaceVoxels();
                    edificio2.renderSurfaceVoxels();
                    edificio3.renderSurfaceVoxels();
                    break;

                //Inner voxels
                case 2:
                    effect.Technique = "NoTextureTechnique";
                    effect.SetValue("alphaValue", 1f);
                    edificio1.renderInnerVoxels();
                    edificio2.renderInnerVoxels();
                    edificio3.renderInnerVoxels();

                    piso.render();
                    effect.Technique = "DefaultTechnique";
                    effect.SetValue("alphaValue", 0.3f);
                    edificio1.renderMesh(false);
                    edificio2.renderMesh(false);
                    edificio3.renderMesh(false);
                    break;

                //Edificios solos con alpha
                case 3:
                    piso.render();
                    effect.Technique = "DefaultTechnique";
                    effect.SetValue("alphaValue", 0.5f);
                    edificio1.renderMesh(false);
                    edificio2.renderMesh(false);
                    edificio3.renderMesh(false);
                    break;

                default:
                    break;
            }

            //Paso de AABBs
            if (step >= 4)
            {
                int aabbCount = step - 3;
                edificio1.renderConservatibeAABB(aabbCount);
                edificio2.renderConservatibeAABB(aabbCount);
                edificio3.renderConservatibeAABB(aabbCount);

                piso.render();
                effect.Technique = "DefaultTechnique";
                effect.SetValue("alphaValue", 0.3f);
                edificio1.renderMesh(false);
                edificio2.renderMesh(false);
                edificio3.renderMesh(false);
            }
            

        }

        /// <summary>
        /// Mesh para este demo
        /// </summary>
        private class DemoMesh
        {
            TgcMeshShader mesh;
            Voxel[, ,] voxels;
            VoxelMesh[, ,] voxelMeshes;
            List<TgcBoundingBox> conservativeAABBs;
            List<TgcBox> aabbBoxes;

            /// <summary>
            /// Cargar
            /// </summary>
            public DemoMesh(string path, Effect effect)
            {
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
                /*
                foreach (TgcBoundingBox aabb in conservativeAABBs)
                {
                    aabb.setRenderColor(Utils.getRandomGreenColor());
                }
                */

                //Crear boxes para dibujar los AAABBs
                aabbBoxes = new List<TgcBox>();
                foreach (TgcBoundingBox aabb in conservativeAABBs)
                {
                    aabbBoxes.Add(TgcBox.fromExtremes(aabb.PMin, aabb.PMax, Utils.getRandomGreenColor()/*Color.GreenYellow*/));
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
            }

            /// <summary>
            /// Dibujar malla
            /// </summary>
            public void renderMesh(bool aabb)
            {
                mesh.render();
                if (aabb)
                {
                    mesh.BoundingBox.render();
                }
            }

            /// <summary>
            /// Dibujar inner voxels
            /// </summary>
            public void renderInnerVoxels()
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
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Dibujar surface voxels
            /// </summary>
            public void renderSurfaceVoxels()
            {
                for (int i = 0; i < voxelMeshes.GetLength(0); i++)
                {
                    for (int j = 0; j < voxelMeshes.GetLength(1); j++)
                    {
                        for (int k = 0; k < voxelMeshes.GetLength(2); k++)
                        {
                            if (voxels[i, j, k].type == Voxel.VoxelType.Surface)
                            {
                                voxelMeshes[i, j, k].render();
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Dibujar aabb conservativas
            /// </summary>
            public void renderConservatibeAABB(int aabbCount)
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

            /// <summary>
            /// Mover todo
            /// </summary>
            public void move(Vector3 m)
            {
                mesh.move(m);

                //voxels
                for (int i = 0; i < voxels.GetLength(0); i++)
                {
                    for (int j = 0; j < voxels.GetLength(1); j++)
                    {
                        for (int k = 0; k < voxels.GetLength(2); k++)
                        {
                            voxels[i, j, k].aabb.move(m);
                            if (voxelMeshes[i, j, k] != null)
                            {
                                voxelMeshes[i, j, k].PMin += m;
                                voxelMeshes[i, j, k].PMax += m;
                                voxelMeshes[i, j, k].updateValues();
                            }
                        }
                    }
                }

                //conservativeAABBs
                for (int i = 0; i < conservativeAABBs.Count; i++)
                {
                    conservativeAABBs[i].move(m);
                    aabbBoxes[i].move(m);
                }
            }

            /// <summary>
            /// Limpiar
            /// </summary>
            public void dispose()
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
            }
        }


        public override void close()
        {
            edificio1.dispose();
            edificio2.dispose();
            edificio3.dispose();
        }

        

    }
}
