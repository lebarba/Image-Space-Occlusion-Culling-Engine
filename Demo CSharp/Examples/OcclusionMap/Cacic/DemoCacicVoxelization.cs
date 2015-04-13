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

namespace Examples.OcclusionMap.Cacic
{
    /// <summary>
    /// DemoCacicVoxelization
    /// </summary>
    public class DemoCacicVoxelization : TgcExample
    {
        const int MAX_STEPS = 11;
        const int MAX_AABB = 3;
        const float OCCLUDER_SPEED = 250f;
        const float VOXEL_SPEED = 500f;
        const float ARROW_SPEED = 250f;

        Effect effect;
        DemoMesh edificio1;
        DemoMesh edificio2;
        DemoMesh edificio3;
        TgcBox piso;
        int step;
        TgcBox demoVoxel;
        TgcBox[] testVoxels;
        TgcArrow[] testArrows;
        TgcBox demoBigBox;
        


        public override string getCategory()
        {
            return "Cacic";
        }

        public override string getName()
        {
            return "1 - Voxelization";
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


            //Test voxels
            Vector3 voxelSize = new Vector3(50, 50, 50);
            Vector3 halfVoxelSize = Vector3.Scale(voxelSize, 0.5f);
            Vector3 voxelCenter = new Vector3(0, 300, 0);
            demoVoxel = TgcBox.fromSize(voxelCenter, voxelSize, Color.Blue);
            testVoxels = new TgcBox[6];
            testVoxels[0] = TgcBox.fromSize(voxelCenter + new Vector3(200, 0, 0), voxelSize, Color.Red);
            testVoxels[1] = TgcBox.fromSize(voxelCenter + new Vector3(-300, 0, 0), voxelSize, Color.Red);
            testVoxels[2] = TgcBox.fromSize(voxelCenter + new Vector3(0, 400, 0), voxelSize, Color.Red);
            testVoxels[3] = TgcBox.fromSize(voxelCenter + new Vector3(0, -200, 0), voxelSize, Color.Red);
            testVoxels[4] = TgcBox.fromSize(voxelCenter + new Vector3(0, 0, 400), voxelSize, Color.Red);
            testVoxels[5] = TgcBox.fromSize(voxelCenter + new Vector3(0, 0, -450), voxelSize, Color.Red);
            for (int i = 0; i < testVoxels.Length; i++)
            {
                testVoxels[i].BoundingBox.setRenderColor(Color.Orange);
            }
            demoBigBox = TgcBox.fromExtremes(
                new Vector3(testVoxels[1].BoundingBox.PMin.X, testVoxels[3].BoundingBox.PMin.Y, testVoxels[5].BoundingBox.PMin.Z) + halfVoxelSize,
                new Vector3(testVoxels[0].BoundingBox.PMax.X, testVoxels[2].BoundingBox.PMax.Y, testVoxels[4].BoundingBox.PMax.Z) - halfVoxelSize,
                Color.Yellow
                );
            testArrows = new TgcArrow[6];
            Color arrowColor = Color.Green;
            float arrowThickness = 5f;
            Vector2 arrowHead = new Vector2(10f, 10f);
            testArrows[0] = TgcArrow.fromExtremes(voxelCenter, voxelCenter + new Vector3(1, 0, 0), arrowColor, Color.Violet, arrowThickness, arrowHead);
            testArrows[1] = TgcArrow.fromExtremes(voxelCenter, voxelCenter + new Vector3(-1, 0, 0), arrowColor, Color.Violet, arrowThickness, arrowHead);
            testArrows[2] = TgcArrow.fromExtremes(voxelCenter, voxelCenter + new Vector3(0, 1, 0), arrowColor, Color.Violet, arrowThickness, arrowHead);
            testArrows[3] = TgcArrow.fromExtremes(voxelCenter, voxelCenter + new Vector3(0, -1, 0), arrowColor, Color.Violet, arrowThickness, arrowHead);
            testArrows[4] = TgcArrow.fromExtremes(voxelCenter, voxelCenter + new Vector3(0, 0, 1), arrowColor, Color.Violet, arrowThickness, arrowHead);
            testArrows[5] = TgcArrow.fromExtremes(voxelCenter, voxelCenter + new Vector3(0, 0, -1), arrowColor, Color.Violet, arrowThickness, arrowHead);


            //Camera
            GuiController.Instance.FpsCamera.Enable = true;
            setDefaultCamera();
            GuiController.Instance.FpsCamera.MovementSpeed = 300f;
            GuiController.Instance.FpsCamera.JumpSpeed = 300f;


            //Modifiers
            GuiController.Instance.Modifiers.addFloat("meshAlpha", 0, 1, 0.1f);
            GuiController.Instance.Modifiers.addBoolean("showMesh", "showMesh", true);
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
                    edificio1.renderMesh(false);
                    edificio2.renderMesh(false);
                    edificio3.renderMesh(false);
                    break;

                //Edificios con BoundingBox
                case 1:
                    //Reset voxels
                    edificio1.resetCurrentVoxelVis();
                    edificio2.resetCurrentVoxelVis();
                    edificio3.resetCurrentVoxelVis();

                    piso.render();
                    effect.Technique = "DefaultTechnique";
                    effect.SetValue("alphaValue", 1f);
                    edificio1.renderMesh(true);
                    edificio2.renderMesh(true);
                    edificio3.renderMesh(true);
                    break;

                //Empty Voxels
                case 2:
                    //Voxels AABB
                    edificio1.renderAllVoxelsAABB(elapsedTime);
                    edificio2.renderAllVoxelsAABB(elapsedTime);
                    edificio3.renderAllVoxelsAABB(elapsedTime);

                    //Edificios con alpha
                    piso.render();
                    effect.Technique = "DefaultTechnique";
                    effect.SetValue("alphaValue", 0.3f);
                    edificio1.renderMesh(false);
                    edificio2.renderMesh(false);
                    edificio3.renderMesh(false);
                    break;


                //Surface voxels
                case 3:
                    //Reset camara
                    setDefaultCamera();

                    piso.render();

                    effect.Technique = "NoTextureTechnique";
                    effect.SetValue("alphaValue", 1f);
                    edificio1.renderSurfaceVoxels(elapsedTime);
                    edificio2.renderSurfaceVoxels(elapsedTime);
                    edificio3.renderSurfaceVoxels(elapsedTime);
                    break;

                //Test voxel - quieto
                case 4:
                    //Camara especial
                    setTestVoxelCamera();

                    piso.render();

                    //Reset de flechas
                    testArrows[0].PEnd = testArrows[0].PStart + new Vector3(1, 0, 0);
                    testArrows[1].PEnd = testArrows[0].PStart + new Vector3(-1, 0, 0);
                    testArrows[2].PEnd = testArrows[0].PStart + new Vector3(0, 1, 0);
                    testArrows[3].PEnd = testArrows[0].PStart + new Vector3(0, -1, 0);
                    testArrows[4].PEnd = testArrows[0].PStart + new Vector3(0, 0, 1);
                    testArrows[5].PEnd = testArrows[0].PStart + new Vector3(0, 0, -1);
                    for (int i = 0; i < testArrows.Length; i++)
                    {
                        testArrows[i].updateValues();
                    }

                    //Demo voxel quieto, solo AABB
                    demoVoxel.BoundingBox.render();
                    demoBigBox.BoundingBox.render();

                    //Test voxels
                    for (int i = 0; i < testVoxels.Length; i++)
                    {
                        testVoxels[i].render();
                        testVoxels[i].BoundingBox.render();
                    }
                    break;

                //Test voxel - estirar flechas
                case 5:
                    piso.render();

                    //Estirar flechas
                    float increment = ARROW_SPEED * elapsedTime;
                    bool estirando = false;
                    for (int i = 0; i < testArrows.Length; i++)
                    {
                        estirando |= estirarFlecha(testArrows[i], testVoxels[i], increment);
                        testArrows[i].render();
                    }

                    //Demo voxel quieto
                    demoVoxel.BoundingBox.render();
                    demoBigBox.BoundingBox.render();
                    if (!estirando)
                    {
                        demoVoxel.render();
                    }

                    //Test voxels
                    for (int i = 0; i < testVoxels.Length; i++)
                    {
                        testVoxels[i].render();
                        testVoxels[i].BoundingBox.render();
                    }
                    break;

                //Inner voxels
                case 6:
                    //Reset camara
                    setDefaultCamera();

                    effect.Technique = "NoTextureTechnique";
                    effect.SetValue("alphaValue", 1f);
                    edificio1.renderInnerVoxels(elapsedTime);
                    edificio2.renderInnerVoxels(elapsedTime);
                    edificio3.renderInnerVoxels(elapsedTime);

                    piso.render();
                    effect.Technique = "DefaultTechnique";
                    effect.SetValue("alphaValue", 0.3f);
                    edificio1.renderMesh(false);
                    edificio2.renderMesh(false);
                    edificio3.renderMesh(false);
                    break;

                //Edificios solos con alpha
                case 7:
                    //Reset de Boxes
                    edificio1.resetAabbBox();
                    edificio2.resetAabbBox();
                    edificio3.resetAabbBox();

                    piso.render();
                    effect.Technique = "DefaultTechnique";
                    effect.SetValue("alphaValue", 0.5f);
                    edificio1.renderMesh(false);
                    edificio2.renderMesh(false);
                    edificio3.renderMesh(false);
                    break;

                //Occluders solos al final
                case 11:
                    edificio1.renderConservatibeAABB(MAX_AABB, elapsedTime);
                    edificio2.renderConservatibeAABB(MAX_AABB, elapsedTime);
                    edificio3.renderConservatibeAABB(MAX_AABB, elapsedTime);

                    piso.render();

                    //Dibujar meshes opcionalmente
                    if ((bool)GuiController.Instance.Modifiers["showMesh"])
                    {
                        effect.Technique = "DefaultTechnique";
                        effect.SetValue("alphaValue", (float)GuiController.Instance.Modifiers["meshAlpha"]);
                        edificio1.renderMesh(false);
                        edificio2.renderMesh(false);
                        edificio3.renderMesh(false);
                    }
                    break;

                default:
                    break;
            }

