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

namespace Examples.Voxelization
{
    /// <summary>
    /// EjemploVoxelization
    /// </summary>
    public class EjemploVoxelization : TgcExample
    {

        TgcMesh mesh;
        string currentPath;
        OccluderVoxelizer.Voxel[, ,] voxels;
        List<TgcBoundingBox> conservativeAABBs;
        List<TgcBox> aabbBoxes;


        public override string getCategory()
        {
            return "Voxelization";
        }

        public override string getName()
        {
            return "AABB Voxelization";
        }

        public override string getDescription()
        {
            return "Voxelization";
        }

        public override void init()
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            //Malla default
            //string initialMeshFile = GuiController.Instance.ExamplesMediaDir + "OcclusionMap\\Auto\\Auto-TgcScene.xml";
            string initialMeshFile = GuiController.Instance.ExamplesMediaDir + "OcclusionMap\\Edificio\\Edificio-TgcScene.xml";

            //Modifiers
            currentPath = null;
            GuiController.Instance.Modifiers.addFile("Mesh", initialMeshFile, "-TgcScene.xml |*-TgcScene.xml");
            GuiController.Instance.Modifiers.addBoolean("showMesh", "showMesh", true);
            GuiController.Instance.Modifiers.addBoolean("showSurface", "showSurface", true);
            GuiController.Instance.Modifiers.addBoolean("showSolid", "showSolid", true);
            GuiController.Instance.Modifiers.addBoolean("showConservatibeAABB", "showSolid", true);
            GuiController.Instance.Modifiers.addInt("aabbCount", 0, 20, 20);

            //Camera
            GuiController.Instance.FpsCamera.Enable = true;
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
            mesh = loader.loadSceneFromFile(path).Meshes[0];

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



            //Dibujar mesh
            bool showMesh = (bool)GuiController.Instance.Modifiers["showMesh"];
            if (showMesh)
            {
                mesh.render();
                //mesh.BoundingBox.render();
            }

            //Dibujar conservatibeAABB
            bool showConservatibeAABB = (bool)GuiController.Instance.Modifiers["showConservatibeAABB"];
            int aabbCount = (int)GuiController.Instance.Modifiers["aabbCount"];
            if (showConservatibeAABB)
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

            //Dibujar voxels
            bool showSurface = (bool)GuiController.Instance.Modifiers["showSurface"];
            bool showSolid = (bool)GuiController.Instance.Modifiers["showSolid"];
            for (int i = 0; i < voxels.GetLength(0); i++)
            {
                for (int j = 0; j < voxels.GetLength(1); j++)
                {
                    for (int k = 0; k < voxels.GetLength(2); k++)
                    {
                        OccluderVoxelizer.Voxel voxel = voxels[i, j, k];
                        switch (voxel.type)
                        {
                            case OccluderVoxelizer.Voxel.VoxelType.Surface:
                                if (showSurface)
                                {
                                    voxel.mesh.render();
                                }
                                break;
                            case OccluderVoxelizer.Voxel.VoxelType.Solid:
                                if (showSolid)
                                {
                                    /*
                                    if (i == 3 && j == 8 && k == 3)
                                    {
                                        voxel.mesh.render();
                                    }*/
                                    voxel.mesh.render();
                                }
                                break;
                            case OccluderVoxelizer.Voxel.VoxelType.Empty:
                                break;
                        }
                    }
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

                for (int i = 0; i < conservativeAABBs.Count; i++)
			    {
                    conservativeAABBs[i].dispose();
                    aabbBoxes[i].dispose();
			    }
                conservativeAABBs = null;
                aabbBoxes = null;
            }
        }

    }
}
