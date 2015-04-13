using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX;
using TgcViewer.Utils.TgcGeometry;
using TgcViewer.Utils.TgcSceneLoader;
using Examples.OcclusionMap;
using System.Drawing;

namespace Examples.Voxelization2
{
    /// <summary>
    /// Utilidad para Voxelizar un mesh y construir BoundingBox conservativas a partir
    /// de este
    /// </summary>
    public class OccluderVoxelizer
    {

        const float VOXEL_VOLUME_PERCENT = 0.05f;
        const float MIN_VOXEL_AXIS_DIM = 1;
        const float MESH_AABB_EPSILON_INCREMENT = 1f;
        const int MAX_AABB_COUNT = 10;
        const float MIN_AABB_VOLUME_IN_VOXELS = 4;
        const float MAX_TESSELLATE_LEVEL = 3;


        public List<Triangle> tessellatedTriangles;

        float minVoxelSize;
        /// <summary>
        /// Tamaño minimo tolerado de un lado del voxel
        /// </summary>
        public float MinVoxelSize
        {
            get { return minVoxelSize; }
            set { minVoxelSize = value; }
        }

        float voxelVolumePercent;
        /// <summary>
        /// Porcentaje del volumen total del AABB a partir del cual se saca
        /// el tamaño de un voxel
        /// </summary>
        public float VoxelVolumePercent
        {
            get { return voxelVolumePercent; }
            set { voxelVolumePercent = value; }
        }

        int maxAabbCount;
        /// <summary>
        /// Maxima cantidad de AABB que se intentan generar
        /// </summary>
        public int MaxAabbCount
        {
            get { return maxAabbCount; }
            set { maxAabbCount = value; }
        }


        public OccluderVoxelizer()
        {
            minVoxelSize = MIN_VOXEL_AXIS_DIM;
            voxelVolumePercent = VOXEL_VOLUME_PERCENT;
            maxAabbCount = MAX_AABB_COUNT;
        }

        /// <summary>
        /// Crear una representacion de voxels a partir de un mesh.
        /// Se crean voxels del tipo Surface-voxel y Solid-voxel.
        /// </summary>
        /// <param name="mesh">Mesh a voxelizar</param>
        /// <returns>Voxels creados</returns>
        public Voxel[, ,] voxelizeMesh(TgcMesh mesh)
        {
            //Crear array de triangulos
            Vector3[] vertices = mesh.getVertexPositions();
            int triCount = vertices.Length / 3;
            List<Triangle> triangles = new List<Triangle>(triCount);
            for (int i = 0; i < triCount; i++)
            {
                Vector3 v1 = vertices[i * 3];
                Vector3 v2 = vertices[i * 3 + 1];
                Vector3 v3 = vertices[i * 3 + 2];
                triangles.Add(new Triangle(v1, v2, v3));
            }

            //Tamaño de voxel
            Vector3 meshSize = mesh.BoundingBox.calculateSize();
            Vector3 voxelSize = meshSize * voxelVolumePercent;

            /*
            //Voxel con tamaño a partir del menor lado del mesh
            float m = TgcVectorUtils.min(voxelSize);
            m = m < minVoxelSize ? minVoxelSize : m;
            voxelSize = new Vector3(m, m, m);
            */

            if (voxelSize.X < minVoxelSize) voxelSize.X = minVoxelSize;
            if (voxelSize.Y < minVoxelSize) voxelSize.Y = minVoxelSize;
            if (voxelSize.Z < minVoxelSize) voxelSize.Z = minVoxelSize;

            //Dividir triangulos de forma tal que queden del tamaño de un cuarto de voxel aprox
            Vector3 tessellateVoxelSize = voxelSize /** 0.25f*/;
            float tessellateEdgeLength = TgcVectorUtils.min(tessellateVoxelSize);
            //List<Triangle> tessellatedTriangles = tesselleteTriangles(triangles, tessellateEdgeLength);
            tessellatedTriangles = tesselleteTriangles(triangles, tessellateEdgeLength);

            
            //Crear Octree de triangulos
            TriangleOctreeNode triOctreeRoot = new TriangleOctreeNode();
            triOctreeRoot.build(tessellatedTriangles, mesh.BoundingBox, 0);
            triangles = null;
            


            //Crear matriz de voxels
            int voxelsCountX = (int)(meshSize.X / voxelSize.X) + 1;
            int voxelsCountY = (int)(meshSize.Y / voxelSize.Y) + 1;
            int voxelsCountZ = (int)(meshSize.Z / voxelSize.Z) + 1;
            Voxel[, ,] voxels = new Voxel[voxelsCountX, voxelsCountY, voxelsCountZ];
            
            //Crear surface voxels
            Vector3 initPos = mesh.BoundingBox.PMin;
            createSurfaceVoxels(triOctreeRoot, voxels, voxelSize, initPos);
            //deleteInsideSurfaceVoxels(voxels);

            //Crear inner voxels
            createInnerVoxels(voxels);


            return voxels;
        }

        

