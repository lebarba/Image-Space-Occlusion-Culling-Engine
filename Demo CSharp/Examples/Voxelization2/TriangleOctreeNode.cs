using System;
using System.Collections.Generic;
using System.Text;
using TgcViewer.Utils.TgcGeometry;
using Microsoft.DirectX;

namespace Examples.Voxelization2
{
    /// <summary>
    /// Nodo de Octree de triangulos
    /// </summary>
    public class TriangleOctreeNode
    {
        const int MAX_LEVEL = 2;
        const int MIN_TRIANGLE_COUNT = 3;


        List<OccluderVoxelizer.Triangle> triangles;
        /// <summary>
        /// Triangulos del nodo
        /// </summary>
        public List<OccluderVoxelizer.Triangle> Triangles
        {
            get { return triangles; }
        }

        TriangleOctreeNode[] childNodes;
        /// <summary>
        /// Hijos
        /// </summary>
        public TriangleOctreeNode[] ChildNodes
        {
            get { return childNodes; }
        }

        TgcBoundingBox aabb;
        /// <summary>
        /// BoundingBox del nodo
        /// </summary>
        public TgcBoundingBox Aabb
        {
            get { return aabb; }
        }

        public TriangleOctreeNode()
        {
        }

        /// <summary>
        /// Construir nodo recursivamente
        /// </summary>
        /// <param name="parentTriangles">Triangulos</param>
        /// <param name="aabb">BoundingBox del nodo</param>
        /// <param name="level">nivel del nodo</param>
        public void build(List<OccluderVoxelizer.Triangle> parentTriangles, TgcBoundingBox aabb, int level)
        {
            this.aabb = aabb;

            //Detectar triangulos que están adentro
            List<OccluderVoxelizer.Triangle> trianglesInside = new List<OccluderVoxelizer.Triangle>();
            foreach (OccluderVoxelizer.Triangle tri in parentTriangles)
            {
                if (tri.testAabb(aabb))
                {
                    trianglesInside.Add(tri);
                }
            }

            //Hoja
            if (level == MAX_LEVEL || trianglesInside.Count < MIN_TRIANGLE_COUNT)
            {
                this.triangles = trianglesInside;
            }
            //Nodo intermedio
            else
            {
                childNodes = new TriangleOctreeNode[8];
                Vector3 halfSize = aabb.calculateAxisRadius();
                level++;

                //Recursividad con hijos
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        for (int k = 0; k < 2; k++)
                        {
                            //aabb
                            Vector3 s = new Vector3(i, j, k);
                            Vector3 min = aabb.PMin + TgcVectorUtils.mul(halfSize, s);
                            Vector3 max = min + halfSize;
                            TgcBoundingBox childAabb = new TgcBoundingBox(min, max);

                            TriangleOctreeNode childNode = new TriangleOctreeNode();
                            childNode.build(trianglesInside, childAabb, level);
                            childNodes[i * 4 + j * 2 + k] = childNode;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Indica si es hoja
        /// </summary>
        public bool isLeaf()
        {
            return childNodes == null;
        }

        /// <summary>
        /// Testear recursivamente colision entre el AABB y los triangulos del octree.
        /// Devuelve true ni bien encuentra uno
        /// </summary>
        public bool testAABBCollision(TgcBoundingBox aabb)
        {
            //Hoja
            if (isLeaf())
            {
                foreach (OccluderVoxelizer.Triangle tri in triangles)
                {
                    if (tri.testAabb(aabb))
                    {
                        return true;
                    }
                }
                return false;
            }
            //Nodo intermedio
            else
            {
                Vector3 cNode = this.aabb.calculateBoxCenter();
                Vector3 c = aabb.calculateBoxCenter();

                //Obtener indice
                int i = 0;
                i += c.X < cNode.X ? 0 : 4;
                i += c.Y < cNode.Y ? 0 : 2;
                i += c.Z < cNode.Z ? 0 : 1;

                return childNodes[i].testAABBCollision(aabb);
            }
        }
    }
}
