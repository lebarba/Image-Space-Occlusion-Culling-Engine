using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using TgcViewer.Utils.TgcSceneLoader;
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX;
using TgcViewer.Utils.TgcGeometry;

namespace TgcViewer.Utils.TgcSkeletalAnimation
{
    /// <summary>
    /// Herramienta para cargar una Malla con animacion del tipo Skeletal Animation, segun formato TGC
    /// </summary>
    public class TgcSkeletalLoader
    {

        Dictionary<string, TgcTexture> texturesDict;
        Device device;

        IMeshFactory meshFactory;
        /// <summary>
        /// Factory utilizado para crear una instancia de TgcSkeletalMesh.
        /// Por default se utiliza la clase DefaultMeshFactory.
        /// </summary>
        public IMeshFactory MeshFactory
        {
            get { return meshFactory; }
            set { meshFactory = value; }
        }


        /// <summary>
        /// Crear un nuevo Loader
        /// </summary>
        public TgcSkeletalLoader()
        {
            this.device = GuiController.Instance.D3dDevice;
            texturesDict = new Dictionary<string, TgcTexture>();
            this.meshFactory = new DefaultMeshFactory();
        }

        /// <summary>
        /// Carga un modelo a partir de un archivo
        /// </summary>
        /// <param name="filePath">Ubicacion del archivo XML</param>
        /// <param name="mediaPath">Path a partir del cual hay que buscar las Texturas</param>
        /// <returns>Modelo cargado</returns>
        public TgcSkeletalMesh loadMeshFromFile(string filePath, string mediaPath)
        {
            try
            {
                string xmlString = File.ReadAllText(filePath);
                return loadMeshFromString(xmlString, mediaPath);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al cargar mesh desde archivo: " + filePath, ex);
            }
        }

        /// <summary>
        /// Carga un modelo a partir de un archivo.
        /// Se elige el directorio de texturas y recursos en base al directorio en el cual se encuntra el archivo del modelo.
        /// </summary>
        /// <param name="filePath">Ubicacion del archivo XML</param>
        /// <returns>Modelo cargado</returns>
        public TgcSkeletalMesh loadMeshFromFile(string filePath)
        {
            string mediaPath = new FileInfo(filePath).DirectoryName + "\\";
            return loadMeshFromFile(filePath, mediaPath);
        }

        /// <summary>
        /// Carga un modelo y un conjunto de animaciones a partir de varios archivos
        /// </summary>
        /// <param name="meshFilePath">Ubicacion del archivo XML del modelo</param>
        /// <param name="mediaPath">Path a partir del cual hay que buscar las Texturas</param>
        /// <param name="animationsFilePath">Array con ubicaciones de los archivos XML de cada animación</param>
        /// <returns>Modelo cargado con sus animaciones</returns>
        public TgcSkeletalMesh loadMeshAndAnimationsFromFile(string meshFilePath, string mediaPath, string[] animationsFilePath)
        {
            TgcSkeletalMesh mesh = loadMeshFromFile(meshFilePath, mediaPath);
            foreach (string animPath in animationsFilePath)
            {
                loadAnimationFromFile(mesh, animPath);
            }
            return mesh;
        }

        /// <summary>
        /// Carga un modelo y un conjunto de animaciones a partir de varios archivos.
        /// Se elige el directorio de texturas y recursos en base al directorio en el cual se encuntra el archivo del modelo.
        /// </summary>
        /// <param name="meshFilePath">Ubicacion del archivo XML del modelo</param>
        /// <param name="animationsFilePath">Array con ubicaciones de los archivos XML de cada animación</param>
        /// <returns>Modelo cargado con sus animaciones</returns>
        public TgcSkeletalMesh loadMeshAndAnimationsFromFile(string meshFilePath, string[] animationsFilePath)
        {
            string mediaPath = new FileInfo(meshFilePath).DirectoryName + "\\";
            return loadMeshAndAnimationsFromFile(meshFilePath, mediaPath, animationsFilePath);
        }

        /// <summary>
        /// Carga un modelo a partir del string del XML 
        /// </summary>
        /// <param name="xmlString">contenido del XML</param>
        /// <param name="mediaPath">Path a partir del cual hay que buscar las Texturas</param>
        /// <returns>Modelo cargado</returns>
        public TgcSkeletalMesh loadMeshFromString(string xmlString, string mediaPath)
        {
            TgcSkeletalParser parser = new TgcSkeletalParser();
            TgcSkeletalMeshData meshData = parser.parseMeshFromString(xmlString);
            return loadMesh(meshData, mediaPath);
        }

