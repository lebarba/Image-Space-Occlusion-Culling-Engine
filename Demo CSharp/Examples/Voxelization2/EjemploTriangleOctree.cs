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
using Examples.Voxelization2;

namespace Examples.Voxelization2
{
    /// <summary>
    /// EjemploTriangleOctree
    /// </summary>
    public class EjemploTriangleOctree : TgcExample
    {

        TgcMesh mesh;
        string currentPath;
        TriangleOctreeNode triOctreeRoot;


        public override string getCategory()
        {
            return "Voxelization2";
        }

        public override string getName()
        {
            return "Triangle-Octree";
        }

        public override string getDescription()
        {
            return "Triangle-Octree";
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
            GuiController.Instance.Modifiers.addBoolean("showAABB", "showAABB", true);

            //Camera
            GuiController.Instance.FpsCamera.Enable = true;
        }

        /// <summary>
        /// Cargar nuevo mesh para voxelizar
        /// </summary>
        private void loadMesh(string path)
        {
            //Limpiar mesh anterior
            if (mesh != null)
            {
                mesh.dispose();
                mesh = null;
            }

            //Cargar mesh
            TgcSceneLoader loader = new TgcSceneLoader();
            mesh = loader.loadSceneFromFile(path).Meshes[0];

            //Crear octree
            Vector3[] vertices = mesh.getVertexPositions();
            int triCount = vertices.Length / 3;
            List<OccluderVoxelizer.Triangle> triangles = new List<OccluderVoxelizer.Triangle>(triCount);
            for (int i = 0; i < triCount; i++)
            {
                Vector3 v1 = vertices[i * 3];
                Vector3 v2 = vertices[i * 3 + 1];
                Vector3 v3 = vertices[i * 3 + 2];
                triangles.Add(new OccluderVoxelizer.Triangle(v1, v2, v3));
            }
            triOctreeRoot = new TriangleOctreeNode();
            triOctreeRoot.build(triangles, mesh.BoundingBox, 0);
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
            bool showAABB = (bool)GuiController.Instance.Modifiers["showAABB"];
            if (showAABB)
            {
                renderOctreeNode(triOctreeRoot);
            }

            
        }

        private void renderOctreeNode(TriangleOctreeNode node)
        {
            node.Aabb.render();
            if (!node.isLeaf())
            {
                foreach (TriangleOctreeNode c in node.ChildNodes)
                {
                    renderOctreeNode(c);
                }
            }
        }




        public override void close()
        {
            if (mesh != null)
            {
                mesh.dispose();
                mesh = null;
            }
        }


    }
}