        /// <summary>
        /// Dividir todos los triangulos recursivamente en 4 triangulos hasta llegar al umbral de corte
        /// </summary>
        private List<Triangle> tesselleteTriangles(List<Triangle> origTriangles, float edgeLength)
        {
            List<Triangle> tessellatedTriangles = new List<Triangle>();
            foreach (Triangle t in origTriangles)
            {
                divideTriangle(t.a, t.b, t.c, 0, tessellatedTriangles, edgeLength);
            }
            return tessellatedTriangles;
        }

        /// <summary>
        /// Divide un triangulo en 4 recursivamente hasta que se llego al maximo nivel de recursividad o hasta
        /// que las aristas no superan el limite de maximo tamaño de arista.
        /// </summary>
        private void divideTriangle(Vector3 a, Vector3 b, Vector3 c, int level, List<Triangle> tessellatedTriangles, float edgeLength)
        {
            float lengthAB = Vector3.Length(a - b);
            float lengthAC = Vector3.Length(a - c);
            float lengthBC = Vector3.Length(b - c);

            //Umbral o todas las aristas son menores al limite de tamaño de arista
            if (level >= MAX_TESSELLATE_LEVEL || lengthAB <= edgeLength || lengthAC <= edgeLength || lengthBC <= edgeLength)
            {
                Triangle t = new Triangle(a, b, c);
                tessellatedTriangles.Add(t);
            }
            //Dividir en 4
            else
            {
                Vector3 halfAB = a + (b - a) * 0.5f;
                Vector3 halfAC = a + (c - a) * 0.5f;
                Vector3 halfBC = b + (c - b) * 0.5f;
                level++;

                //Recursividad
                divideTriangle(halfAB, b, halfBC, level, tessellatedTriangles, edgeLength);
                divideTriangle(halfBC, c, halfAC, level, tessellatedTriangles, edgeLength);
                divideTriangle(halfAC, a, halfAB, level, tessellatedTriangles, edgeLength);
                divideTriangle(halfAB, halfBC, halfAC, level, tessellatedTriangles, edgeLength);
            }
        }

        /// <summary>
        /// Crear AABB conservativos a partir del mesh voxelizado
        /// </summary>
        public List<TgcBoundingBox> buildConservativesAABB(Voxel[, ,] voxels)
        {
            //Determinar volumen minimo tolerado de un AABB
            Vector3 voxelSize = voxels[0, 0, 0].aabb.calculateSize();
            float voxelVolume = voxelSize.X * voxelSize.Y * voxelSize.Z;
            float minAabbVolume = voxelVolume * MIN_AABB_VOLUME_IN_VOXELS;

            //Crear AABBs
            return createAllConservatibeAABBs(voxels, minAabbVolume);
        }