        /// <summary>
        /// Carga una animación a un modelo ya cargado, en base a un archivo
        /// La animación se agrega al modelo.
        /// </summary>
        /// <param name="mesh">Modelo ya cargado</param>
        /// <param name="filePath">Ubicacion del archivo XML de la animación</param>
        public void loadAnimationFromFile(TgcSkeletalMesh mesh, string filePath)
        {
            try
            {
                string xmlString = File.ReadAllText(filePath);
                loadAnimationFromString(mesh, xmlString);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al cargar animacion desde archivo: " + filePath, ex);
            }
        }

        /// <summary>
        /// Carga una animación a un modelo ya cargado, a partir del string del XML.
        /// La animación se agrega al modelo.
        /// </summary>
        /// <param name="mesh">Modelo ya cargado</param>
        /// <param name="xmlString">contenido del XML</param>
        public void loadAnimationFromString(TgcSkeletalMesh mesh, string xmlString)
        {
            TgcSkeletalParser parser = new TgcSkeletalParser();
            TgcSkeletalAnimationData animationData = parser.parseAnimationFromString(xmlString);
            TgcSkeletalAnimation animation = loadAnimation(mesh, animationData);
            mesh.Animations.Add(animation.Name, animation);
        }



        /// <summary>
        /// Carga un Modelo a partir de un objeto TgcSkeletalMeshData ya parseado
        /// </summary>
        /// <param name="meshData">Objeto con datos ya parseados</param>
        /// <param name="mediaPath">Path a partir del cual hay que buscar las Texturas</param>
        /// <returns>Modelo cargado</returns>
        public TgcSkeletalMesh loadMesh(TgcSkeletalMeshData meshData, string mediaPath)
        {
            //Cargar Texturas
            TgcSkeletalLoaderMaterialAux[] materialsArray = new TgcSkeletalLoaderMaterialAux[meshData.materialsData.Length];
            for (int i = 0; i < meshData.materialsData.Length; i++)
            {
                TgcMaterialData materialData = meshData.materialsData[i];
                string texturesPath = mediaPath + meshData.texturesDir + "\\";

                //Crear StandardMaterial
                if (materialData.type.Equals(TgcMaterialData.StandardMaterial))
                {
                    materialsArray[i] = createTextureAndMaterial(materialData, texturesPath);
                }

                //Crear MultiMaterial
                else if (materialData.type.Equals(TgcMaterialData.MultiMaterial))
                {
                    TgcSkeletalLoaderMaterialAux matAux = new TgcSkeletalLoaderMaterialAux();
                    materialsArray[i] = matAux;
                    matAux.subMaterials = new TgcSkeletalLoaderMaterialAux[materialData.subMaterials.Length];
                    for (int j = 0; j < materialData.subMaterials.Length; j++)
                    {
                        matAux.subMaterials[j] = createTextureAndMaterial(materialData.subMaterials[j], texturesPath);
                    }
                }
            }


            //Crear Mesh
            TgcSkeletalMesh tgcMesh = null;

            //Crear mesh que no tiene Material, con un color simple
            if (meshData.materialId == -1)
            {
                tgcMesh = crearMeshSoloColor(meshData);
            }


            //Crear mesh con DiffuseMap
            else
            {
                tgcMesh = crearMeshDiffuseMap(materialsArray, meshData);
            }


            //Crear BoundingBox, aprovechar lo que viene del XML o crear uno por nuestra cuenta
            if (meshData.pMin != null && meshData.pMax != null)
            {
                tgcMesh.BoundingBox = new TgcBoundingBox(
                        TgcParserUtils.float3ArrayToVector3(meshData.pMin),
                        TgcParserUtils.float3ArrayToVector3(meshData.pMax)
                        );
            }
            else
            {
                tgcMesh.createBoundingBox();
            }


            tgcMesh.Enabled = true;
            return tgcMesh;
        }

