using System;
using System.Collections.Generic;
using System.Text;
using TgcViewer.Utils.TgcSceneLoader;
using Microsoft.DirectX;
using TgcViewer.Utils.TgcGeometry;
using System.Drawing;
using Examples.OcclusionMap;

namespace Examples.Voxelization
{
    /// <summary>
    /// Utilidad para Voxelizar un mesh y construir BoundingBox conservativas a partir
    /// de este
    /// </summary>
    public class OccluderVoxelizer
    {

        const float VOXEL_VOLUME_PERCENT = 0.04f;
        const float MIN_AABB_VOLUME_FACTOR = 2;
        //const float MAX_AABB_COUNT = 10;
        const float MAX_AABB_COUNT = 20;

        float voxelSize;
        float minAabbVolume;

        public OccluderVoxelizer()
        {
        }


        /// <summary>
        /// Crear una representacion de voxels a partir de un mesh.
        /// Se crean voxels del tipo Surface-voxel y Solid-voxel.
        /// </summary>
        /// <param name="mesh">Mesh a voxelizar</param>
        /// <returns>Voxels creados</returns>
        public Voxel[,,] voxelizeMesh(TgcMesh mesh)
        {
            //Crear array de AABB de triangulos
            Vector3[] vertices = mesh.getVertexPositions();
            int triCount = vertices.Length / 3;
            TgcBoundingBox[] trianglesAABB = new TgcBoundingBox[triCount];
            for (int i = 0; i < triCount; i++)
            {
                Vector3 v1 = vertices[i * 3];
                Vector3 v2 = vertices[i * 3 + 1];
                Vector3 v3 = vertices[i * 3 + 2];

                trianglesAABB[i] = TgcBoundingBox.computeFromPoints(new Vector3[] { v1, v2, v3 });
            }

            //Crear matriz de voxels
            Vector3 meshSize = mesh.BoundingBox.calculateSize();
            float maxAxis = TgcCollisionUtils.max(meshSize.X, meshSize.Y, meshSize.Z);
            //voxelSize = FastMath.Ceiling(maxAxis * VOXEL_VOLUME_PERCENT);
            voxelSize = 10;
            minAabbVolume = (voxelSize * voxelSize * voxelSize) * MIN_AABB_VOLUME_FACTOR;
            int voxelsCountX = (int)(meshSize.X / voxelSize);
            int voxelsCountY = (int)(meshSize.Y / voxelSize);
            int voxelsCountZ = (int)(meshSize.Z / voxelSize);
            Voxel[,,] voxels = new Voxel[voxelsCountX, voxelsCountY, voxelsCountZ];

            //Crear surface voxels
            createSurfaceVoxels(mesh, vertices, trianglesAABB, voxels);

            //Dispose trianglesAABB
            foreach (TgcBoundingBox b in trianglesAABB)
            {
                b.dispose();
            }
            trianglesAABB = null;

            //Crear solid voxels
            createSolidVoxels(voxels);

            return voxels;
        }

        /// <summary>
        /// Crear AABB conservativos a partir del mesh voxelizado
        /// </summary>
        public List<TgcBoundingBox> buildConservativesAABB(Voxel[,,] voxels)
        {
            //Crear AABBs
            return createAllConservatibeAABBs(voxels);
        }

        /// <summary>
        /// Crear surface voxels.
        /// Son los que colisionan contra los triangulos del mesh
        /// </summary>
        private void createSurfaceVoxels(TgcMesh mesh, Vector3[] vertices, TgcBoundingBox[] trianglesAABB, Voxel[,,] voxels)
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
                        Vector3 min = mesh.BoundingBox.PMin + new Vector3(i * voxelSize, j * voxelSize, k * voxelSize);
                        Vector3 max = min + new Vector3(voxelSize, voxelSize, voxelSize);
                        voxel.aabb = new TgcBoundingBox(min, max);