        /// <summary>
        /// Crear surface voxels.
        /// Son los que colisionan contra los triangulos del mesh
        /// </summary>
        private void createSurfaceVoxels(TriangleOctreeNode triOctreeRoot, Voxel[, ,] voxels, Vector3 voxelSize, Vector3 initPos)
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
                        if (triOctreeRoot.testAABBCollision(voxel.aabb))
                        {
                            //Marcar como surface-voxel
                            voxel.type = Voxel.VoxelType.Surface;
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
        /// Elimina los surface voxels interiores. Dado un surface voxel busca si tiene otro
        /// surface voxels al lado en las 6 direcciones. Si es así ya se puede marcar como Inner Voxel.
        /// 
        /// NO FUNCIONA!!! REPENSAR
        /// 
        /// </summary>
        private void deleteInsideSurfaceVoxels(Voxel[, ,] voxels)
        {
            for (int i = 0; i < voxels.GetLength(0); i++)
            {
                for (int j = 0; j < voxels.GetLength(1); j++)
                {
                    for (int k = 0; k < voxels.GetLength(2); k++)
                    {
                        //Ver si el voxel Surface esta completamente adentro
                        Voxel voxel = voxels[i, j, k];
                        if (voxel.type == Voxel.VoxelType.Surface)
                        {
                            //Ver si tiene un SurfaceVoxel al lado en las 6 direcciones

                            //X
                            if (i - 1 >= 0 && voxels[i - 1, j, k].type != Voxel.VoxelType.Surface) continue;
                            if (i + 1 < voxels.GetLength(0) && voxels[i + 1, j, k].type != Voxel.VoxelType.Surface) continue;

                            //Y
                            if (j - 1 >= 0 && voxels[i, j - 1, k].type != Voxel.VoxelType.Surface) continue;
                            if (j + 1 < voxels.GetLength(1) && voxels[i, j + 1, k].type != Voxel.VoxelType.Surface) continue;

                            //Z
                            if (k - 1 >= 0 && voxels[i, j, k - 1].type != Voxel.VoxelType.Surface) continue;
                            if (k + 1 < voxels.GetLength(2) && voxels[i, j, k + 1].type != Voxel.VoxelType.Surface) continue;

                            //Tiene SurfaceVoxel en las 6 direcciones, entonces es un InnerVoxel
                            voxel.type = Voxel.VoxelType.Empty; 
                            //TODO: revisar si es correcto pasarlo a Inner o primero a Empty
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Crear voxels interiores
        /// </summary>
        private void createInnerVoxels(Voxel[, ,] voxels)
        {
            for (int i = 0; i < voxels.GetLength(0); i++)
            {
                for (int j = 0; j < voxels.GetLength(1); j++)
                {
                    for (int k = 0; k < voxels.GetLength(2); k++)
                    {
                        //Ver si el voxel Empty es un InnerVoxel
                        Voxel voxel = voxels[i, j, k];
                        if(voxel.type == Voxel.VoxelType.Empty)
                        {
                            //Controlar que tenga un SurfaceVoxel en las 6 direcciones

                            //X
                            if (!detectSurfaceVoxel(voxels, i, j, k, 1, 0, 0)) continue;
                            if (!detectSurfaceVoxel(voxels, i, j, k, -1, 0, 0)) continue;

                            //Y
                            if (!detectSurfaceVoxel(voxels, i, j, k, 0, 1, 0)) continue;
                            if (!detectSurfaceVoxel(voxels, i, j, k, 0, -1, 0)) continue;

                            //Z
                            if (!detectSurfaceVoxel(voxels, i, j, k, 0, 0, 1)) continue;
                            if (!detectSurfaceVoxel(voxels, i, j, k, 0, 0, -1)) continue;

                            //Tiene SurfaceVoxel en las 6 direcciones, entonces es un InnerVoxel
                            voxel.type = Voxel.VoxelType.Inner;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Busca en la matriz de voxels si hay un surface voxel partiendo de la posicion indicada (initX, initY, initZ) y avanzando
        /// en la direccion indicada incrementalmente (incX, incY, incZ).
        /// Corta ni bien encuentra uno o si se sale de los limites de la matriz
        /// </summary>
        private bool detectSurfaceVoxel(Voxel[, ,] voxels, int initX, int initY, int initZ, int incX, int incY, int incZ)
        {
            //Se asume que el voxel inicial no es Surface

            int i = initX + incX;
            int j = initY + incY;
            int k = initZ + incZ;

            //Recorrer hasta salir de la matriz o encontrar un SurfaceVoxel
            while (i >= 0 && i < voxels.GetLength(0) && j >= 0 && j < voxels.GetLength(1) && k >= 0 && k < voxels.GetLength(2))
            {
                if (voxels[i, j, k].type == Voxel.VoxelType.Surface)
                {
                    return true;
                }

                //Avanzar
                i += incX;
                j += incY;
                k += incZ;
            }

            //No se encontró ninguno
            return false;
        }

        /// <summary>
        /// Crear BoundingBox conservativas a partir de los InnerVoxels.
        /// Crear la menor cantidad posible y con el mayor tamaño posible.
        /// </summary>
        private List<TgcBoundingBox> createAllConservatibeAABBs(Voxel[, ,] voxels, float minAabbVolume)
        {
            //Calcular densidad de InnerVoxels
            computeInnerVoxelsDensity(voxels);

            //Crear N BoundingBox hasta llegar a un limite de tamaño o de cantidad de boxes
            List<TgcBoundingBox> boxes = new List<TgcBoundingBox>();
            VoxelBoxSolution solution;
            while (boxes.Count < MAX_AABB_COUNT)
            {
                //Crear aabb
                TgcBoundingBox aabb = createConservativeAABB(voxels, out solution);

                //Sin mas InnerVoxels o se llego al umbral de volumen minimo
                if (solution == null || solution.volume <= minAabbVolume)
                {
                    break;
                }

                boxes.Add(aabb);
            }

            return boxes;
        }

        /// <summary>
        /// Calcula un número para cada voxel que indica su densidad. Se calcula como que tan alejado de los SurfaceVoxel
        /// de las 6 direcciones.
        /// </summary>
        private void computeInnerVoxelsDensity(Voxel[, ,] voxels)
        {
            for (int i = 0; i < voxels.GetLength(0); i++)
            {
                for (int j = 0; j < voxels.GetLength(1); j++)
                {
                    for (int k = 0; k < voxels.GetLength(2); k++)
                    {
                        //Calcular densidad para InnerVoxels
                        Voxel voxel = voxels[i, j, k];
                        if (voxel.type == Voxel.VoxelType.Inner)
                        {
                            voxel.density = 0;

                            //X
                            voxel.density += distanceToSurfaceVoxel(voxels, i, j, k, 1, 0, 0);
                            voxel.density += distanceToSurfaceVoxel(voxels, i, j, k, -1, 0, 0);

                            //Y
                            voxel.density += distanceToSurfaceVoxel(voxels, i, j, k, 0, 1, 0);
                            voxel.density += distanceToSurfaceVoxel(voxels, i, j, k, 0, -1, 0);

                            //Z
                            voxel.density += distanceToSurfaceVoxel(voxels, i, j, k, 0, 0, 1);
                            voxel.density += distanceToSurfaceVoxel(voxels, i, j, k, 0, 0, -1);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Busca a cuantos slots de la matriz de voxels se encuentra un SurfaceVoxel. Parte de la posicion inicial indicada
        /// (initX, initY, initZ) y se mueve incrementalmente segun (incX, incY, incZ)
        /// Devuelve la cantidad de slots recorridos antes de encontrar un SurfaceVoxel.
        /// </summary>
        private int distanceToSurfaceVoxel(Voxel[, ,] voxels, int initX, int initY, int initZ, int incX, int incY, int incZ)
        {
            //Se asume que el voxel inicial no es Surface

            int i = initX + incX;
            int j = initY + incY;
            int k = initZ + incZ;

            //Recorrer hasta salir de la matriz o encontrar un SurfaceVoxel
            int n = 0;
            while (i >= 0 && i < voxels.GetLength(0) && j >= 0 && j < voxels.GetLength(1) && k >= 0 && k < voxels.GetLength(2))
            {
                //Salimos si encontramos algo distinto de un InnerVoxel
                if (voxels[i, j, k].type != Voxel.VoxelType.Inner)
                {
                    return n;
                }

                //Avanzar
                i += incX;
                j += incY;
                k += incZ;
                n++;
            }

            return n;
        }

        /// <summary>
        /// Crear un AABB conservativo que crece hasta chocar con los surface-voxels.
        /// Retorna NULL si ya no queda ningun InnerVoxel
        /// </summary>
        private TgcBoundingBox createConservativeAABB(Voxel[, ,] voxels, out VoxelBoxSolution bestSol)
        {
            Voxel initVoxel = getDensestInnerVoxel(voxels);
            if (initVoxel == null)
            {
                bestSol = null;
                return null;
            }

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
        /// Buscar el InnerVoxel mas denso aun no utilizado.
        /// Null si no hay ninguno
        /// </summary>
        private Voxel getDensestInnerVoxel(Voxel[, ,] voxels)
        {
            float maxDensity = float.MinValue;
            Voxel maxVoxel = null;

            for (int i = 0; i < voxels.GetLength(0); i++)
            {
                for (int j = 0; j < voxels.GetLength(1); j++)
                {
                    for (int k = 0; k < voxels.GetLength(2); k++)
                    {
                        //Quedarse con el InnerVoxel con mayor densidad, que aun no se haya usado
                        Voxel voxel = voxels[i, j, k];
                        if (voxel.type == Voxel.VoxelType.Inner && !voxel.used)
                        {
                            if (voxel.density > maxDensity)
                            {
                                maxDensity = voxel.density;
                                maxVoxel = voxel;
                            }
                        }
                    }
                }
            }

            return maxVoxel;
        }

        /// <summary>
        /// Crear nueva solucion de AABB, si es posible.
        /// Devuelve true si pudo agregar
        /// </summary>
        private bool addVoxelBoxSolution(Voxel[, ,] voxels, List<VoxelBoxSolution> open, int i1, int j1, int k1, int i2, int j2, int k2)
        {
            //Controlar extremos 1
            if (i1 < 0 || i1 >= voxels.GetLength(0) || j1 < 0 || j1 >= voxels.GetLength(1) || k1 < 0 || k1 >= voxels.GetLength(2))
                return false;

            //Controlar extremos 2
            if (i2 < 0 || i2 >= voxels.GetLength(0) || j2 < 0 || j2 >= voxels.GetLength(1) || k2 < 0 || k2 >= voxels.GetLength(2))
                return false;

            //Testear que todos los voxels abarcados sean del tipo InnerVoxels
            Voxel vInit = voxels[i1, j1, k1];
            Voxel vEnd = voxels[i2, j2, k2];
            for (int i = i1; i <= i2; i++)
            {
                for (int j = j1; j <= j2; j++)
                {
                    for (int k = k1; k <= k2; k++)
                    {
                        Voxel v = voxels[i, j, k];
                        if (v.type != Voxel.VoxelType.Inner || v.used)
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
        /// Triangulo
        /// </summary>
        public class Triangle
        {
            const float AABB_EPSILON = 1f;

            public Vector3 a;
            public Vector3 b;
            public Vector3 c;
            public TgcBoundingBox aabb;

            public Triangle(Vector3 a, Vector3 b, Vector3 c)
            {
                this.a = a;
                this.b = b;
                this.c = c;

                this.aabb = TgcBoundingBox.computeFromPoints(new Vector3[] { a, b, c });
                Vector3 diff = new Vector3(AABB_EPSILON, AABB_EPSILON, AABB_EPSILON);
                this.aabb.setExtremes(aabb.PMin - diff, aabb.PMax + diff);
            }
            /// <summary>
            /// Colision con AABB
            /// </summary>
            public bool testAabb(TgcBoundingBox aabb)
            {
                //return TgcCollisionUtils.testTriangleAABB(a, b, c, aabb);
                //return testTriangleAABB(a, b, c, aabb);
                //return TriangleBoxCollision.testTriangleAABB(new Vector3d(a), new Vector3d(b), new Vector3d(c), new TgcBoundingBoxd(aabb));
                //return testTinyTriangleAABB(a, b, c, aabb);

                return TgcCollisionUtils.testAABBAABB(this.aabb, aabb);
            }

            /// <summary>
            /// Indica si alguno de los 3 vertices del triangulo esta adentro del AABB
            /// </summary>
            public static bool testTinyTriangleAABB(Vector3 a, Vector3 b, Vector3 c, TgcBoundingBox aabb)
            {
                return testPointAABB(a, aabb) || testPointAABB(b, aabb) || testPointAABB(c, aabb);
            }

            /// <summary>
            /// Test si un punto esta dentro de un un AABB
            /// </summary>
            public static bool testPointAABB(Vector3 p, TgcBoundingBox aabb)
            {
                return p.X >= aabb.PMin.X && p.Y >= aabb.PMin.Y && p.Z >= aabb.PMin.Z &&
                    p.X <= aabb.PMax.X && p.Y <= aabb.PMax.Y && p.Z <= aabb.PMax.Z;
            }

            /// <summary>
            /// Colisión Triange-AABB
            /// Basado en: http://www.geometrictools.com/LibMathematics/Intersection/Wm5IntrTriangle3Box3.cpp
            /// </summary>
            public static bool testTriangleAABB(Vector3 a, Vector3 b, Vector3 c, TgcBoundingBox box)
            {
                Vector3 boxCenter = box.calculateBoxCenter();
                Vector3 boxExtentVec = box.calculateAxisRadius();
                float[] boxExtent = new float[] { boxExtentVec.X, boxExtentVec.Y, boxExtentVec.Z };
                Vector3[] boxAxis = new Vector3[]{
                new Vector3(1, 0, 0),
                new Vector3(0, 1, 0),
                new Vector3(0, 0, 1)
            };

                float min0, max0, min1, max1;
                Vector3 D;
                Vector3[] edge = new Vector3[3];

                // Test direction of triangle normal.
                edge[0] = b - a;
                edge[1] = c - a;
                D = Vector3.Cross(edge[0], edge[1]);
                min0 = Vector3.Dot(D, a);
                max0 = min0;
                getAabbProjection(D, boxCenter, boxExtent, out min1, out max1);
                if (max1 < min0 || max0 < min1)
                {
                    return false;
                }

                // Test direction of box faces.
                for (int i = 0; i < 3; ++i)
                {
                    D = boxAxis[i];
                    getTriangleProjection(D, a, b, c, out min0, out max0);
                    float DdC = Vector3.Dot(D, boxCenter);
                    min1 = DdC - boxExtent[i];
                    max1 = DdC + boxExtent[i];
                    if (max1 < min0 || max0 < min1)
                    {
                        return false;
                    }
                }

                // Test direction of triangle-box edge cross products.
                edge[2] = edge[1] - edge[0];
                for (int i0 = 0; i0 < 3; ++i0)
                {
                    for (int i1 = 0; i1 < 3; ++i1)
                    {
                        D = Vector3.Cross(edge[i0], boxAxis[i1]);
                        getTriangleProjection(D, a, b, c, out min0, out max0);
                        getAabbProjection(D, boxCenter, boxExtent, out min1, out max1);
                        if (max1 < min0 || max0 < min1)
                        {
                            return false;
                        }
                    }
                }

                return true;

            }

            public static bool IsZeroVector(Vector3 v)
            {
                return FastMath.Abs(v.X) < 0.01f && FastMath.Abs(v.Y) < 0.01f && FastMath.Abs(v.Z) < 0.01f;
            }

            /// <summary>
            /// http://www.geometrictools.com/LibMathematics/Intersection/Wm5IntrUtility3.cpp
            /// Adaptado para AABB
            /// </summary>
            public static void getAabbProjection(Vector3 axis, Vector3 boxCenter, float[] boxExtent, out float imin, out float imax)
            {
                float origin = Vector3.Dot(axis, boxCenter);
                float maximumExtent =
                    FastMath.Abs(boxExtent[0] * axis.X) +
                    FastMath.Abs(boxExtent[1] * axis.Y) +
                    FastMath.Abs(boxExtent[2] * axis.Z);

                imin = origin - maximumExtent;
                imax = origin + maximumExtent;
            }

            /// <summary>
            /// http://www.geometrictools.com/LibMathematics/Intersection/Wm5IntrUtility3.cpp
            /// </summary>
            private static void getTriangleProjection(Vector3 axis, Vector3 v0, Vector3 v1, Vector3 v2, out float imin, out float imax)
            {
                float[] dot = new float[]{
                Vector3.Dot(axis, v0),
                Vector3.Dot(axis, v1),
                Vector3.Dot(axis, v2)
            };
                imin = dot[0];
                imax = imin;

                if (dot[1] < imin)
                {
                    imin = dot[1];
                }
                else if (dot[1] > imax)
                {
                    imax = dot[1];
                }

                if (dot[2] < imin)
                {
                    imin = dot[2];
                }
                else if (dot[2] > imax)
                {
                    imax = dot[2];
                }
            }

        }


    }




    


    /// <summary>
    /// Voxel
    /// </summary>
    public class Voxel
    {
        /// <summary>
        /// Tipos de voxels
        /// </summary>
        public enum VoxelType
        {
            Surface,
            Inner,
            Empty,
        }


        public TgcBoundingBox aabb;
        public VoxelType type;
        public int i;
        public int j;
        public int k;
        public bool used;
        public int density;

        public void dispose()
        {
            if (aabb != null)
            {
                aabb.dispose();
                aabb = null;
            }
        }

        public override string ToString()
        {
            return "[" + i + ", " + j + ", " + k + "] - " + type.ToString();
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