        /// <summary>
        /// Cargar estructura de animacion
        /// </summary>
        private TgcSkeletalAnimation loadAnimation(TgcSkeletalMesh mesh, TgcSkeletalAnimationData animationData)
        {
            //Crear array para todos los huesos, tengan o no keyFrames
            List<TgcSkeletalAnimationFrame>[] boneFrames = new List<TgcSkeletalAnimationFrame>[mesh.Bones.Length];
  
            //Cargar los frames para los huesos que si tienen
            for (int i = 0; i < animationData.bonesFrames.Length; i++)
			{
			    TgcSkeletalAnimationBoneData boneData = animationData.bonesFrames[i];
                
                //Crear frames
                for (int j = 0; j < boneData.keyFrames.Length; j++)
			    {
    			    TgcSkeletalAnimationBoneFrameData frameData = boneData.keyFrames[j];

                    TgcSkeletalAnimationFrame frame = new TgcSkeletalAnimationFrame(
                        frameData.frame,
                        new Vector3(frameData.position[0], frameData.position[1], frameData.position[2]),
                        new Quaternion(frameData.rotation[0], frameData.rotation[1], frameData.rotation[2], frameData.rotation[3])
                        );

                    //Agregar a lista de frames del hueso
                    if (boneFrames[boneData.id] == null)
                    {
                        boneFrames[boneData.id] = new List<TgcSkeletalAnimationFrame>();
                    }
                    boneFrames[boneData.id].Add(frame);
			    }
			}

            //BoundingBox de la animación, aprovechar lo que viene en el XML o utilizar el de la malla estática
            TgcBoundingBox boundingBox = null;
            if (animationData.pMin != null && animationData.pMax != null)
            {
                boundingBox = new TgcBoundingBox(
                    TgcParserUtils.float3ArrayToVector3(animationData.pMin),
                    TgcParserUtils.float3ArrayToVector3(animationData.pMax));
            }
            else
            {
                boundingBox = mesh.BoundingBox;
            }
                
            //Crear animacion
            TgcSkeletalAnimation animation = new TgcSkeletalAnimation(animationData.name, animationData.frameRate, animationData.framesCount, boneFrames, boundingBox);
            return animation;
        }

        /// <summary>
        /// Cargar estructura de esqueleto
        /// </summary>
        private TgcSkeletalBone[] loadSkeleton(TgcSkeletalMeshData meshData)
        {
            //Crear huesos
            TgcSkeletalBone[] bones = new TgcSkeletalBone[meshData.bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                TgcSkeletalBoneData boneData = meshData.bones[i];

                TgcSkeletalBone bone = new TgcSkeletalBone(i, boneData.name,
                    new Vector3(boneData.startPosition[0], boneData.startPosition[1], boneData.startPosition[2]),
                    new Quaternion(boneData.startRotation[0], boneData.startRotation[1], boneData.startRotation[2], boneData.startRotation[3])
                    );
                bones[i] = bone;
            }

            //Cargar padres en huesos
            for (int i = 0; i < bones.Length; i++)
            {
                TgcSkeletalBoneData boneData = meshData.bones[i];
                if (boneData.parentId == -1)
                {
                    bones[i].ParentBone = null;
                }
                else
                {
                    bones[i].ParentBone = bones[boneData.parentId];
                }
            }

            return bones;
        }

        /// <summary>
        /// Cargar Weights de vertices
        /// </summary>
        /// <param name="meshData"></param>
        /// <returns></returns>
        private TgcSkeletalVertexWeight[] loadVerticesWeights(TgcSkeletalMeshData meshData, TgcSkeletalBone[] bones)
        {
            //Crear un array de Weights para cada uno de los vertices de la malla
            TgcSkeletalVertexWeight[] weights = new TgcSkeletalVertexWeight[meshData.verticesCoordinates.Length / 3];
            float[] vertexWeightTotals = new float[weights.Length];
            for (int i = 0; i < weights.Length; i++)
			{
			    weights[i] = new TgcSkeletalVertexWeight();
                vertexWeightTotals[i] = 0;
			}

            //Cargar los weights de cada vertice
            int weightsCount = meshData.verticesWeights.Length / 3;
            for (int i = 0; i < weightsCount; i++)
            {
                int vertexIdx = (int)meshData.verticesWeights[i * 3];
                int boneIdx = (int)meshData.verticesWeights[i * 3 + 1];
                float weightVal = meshData.verticesWeights[i * 3 + 2];

                TgcSkeletalBone bone = bones[boneIdx];
                TgcSkeletalVertexWeight.BoneWeight weight = new TgcSkeletalVertexWeight.BoneWeight(bone, weightVal);

                weights[vertexIdx].Weights.Add(weight);

                //acumular total de weight para ese vertice, para luego poder normalizar
                vertexWeightTotals[vertexIdx] += weightVal;
            }

            //Normalizar weights de cada vertice
            for (int i = 0; i < weights.Length; i++)
            {
                TgcSkeletalVertexWeight vertexWeight = weights[i];
                float vTotal = vertexWeightTotals[i];

                //Normalizar cada valor segun el total acumulado en el vertice
                foreach (TgcSkeletalVertexWeight.BoneWeight w in vertexWeight.Weights)
                {
                    w.Weight = w.Weight / vTotal;
                }
            }

            return weights;
        }