                        //Testear colision contra todos los triangulos
                        if (voxelCollideMesh(trianglesAABB, vertices, voxel.aabb))
                        {
                            //Marcar como surface-voxel
                            voxel.type = Voxel.VoxelType.Surface;
                            voxel.mesh = TgcBox.fromExtremes(voxel.aabb.PMin, voxel.aabb.PMax, Utils.getRandomRedColor());
                        }
                        else
                        {
                            //Marcar como vacio
                            voxel.type = Voxel.VoxelType.Empty;
                        }

                    }
                }
            }
        }

        /// <summary>
        /// Crear solid voxels.
        /// Son los que estan dentro de los surface voxels.
        /// Son el interior del mesh.
        /// </summary>
        private void createSolidVoxels(Voxel[,,] voxels)
        {
            Voxel initVoxel = getSolidCenterVoxel(voxels);

            initVoxel.type = Voxel.VoxelType.Solid;
            initVoxel.mesh = TgcBox.fromExtremes(initVoxel.aabb.PMin, initVoxel.aabb.PMax, Utils.getRandomBlueColor());

            //Ir expandiendo los voxels interiores
            Queue<Voxel> open = new Queue<Voxel>();
            open.Enqueue(initVoxel);
            while (open.Count > 0)
            {
                //Expandir a los 6 vecinos del voxel (si se puede)
                Voxel voxel = open.Dequeue();

                //X
                addNeighbor(voxels, voxel.i - 1, voxel.j, voxel.k, open);
                addNeighbor(voxels, voxel.i + 1, voxel.j, voxel.k, open);

                //Y
                addNeighbor(voxels, voxel.i, voxel.j - 1, voxel.k, open);
                addNeighbor(voxels, voxel.i, voxel.j + 1, voxel.k, open);

                //Z
                addNeighbor(voxels, voxel.i, voxel.j, voxel.k - 1, open);
                addNeighbor(voxels, voxel.i, voxel.j, voxel.k + 1, open);
            }
        }

        /// <summary>
        /// Voxel central
        /// </summary>
        private Voxel getSolidCenterVoxel(Voxel[,,] voxels)
        {
            //TODO: Hacer esto mas robusto!!!!
            return voxels[voxels.GetLength(0) / 2, voxels.GetLength(1) / 2, voxels.GetLength(2) / 2];
        }

        /// <summary>
        /// Agregar vecino a lista de abierto, si es que estaba libre
        /// </summary>
        private void addNeighbor(Voxel[, ,] voxels, int i, int j, int k, Queue<Voxel> open)
        {
            //Controlar extremos
            if (i < 0 || i >= voxels.GetLength(0) || j < 0 || j >= voxels.GetLength(1) || k < 0 || k >= voxels.GetLength(2))
                return;

            Voxel neighbor = voxels[i, j, k];
            if (neighbor.type == Voxel.VoxelType.Empty)
            {
                //Marcar como solido
                neighbor.type = Voxel.VoxelType.Solid;
                neighbor.mesh = TgcBox.fromExtremes(neighbor.aabb.PMin, neighbor.aabb.PMax, Utils.getRandomBlueColor());

                //Agregar
                open.Enqueue(neighbor);
            }
        }

        /// <summary>
        /// Buscar el solid-voxel mas denso, que aun no haya sido utilizado.
        /// Se calcula como el voxel que se encuentra a mayor distancia
        /// de todos los surface voxels
        /// </summary>
        private Voxel findDensestVoxel(Voxel[,,] voxels)
        {
            float maxDistPromedy = float.MinValue;
            Voxel densestVoxel = null;

            for (int i = 0; i < voxels.GetLength(0); i++)
            {
                for (int j = 0; j < voxels.GetLength(1); j++)
                {
                    for (int k = 0; k < voxels.GetLength(2); k++)
                    {
                        Voxel voxel = voxels[i, j, k];
                        if (voxel.type == Voxel.VoxelType.Solid && !voxel.used)
                        {
                            float distProm = getDistancePromedyFromSurface(voxels, voxel);
                            if (distProm > maxDistPromedy)
                            {
                                maxDistPromedy = distProm;
                                densestVoxel = voxel;
                            }
                        }
                    }
                }
            }

            return densestVoxel;
        }

        /// <summary>
        /// Calcula la distancia promedio entre un voxel y todos los surface-voxels
        /// </summary>
        private float getDistancePromedyFromSurface(Voxel[,,] voxels, Voxel voxel)
        {
            Vector3 voxelCenter = voxel.aabb.calculateBoxCenter();
            float distSum = 0;
            int voxelCount = 0;
            for (int i = 0; i < voxels.GetLength(0); i++)
            {
                for (int j = 0; j < voxels.GetLength(1); j++)
                {
                    for (int k = 0; k < voxels.GetLength(2); k++)
                    {
                        Voxel v = voxels[i, j, k];
                        if (v.type == Voxel.VoxelType.Surface)
                        {
                            Vector3 center = v.aabb.calculateBoxCenter();
                            distSum += Vector3.LengthSq(center - voxelCenter);
                            voxelCount++;
                        }
                    }
                }
            }

            return distSum / voxelCount;
        }


        /// <summary>
        /// Testear si un voxel colisiona con algun triangulo del mesh
        /// </summary>
        private bool voxelCollideMesh(TgcBoundingBox[] trianglesAABB, Vector3[] vertices, TgcBoundingBox voxelBB)
        {
            int triCount = vertices.Length / 3;
            for (int i = 0; i < triCount; i++)
            {
                //Triangle AABB test
                if (TgcCollisionUtils.testAABBAABB(trianglesAABB[i], voxelBB))
                {
                    if (TgcCollisionUtils.testTriangleAABB(vertices[i * 3], vertices[i * 3 + 1], vertices[i * 3 + 2], voxelBB))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Crear BoundingBox conservativas a partir de los solid-voxels.
        /// Crear la menor cantidad posible y con el mayor tamaño posible.
        /// </summary>
        private List<TgcBoundingBox> createAllConservatibeAABBs(Voxel[,,] voxels)
        {
            //Crear N BoundingBox hasta llegar a un limite de tamaño o de cantidad de boxes
            List<TgcBoundingBox> boxes = new List<TgcBoundingBox>();
            VoxelBoxSolution solution;
            while (boxes.Count < MAX_AABB_COUNT)
            {
                //Crear aabb
                TgcBoundingBox aabb = createConservativeAABB(voxels, out solution);

                //Umbral de volumen
                if (solution.volume <= minAabbVolume)
                {
                    break;
                }

                boxes.Add(aabb);
            }

            return boxes;
        }

        /// <summary>
        /// Crear un AABB conservativo que crece hasta chocar con los surface-voxels
        /// </summary>
        private TgcBoundingBox createConservativeAABB(Voxel[,,] voxels, out VoxelBoxSolution bestSol)
        {
            //Voxel initVoxel = getSolidCenterVoxel();
            Voxel initVoxel = findDensestVoxel(voxels);

            List<VoxelBoxSolution> open = new List<VoxelBoxSolution>();
            open.Add(new VoxelBoxSolution(initVoxel, initVoxel));

            //Buscar la mejor solucion
            bool expansionTest = true;
            while (expansionTest)
            {
                expansionTest = false;

                //Agarrar el primero que es la solucion con mas volumen
                VoxelBoxSolution sol = open[0];

                //X
                expansionTest |= addVoxelBoxSolution(voxels, open, sol.voxelInit.i - 1, sol.voxelInit.j, sol.voxelInit.k, sol.voxelEnd.i, sol.voxelEnd.j, sol.voxelEnd.k);
                expansionTest |= addVoxelBoxSolution(voxels, open, sol.voxelInit.i, sol.voxelInit.j, sol.voxelInit.k, sol.voxelEnd.i + 1, sol.voxelEnd.j, sol.voxelEnd.k);

                //Y
                expansionTest |= addVoxelBoxSolution(voxels, open, sol.voxelInit.i, sol.voxelInit.j - 1, sol.voxelInit.k, sol.voxelEnd.i, sol.voxelEnd.j, sol.voxelEnd.k);
                expansionTest |= addVoxelBoxSolution(voxels, open, sol.voxelInit.i, sol.voxelInit.j, sol.voxelInit.k, sol.voxelEnd.i, sol.voxelEnd.j + 1, sol.voxelEnd.k);

                //Z
                expansionTest |= addVoxelBoxSolution(voxels, open, sol.voxelInit.i, sol.voxelInit.j, sol.voxelInit.k - 1, sol.voxelEnd.i, sol.voxelEnd.j, sol.voxelEnd.k);
                expansionTest |= addVoxelBoxSolution(voxels, open, sol.voxelInit.i, sol.voxelInit.j, sol.voxelInit.k, sol.voxelEnd.i, sol.voxelEnd.j, sol.voxelEnd.k + 1);

                //Ordenar por volumen DESC
                open.Sort();
            }

            //Tomar mejor solucion
            bestSol = open[0];
            open.Clear();

            //Marcar todos los voxels de esta solucion como utilizados
            for (int i = bestSol.voxelInit.i; i <= bestSol.voxelEnd.i; i++)
            {
                for (int j = bestSol.voxelInit.j; j <= bestSol.voxelEnd.j; j++)
                {
                    for (int k = bestSol.voxelInit.k; k <= bestSol.voxelEnd.k; k++)
                    {
                        voxels[i, j, k].used = true;
                    }
                }
            }

            //Crear BoundingBox
            TgcBoundingBox aabb = new TgcBoundingBox(bestSol.voxelInit.aabb.PMin, bestSol.voxelEnd.aabb.PMax);
            aabb.setRenderColor(Color.Green);
            return aabb;
        }

        /// <summary>
        /// Crear nueva solucion de AABB, si es posible.
        /// Devuelve true si pudo agregar
        /// </summary>
        private bool addVoxelBoxSolution(Voxel[,,] voxels, List<VoxelBoxSolution> open, int i1, int j1, int k1, int i2, int j2, int k2)
        {
            //Controlar extremos 1
            if (i1 < 0 || i1 >= voxels.GetLength(0) || j1 < 0 || j1 >= voxels.GetLength(1) || k1 < 0 || k1 >= voxels.GetLength(2))
                return false;

            //Controlar extremos 2
            if (i2 < 0 || i2 >= voxels.GetLength(0) || j2 < 0 || j2 >= voxels.GetLength(1) || k2 < 0 || k2 >= voxels.GetLength(2))
                return false;

            //Testear que todos los voxels abarcados sean del tipo solid-voxels
            Voxel vInit = voxels[i1, j1, k1];
            Voxel vEnd = voxels[i2, j2, k2];
            for (int i = i1; i <= i2; i++)
            {
                for (int j = j1; j <= j2; j++)
                {
                    for (int k = k1; k <= k2; k++)
                    {
                        Voxel v = voxels[i, j, k];
                        if (v.type != Voxel.VoxelType.Solid || v.used)
                        {
                            return false;
                        }
                    }
                }
            }

            //Agregar solucion
            open.Add(new VoxelBoxSolution(vInit, vEnd));
            return true;
        }




        /// <summary>
        /// Voxel
        /// </summary>
        public class Voxel
        {
            public enum VoxelType
            {
                Surface,
                Solid,
                Empty,
            }


            public TgcBoundingBox aabb;
            public VoxelType type;
            public TgcBox mesh;
            public int i;
            public int j;
            public int k;
            public bool used;

            public void dispose()
            {
                if (aabb != null)
                {
                    aabb.dispose();
                    aabb = null;
                }

                if (mesh != null)
                {
                    mesh.dispose();
                    mesh = null;
                }
            }
        }

        /// <summary>
        /// Estructura auxiliar para posibles soluciones de AABB conservativas que engloban los
        /// solid-voxels
        /// </summary>
        public class VoxelBoxSolution : IComparable
        {
            public VoxelBoxSolution(Voxel v1, Voxel v2)
            {
                voxelInit = v1;
                voxelEnd = v2;
                Vector3 size = voxelEnd.aabb.PMax - voxelInit.aabb.PMin;
                volume = size.X * size.Y * size.Z;
            }

            public Voxel voxelInit;
            public Voxel voxelEnd;
            public float volume;

            /// <summary>
            /// Gana el que mas volumen tiene
            /// </summary>
            public int CompareTo(object obj)
            {
                VoxelBoxSolution sol2 = (VoxelBoxSolution)obj;
                return this.volume.CompareTo(sol2.volume) * -1;
            }

            public override string ToString()
            {
                return "Volume: " + volume;
            }
        }

    }
}