            //Paso de AABBs
            if (step >= 8  && step < 11)
            {
                int aabbCount = step - (8 - 1);
                edificio1.renderConservatibeAABB(aabbCount, elapsedTime);
                edificio2.renderConservatibeAABB(aabbCount, elapsedTime);
                edificio3.renderConservatibeAABB(aabbCount, elapsedTime);

                piso.render();
                effect.Technique = "DefaultTechnique";
                effect.SetValue("alphaValue", 0.3f);
                edificio1.renderMesh(false);
                edificio2.renderMesh(false);
                edificio3.renderMesh(false);
            }
            

        }

        /// <summary>
        /// Camara FPS default
        /// </summary>
        private void setDefaultCamera()
        {
            GuiController.Instance.FpsCamera.setCamera(new Vector3(-21.6287f, 728.1927f, -979.9041f), new Vector3(-21.6341f, 727.844f, -978.9669f));
        }

        /// <summary>
        /// Camara para test voxel
        /// </summary>
        private void setTestVoxelCamera()
        {
            GuiController.Instance.FpsCamera.setCamera(new Vector3(-848.9164f, 539.0069f, -629.7078f), new Vector3(-848.1517f, 538.8228f, -629.0901f));
        }

        /// <summary>
        /// Estira una flecha hasta chocar con el voxel
        /// </summary>
        private bool estirarFlecha(TgcArrow arrow, TgcBox voxel, float increment)
        {
            float arrowLength = Vector3.Length(arrow.PEnd - arrow.PStart);
            float totalLength = Vector3.Length(arrow.PStart - voxel.BoundingBox.calculateBoxCenter());
            float length;
            bool estirando;
            if (arrowLength + increment >= totalLength)
            {
                length = totalLength;
                estirando = false;
            }
            else
            {
                length = arrowLength + increment;
                estirando = true;
            }
            arrow.PEnd = arrow.PStart + Vector3.Normalize(arrow.PEnd - arrow.PStart) * length;
            arrow.updateValues();
            return estirando;
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
            int currentSurfaceVoxelsVis;
            int currentInnerVoxelsVis;
            int currentVoxelsAABBVis;

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
                voxelizer.MaxAabbCount = 3;
                voxelizer.VoxelVolumePercent = 0.05f;
                voxels = voxelizer.voxelizeMesh(mesh);

                //Crear AABBs
                conservativeAABBs = voxelizer.buildConservativesAABB(voxels);
                foreach (TgcBoundingBox aabb in conservativeAABBs)
                {
                    aabb.setRenderColor(Color.Orange);
                }

                //Crear boxes para dibujar los AAABBs
                aabbBoxes = new List<TgcBox>();
                Color[] colors = new Color[] { Color.Green, Color.GreenYellow, Color.LightGreen, Color.YellowGreen };
                for (int i = 0; i < conservativeAABBs.Count; i++)
                {
                    TgcBoundingBox aabb = conservativeAABBs[i];
                    Vector3 c = aabb.calculateBoxCenter();
                    aabbBoxes.Add(TgcBox.fromExtremes(c, c, colors[i % colors.Length]));
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

                                default:
                                    voxels[i, j, k].aabb.setRenderColor(Color.Yellow);
                                    break;
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Reset de visibilidad de voxels
            /// </summary>
            public void resetCurrentVoxelVis()
            {
                currentInnerVoxelsVis = 0;
                currentSurfaceVoxelsVis = 0;
                currentVoxelsAABBVis = 0;
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
            /// Dibujar todos los voxels pero solo su AABB
            /// </summary>
            public void renderAllVoxelsAABB(float elapsedTime)
            {
                int n = (int)(VOXEL_SPEED * elapsedTime);
                currentVoxelsAABBVis += n;

                for (int i = 0; i < voxelMeshes.GetLength(0); i++)
                {
                    for (int j = 0; j < voxelMeshes.GetLength(1); j++)
                    {
                        for (int k = 0; k < voxelMeshes.GetLength(2); k++)
                        {
                            int idx = i * voxelMeshes.GetLength(0) + j * voxelMeshes.GetLength(1) + k;
                            if (idx <= currentVoxelsAABBVis)
                            {
                                voxels[i, j, k].aabb.render();
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Dibujar inner voxels
            /// </summary>
            public void renderInnerVoxels(float elapsedTime)
            {
                int n = (int)(VOXEL_SPEED * elapsedTime);
                currentInnerVoxelsVis += n;

                for (int i = 0; i < voxelMeshes.GetLength(0); i++)
                {
                    for (int j = 0; j < voxelMeshes.GetLength(1); j++)
                    {
                        for (int k = 0; k < voxelMeshes.GetLength(2); k++)
                        {
                            if (voxels[i, j, k].type == Voxel.VoxelType.Inner)
                            {
                                int idx = i * voxelMeshes.GetLength(0) + j * voxelMeshes.GetLength(1) + k;
                                if (idx <= currentInnerVoxelsVis)
                                {
                                    voxelMeshes[i, j, k].render();
                                }
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Dibujar surface voxels
            /// </summary>
            public void renderSurfaceVoxels(float elapsedTime)
            {
                int n = (int)(VOXEL_SPEED * elapsedTime);
                currentSurfaceVoxelsVis += n;

                for (int i = 0; i < voxelMeshes.GetLength(0); i++)
                {
                    for (int j = 0; j < voxelMeshes.GetLength(1); j++)
                    {
                        for (int k = 0; k < voxelMeshes.GetLength(2); k++)
                        {
                            if (voxels[i, j, k].type == Voxel.VoxelType.Surface)
                            {
                                int idx = i * voxelMeshes.GetLength(0) + j * voxelMeshes.GetLength(1) + k;
                                if (idx <= currentSurfaceVoxelsVis)
                                {
                                    voxelMeshes[i, j, k].render();
                                }
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Dibujar aabb conservativas
            /// </summary>
            public void renderConservatibeAABB(int aabbCount, float elapsedTime)
            {
                for (int i = 0; i < conservativeAABBs.Count; i++)
                {
                    TgcBox box = aabbBoxes[i];
                    TgcBoundingBox aabb = conservativeAABBs[i];

                    if (i < aabbCount)
                    {
                        //Ultimo AABB, agrandar volumen hasta llegar al tamaño real
                        if (i == aabbCount - 1)
                        {
                            float increment = OCCLUDER_SPEED * elapsedTime;
                            Vector3 min = box.BoundingBox.PMin;
                            Vector3 max = box.BoundingBox.PMax;

                            min.X = min.X - increment <= aabb.PMin.X ? aabb.PMin.X : min.X - increment;
                            min.Y = min.Y - increment <= aabb.PMin.Y ? aabb.PMin.Y : min.Y - increment;
                            min.Z = min.Z - increment <= aabb.PMin.Z ? aabb.PMin.Z : min.Z - increment;

                            max.X = max.X + increment >= aabb.PMax.X ? aabb.PMax.X : max.X + increment;
                            max.Y = max.Y + increment >= aabb.PMax.Y ? aabb.PMax.Y : max.Y + increment;
                            max.Z = max.Z + increment >= aabb.PMax.Z ? aabb.PMax.Z : max.Z + increment;

                            box.setExtremes(min, max);
                            box.updateValues();
                        }


                        //dibujar
                        aabb.render();
                        box.render();
                    }
                }
            }

            /// <summary>
            /// Resetar tamaño de Boxes
            /// </summary>
            public void resetAabbBox()
            {
                for (int i = 0; i < conservativeAABBs.Count; i++)
                {
                    TgcBox box = aabbBoxes[i];
                    TgcBoundingBox aabb = conservativeAABBs[i];

                    Vector3 c = aabb.calculateBoxCenter();
                    box.setExtremes(c, c);
                    box.updateValues();
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