        /// <summary>
        /// Crea un mesh con uno o varios DiffuseMap
        /// </summary>
        /// <returns></returns>
        private TgcSkeletalMesh crearMeshDiffuseMap(TgcSkeletalLoaderMaterialAux[] materialsArray, TgcSkeletalMeshData meshData)
        {
            //Crear Mesh
            Mesh mesh = new Mesh(meshData.coordinatesIndices.Length / 3, meshData.coordinatesIndices.Length, MeshFlags.Managed, DiffuseMapVertexElements, device);
            
            //Cargar VertexBuffer
            using (VertexBuffer vb = mesh.VertexBuffer)
            {
                GraphicsStream data = vb.Lock(0, 0, LockFlags.None);
                for (int j = 0; j < meshData.coordinatesIndices.Length; j++)
                {
                    DiffuseMapVertex v = new DiffuseMapVertex();

                    //vertices
                    int coordIdx = meshData.coordinatesIndices[j] * 3;
                    v.Position = new Vector3(
                        meshData.verticesCoordinates[coordIdx],
                        meshData.verticesCoordinates[coordIdx + 1],
                        meshData.verticesCoordinates[coordIdx + 2]
                        );

                    //texture coordinates diffuseMap
                    int texCoordIdx = meshData.texCoordinatesIndices[j] * 2;
                    v.Tu = meshData.textureCoordinates[texCoordIdx];
                    v.Tv = meshData.textureCoordinates[texCoordIdx + 1];

                    //color
                    int colorIdx = meshData.colorIndices[j];
                    v.Color = meshData.verticesColors[colorIdx];

                    data.Write(v);
                }
                vb.Unlock();
            }

            //Cargar IndexBuffer en forma plana
            using (IndexBuffer ib = mesh.IndexBuffer)
            {
                short[] indices = new short[meshData.coordinatesIndices.Length];
                for (int j = 0; j < indices.Length; j++)
                {
                    indices[j] = (short)j;
                }
                ib.SetData(indices, 0, LockFlags.None);
            }

            //Configurar Material y Textura para un solo SubSet
            TgcSkeletalLoaderMaterialAux matAux = materialsArray[meshData.materialId];
            Material[] meshMaterials;
            TgcTexture[] meshTextures;
            if (matAux.subMaterials == null)
            {
                meshMaterials = new Material[] { matAux.materialId };
                meshTextures = new TgcTexture[] { matAux.texture };
            }

            //Configurar Material y Textura para varios SubSet
            else
            {
                //Cargar attributeBuffer con los id de las texturas de cada triángulo
                int[] attributeBuffer = mesh.LockAttributeBufferArray(LockFlags.None);
                Array.Copy(meshData.materialsIds, attributeBuffer, attributeBuffer.Length);
                mesh.UnlockAttributeBuffer(attributeBuffer);

                //Cargar array de Materials y Texturas
                meshMaterials = new Material[matAux.subMaterials.Length];
                meshTextures = new TgcTexture[matAux.subMaterials.Length];
                for (int m = 0; m < matAux.subMaterials.Length; m++)
                {
                    meshMaterials[m] = matAux.subMaterials[m].materialId;
                    meshTextures[m] = matAux.subMaterials[m].texture;
                }
            }

            //Cargar esqueleto
            TgcSkeletalBone[] bones = loadSkeleton(meshData);
            TgcSkeletalVertexWeight[] verticesWeights = loadVerticesWeights(meshData, bones);

            //Cargar datos de meshData que interesan mantener como originales
            TgcSkeletalMesh.OriginalData origData = new TgcSkeletalMesh.OriginalData();
            origData.coordinatesIndices = meshData.coordinatesIndices;
            origData.verticesCoordinates = meshData.verticesCoordinates;
            origData.colorIndices = meshData.colorIndices;
            origData.verticesColors = meshData.verticesColors;
            origData.texCoordinatesIndices = meshData.texCoordinatesIndices;
            origData.textureCoordinates = meshData.textureCoordinates;

            //Crear mesh de TGC
            TgcSkeletalMesh tgcMesh = meshFactory.createNewMesh(mesh, meshData.name, TgcSkeletalMesh.MeshRenderType.DIFFUSE_MAP, origData, bones, verticesWeights);
            tgcMesh.Materials = meshMaterials;
            tgcMesh.DiffuseMaps = meshTextures;
            return tgcMesh;
        }



