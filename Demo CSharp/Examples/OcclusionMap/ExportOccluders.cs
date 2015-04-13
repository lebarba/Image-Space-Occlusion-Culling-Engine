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
using Examples.Voxelization2;
using System.Windows.Forms;

namespace Examples.OcclusionMap
{
    /// <summary>
    /// Genera occluders y los exporta a un xml de TGC
    /// </summary>
    public class ExportOccluders : TgcExample
    {

        const int MAX_AABBS = 4;

        List<TgcMesh> meshes;
        int index;
        TgcScene sceneOccluders;


        public override string getCategory()
        {
            return "OcclusionMap";
        }

        public override string getName()
        {
            return "Export Occluders";
        }

        public override string getDescription()
        {
            return "Export Occluders.";
        }

        public override void init()
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            //Cargar ciudad
            TgcSceneLoader loader = new TgcSceneLoader();
            //TgcScene scene = loader.loadSceneFromFile(GuiController.Instance.ExamplesMediaDir + "ModelosTgc\\CiudadGrande\\CiudadGrande-TgcScene.xml");
            TgcScene scene = loader.loadSceneFromFile(GuiController.Instance.ExamplesMediaDir + "ModelosTgc\\CiudadGrandeCerrada\\CiudadGrandeCerrada-TgcScene.xml");

            //Descartar el layer de Occluders
            meshes = new List<TgcMesh>();
            for (int i = 0; i < scene.Meshes.Count; i++)
            {
                TgcMesh mesh = scene.Meshes[i];
                if (mesh.Layer != "Occluders")
                {
                    meshes.Add(mesh);
                }
            }


            index = 0;
            //sceneOccluders = new TgcScene("CiudadGrandeOccluders", "CiudadGrandeOccluders.xml");
            sceneOccluders = new TgcScene("CiudadGrandeCerradaOccluders", "CiudadGrandeCerradaOccluders.xml");
        }


        public override void render(float elapsedTime)
        {
            if (index < meshes.Count)
            {
                TgcMesh mesh = meshes[index];
                index++;

                //Crear Occluders
                OccluderVoxelizer voxelizer = new OccluderVoxelizer();
                voxelizer.MaxAabbCount = MAX_AABBS;
                voxelizer.VoxelVolumePercent = 0.025f;
                voxelizer.MinVoxelSize = 20f;
                Voxel[, ,] voxels = voxelizer.voxelizeMesh(mesh);
                List<TgcBoundingBox> conservativeAABBs = voxelizer.buildConservativesAABB(voxels);

                //Agarrar hasta N AABBs maximo
                for (int j = 0; j < conservativeAABBs.Count && j < MAX_AABBS; j++)
                {
                    //Crear mesh de Occluder
                    TgcBox box = TgcBox.fromExtremes(conservativeAABBs[j].PMin, conservativeAABBs[j].PMax, Color.Green);
                    TgcMesh boxMesh = box.toMesh(mesh.Name + "_Occluder_" + j);
                    box.dispose();
                    sceneOccluders.Meshes.Add(boxMesh);
                }


                //Dispose de datos de este mesh
                for (int h = 0; h < voxels.GetLength(0); h++)
                {
                    for (int j = 0; j < voxels.GetLength(1); j++)
                    {
                        for (int k = 0; k < voxels.GetLength(2); k++)
                        {
                            voxels[h, j, k].dispose();
                        }
                    }
                }
                voxels = null;
                for (int j = 0; j < conservativeAABBs.Count; j++)
                {
                    conservativeAABBs[j].dispose();
                }
                conservativeAABBs = null;





                //Si termino, exportar a XML
                if (index == meshes.Count - 1)
                {
                    TgcSceneExporter exporter = new TgcSceneExporter();
                    //exporter.exportSceneToXml(sceneOccluders, GuiController.Instance.ExamplesMediaDir + "ModelosTgc\\CiudadGrandeOccluders\\");
                    exporter.exportSceneToXml(sceneOccluders, GuiController.Instance.ExamplesMediaDir + "ModelosTgc\\CiudadGrandeCerradaOccluders\\");
                    MessageBox.Show("Export OK");
                }
            }

            
        }
        

        public override void close()
        {
            //Dispose de meshes
            foreach (TgcMesh mesh in meshes)
            {
                mesh.dispose();
            }
        }

    }
}