        /// <summary>
        /// Crea un mesh sin texturas, solo con VertexColors
        /// </summary>
        /// <param name="meshData"></param>
        private TgcSkeletalMesh crearMeshSoloColor(TgcSkeletalMeshData meshData)
        {
            //Crear Mesh
            Mesh mesh = new Mesh(meshData.coordinatesIndices.Length / 3, meshData.coordinatesIndices.Length, MeshFlags.Managed, VertexColorVertexElements, device);

            //Cargar VertexBuffer
            using (VertexBuffer vb = mesh.VertexBuffer)
            {
                GraphicsStream data = vb.Lock(0, 0, LockFlags.None);
                for (int j = 0; j < meshData.coordinatesIndices.Length; j++)
                {
                    VertexColorVertex v = new VertexColorVertex();

                    //vertices
                    int coordIdx = meshData.coordinatesIndices[j] * 3;
                    v.Position = new Vector3(
                        meshData.verticesCoordinates[coordIdx],
                        meshData.verticesCoordinates[coordIdx + 1],
                        meshData.verticesCoordinates[coordIdx + 2]
                        );

                    //color
                    int colorIdx = meshData.colorIndices[j];
                    v.Color = meshData.verticesColors[colorIdx];

                    data.Write(v);
                }
                vb.Unlock();
            }

            //Cargar indexBuffer en forma plana
            using (IndexBuffer ib = mesh.IndexBuffer)
            {
                short[] indices = new short[meshData.coordinatesIndices.Length];
                for (int i = 0; i < indices.Length; i++)
                {
                    indices[i] = (short)i;
                }
                ib.SetData(indices, 0, LockFlags.None);
            }

            //Cargar esqueleto
            TgcSkeletalBone[] bones = loadSkeleton(meshData);
            TgcSkeletalVertexWeight[] verticesWeights = loadVerticesWeights(meshData, bones);

            //Cargar datos de meshData que interesan mantener como originales
            TgcSkeletalMesh.OriginalData origData = new TgcSkeletalMesh.OriginalData();
            origData.coordinatesIndices = meshData.coordinatesIndices;
            origData.verticesCoordinates = meshData.verticesCoordinates;
            origData.colorIndices = meshData.colorIndices;
            origData.verticesColors = meshData.verticesColors;
            origData.texCoordinatesIndices = null;
            origData.textureCoordinates = null;

            //Crear mesh de TGC
            TgcSkeletalMesh tgcMesh = meshFactory.createNewMesh(mesh, meshData.name, TgcSkeletalMesh.MeshRenderType.VERTEX_COLOR, origData, bones, verticesWeights);
            return tgcMesh;
        }


        /// <summary>
        /// Crea Material y Textura
        /// </summary>
        private TgcSkeletalLoaderMaterialAux createTextureAndMaterial(TgcMaterialData materialData, string texturesPath)
        {
            TgcSkeletalLoaderMaterialAux matAux = new TgcSkeletalLoaderMaterialAux();

            //Crear material
            Material material = new Material();
            matAux.materialId = material;
            material.AmbientColor = new ColorValue(
                materialData.ambientColor[0],
                materialData.ambientColor[1],
                materialData.ambientColor[2],
                materialData.ambientColor[3]);
            material.DiffuseColor = new ColorValue(
                materialData.diffuseColor[0],
                materialData.diffuseColor[1],
                materialData.diffuseColor[2],
                materialData.diffuseColor[3]);
            material.SpecularColor = new ColorValue(
                materialData.specularColor[0],
                materialData.specularColor[1],
                materialData.specularColor[2],
                materialData.specularColor[3]);

            //TODO ver que hacer con la opacity

            //crear textura
            texturesDict.Clear();
            if (materialData.fileName != null)
            {
                //revisar que esa imagen no se haya cargado previamente
                TgcTexture texture;
                if (texturesDict.ContainsKey(materialData.fileName))
                {
                    texture = texturesDict[materialData.fileName];
                }
                else
                {
                    texture = TgcTexture.createTexture(device, materialData.fileName, texturesPath + "\\" + materialData.fileName);
                    texturesDict[materialData.fileName] = texture;
                    //TODO usar para algo el OFFSET y el TILING
                }
                matAux.texture = texture;


            }
            else
            {
                matAux.texture = null;
            }

            return matAux;
        }




        #region Mesh FVF

        /// <summary>
        /// FVF para formato de malla VERTEX_COLOR
        /// </summary>
        public static readonly VertexElement[] VertexColorVertexElements = new VertexElement[]
        {
            new VertexElement(0, 0, DeclarationType.Float3,
                                    DeclarationMethod.Default,
                                    DeclarationUsage.Position, 0),
                                                 
            new VertexElement(0, 12, DeclarationType.Color,
                                     DeclarationMethod.Default,
                                     DeclarationUsage.Color, 0),
           
            VertexElement.VertexDeclarationEnd 
        };


        /// <summary>
        /// Estructura de Vertice para formato de malla VERTEX_COLOR
        /// </summary>
        public struct VertexColorVertex
        {
            public Vector3 Position;
            public int Color;
        }

        /// <summary>
        /// FVF para formato de malla DIFFUSE_MAP
        /// </summary>
        public static readonly VertexElement[] DiffuseMapVertexElements = new VertexElement[]
        {
            new VertexElement(0, 0, DeclarationType.Float3,
                                    DeclarationMethod.Default,
                                    DeclarationUsage.Position, 0),
                                                               
            new VertexElement(0, 12, DeclarationType.Color,
                                     DeclarationMethod.Default,
                                     DeclarationUsage.Color, 0),

            new VertexElement(0, 16, DeclarationType.Float2,
                                     DeclarationMethod.Default,
                                     DeclarationUsage.TextureCoordinate, 0),
           
            VertexElement.VertexDeclarationEnd 
        };


        /// <summary>
        /// Estructura de Vertice para formato de malla DIFFUSE_MAP
        /// </summary>
        public struct DiffuseMapVertex
        {
            public Vector3 Position;
            public int Color;
            public float Tu;
            public float Tv;
        }


        #endregion




        /// <summary>
        /// Estructura auxiliar para cargar SubMaterials y Texturas
        /// </summary>
        class TgcSkeletalLoaderMaterialAux
        {
            public Material materialId;
            public TgcTexture texture;
            public TgcSkeletalLoaderMaterialAux[] subMaterials;
        }



        #region MeshFactory

        /// <summary>
        /// Factory para permitir crear una instancia especifica de la clase TgcMesh
        /// </summary>
        public interface IMeshFactory
        {
            /// <summary>
            /// Crear una nueva instancia de la clase TgcSkeletalMesh o derivados
            /// </summary>
            /// <param name="d3dMesh">Mesh de Direct3D</param>
            /// <param name="meshName">Nombre de la malla</param>
            /// <param name="renderType">Tipo de renderizado de la malla</param>
            /// <param name="bones">Huesos de la malla</param>
            /// <param name="verticesWeights">Influencias de los vértices</param>
            /// <param name="meshData">Datos de la malla</param>
            /// <returns>Instancia de TgcMesh creada</returns>
            TgcSkeletalMesh createNewMesh(Mesh d3dMesh, string meshName, TgcSkeletalMesh.MeshRenderType renderType, 
                TgcSkeletalMesh.OriginalData origData, TgcSkeletalBone[] bones, TgcSkeletalVertexWeight[] verticesWeights);
        }

        /// <summary>
        /// Factory default que crea una instancia de la clase TgcMesh
        /// </summary>
        public class DefaultMeshFactory : IMeshFactory
        {

            public TgcSkeletalMesh createNewMesh(Mesh d3dMesh, string meshName, TgcSkeletalMesh.MeshRenderType renderType, TgcSkeletalMesh.OriginalData origData, TgcSkeletalBone[] bones, TgcSkeletalVertexWeight[] verticesWeights)
            {
                return new TgcSkeletalMesh(d3dMesh, meshName, renderType, origData, bones, verticesWeights);
            }
        }

        #endregion


    }
}
