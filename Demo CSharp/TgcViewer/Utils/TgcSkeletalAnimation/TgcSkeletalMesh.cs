using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX;
using TgcViewer.Utils.TgcGeometry;
using System.Drawing;
using TgcViewer.Utils.TgcSceneLoader;

namespace TgcViewer.Utils.TgcSkeletalAnimation
{
    /// <summary>
    /// Malla que representa un modelo 3D con varias animaciones, animadas por Skeletal Animation
    /// </summary>
    public class TgcSkeletalMesh : IRenderObject, ITransformObject
    {
        /// <summary>
        /// Mesh de DirectX
        /// </summary>
        protected Mesh d3dMesh;
        /// <summary>
        /// Mesh interna de DirectX
        /// </summary>
        public Mesh D3dMesh
        {
            get { return d3dMesh; }
        }

        protected string name;
        /// <summary>
        /// Nombre de la malla
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        protected Material[] materials;
        /// <summary>
        /// Array de Materials
        /// </summary>
        public Material[] Materials
        {
            get { return materials; }
            set { materials = value; }
        }

        protected TgcTexture[] diffuseMaps;
        /// <summary>
        /// Array de texturas para DiffuseMap
        /// </summary>
        public TgcTexture[] DiffuseMaps
        {
            get { return diffuseMaps; }
            set { diffuseMaps = value; }
        }

        protected bool enabled;
        /// <summary>
        /// Indica si la malla esta habilitada para ser renderizada
        /// </summary>
        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        protected Matrix transform;
        /// <summary>
        /// Matriz final que se utiliza para aplicar transformaciones a la malla.
        /// Si la propiedad AutoTransformEnable esta en True, la matriz se reconstruye en cada cuadro
        /// en base a los valores de: Position, Rotation, Scale.
        /// Si AutoTransformEnable está en False, se respeta el valor que el usuario haya cargado en la matriz.
        /// </summary>
        public Matrix Transform
        {
            get { return transform; }
            set { transform = value; }
        }

        protected bool autoTransformEnable;
        /// <summary>
        /// En True hace que la matriz de transformacion (Transform) de la malla se actualiza en
        /// cada cuadro en forma automática, según los valores de: Position, Rotation, Scale.
        /// En False se respeta lo que el usuario haya cargado a mano en la matriz.
        /// Por default está en True.
        /// </summary>
        public bool AutoTransformEnable
        {
            get { return autoTransformEnable; }
            set { autoTransformEnable = value; }
        }


        protected Vector3 translation;
        /// <summary>
        /// Posicion absoluta de la Malla
        /// </summary>
        public Vector3 Position
        {
            get { return translation; }
            set
            {
                translation = value;
                updateBoundingBox();
            }
        }

        protected Vector3 rotation;
        /// <summary>
        /// Rotación absoluta de la malla
        /// </summary>
        public Vector3 Rotation
        {
            get { return rotation; }
            set { rotation = value; }
        }

        protected Vector3 scale;
        /// <summary>
        /// Escalado absoluto de la malla;
        /// </summary>
        public Vector3 Scale
        {
            get { return scale; }
            set
            {
                scale = value;
                updateBoundingBox();
            }
        }

        /// <summary>
        /// Tipos de de renderizado de mallas
        /// </summary>
        public enum MeshRenderType
        {
            /// <summary>
            /// Solo colores por vertice
            /// </summary>
            VERTEX_COLOR,
            /// <summary>
            /// Solo un canal de textura en DiffuseMap
            /// </summary>
            DIFFUSE_MAP,
        };

        /// <summary>
        /// BoundingBox de la malla sin ninguna animación.
        /// </summary>
        protected TgcBoundingBox staticMeshBoundingBox;

        protected TgcBoundingBox boundingBox;
        /// <summary>
        /// BoundingBox del Mesh.
        /// Puede variar según la animación que tiene configurada en el momento.
        /// </summary>
        public TgcBoundingBox BoundingBox
        {
            get { return boundingBox; }
            set {
                staticMeshBoundingBox = value;
                boundingBox = value; 
            }
        }

        protected MeshRenderType renderType;
        /// <summary>
        /// Tipo de formato de Render de esta malla
        /// </summary>
        public MeshRenderType RenderType
        {
            get { return renderType; }
            set { renderType = value; }
        }

        protected VertexDeclaration vertexDeclaration;
        /// <summary>
        /// VertexDeclaration del Flexible Vertex Format (FVF) usado por la malla
        /// </summary>
        public VertexDeclaration VertexDeclaration
        {
            get { return vertexDeclaration; }
        }

        protected bool autoUpdateBoundingBox;
        /// <summary>
        /// Indica si se actualiza automaticamente el BoundingBox con cada movimiento de la malla
        /// </summary>
        public bool AutoUpdateBoundingBox
        {
            get { return autoUpdateBoundingBox; }
            set { autoUpdateBoundingBox = value; }
        }


        protected TgcSkeletalBone[] bones;
        /// <summary>
        /// Huesos del esqueleto de la malla. Ordenados en forma jerárquica
        /// </summary>
        public TgcSkeletalBone[] Bones
        {
            get { return bones; }
        }

        protected TgcSkeletalVertexWeight[] verticesWeights;
        /// <summary>
        /// Influencias de los vertices
        /// </summary>
        public TgcSkeletalVertexWeight[] VerticesWeights
        {
            get { return verticesWeights; }
        }

        protected Dictionary<string, TgcSkeletalAnimation> animations;
        /// <summary>
        /// Mapa de animaciones de la malla
        /// </summary>
        public Dictionary<string, TgcSkeletalAnimation> Animations
        {
            get { return animations; }
        }

        protected TgcSkeletalAnimation currentAnimation;
        /// <summary>
        /// Animación actual de la malla
        /// </summary>
        public TgcSkeletalAnimation CurrentAnimation
        {
            get { return currentAnimation; }
        }

        protected float frameRate;
        /// <summary>
        /// Velocidad de la animacion medida en cuadros por segundo.
        /// </summary>
        public float FrameRate
        {
            get { return frameRate; }
        }

        protected int currentFrame;
        /// <summary>
        /// Cuadro actual de animacion
        /// </summary>
        public int CurrentFrame
        {
            get { return currentFrame; }
        }

        protected bool isAnimating;
        /// <summary>
        /// Indica si actualmente hay una animación en curso.
        /// </summary>
        public bool IsAnimating
        {
            get { return isAnimating; }
        }

        protected bool playLoop;
        /// <summary>
        /// Indica si la animación actual se ejecuta con un Loop
        /// </summary>
        public bool PlayLoop
        {
            get { return playLoop; }
        }

        protected List<TgcSkeletalBoneAttach> attachments;
        /// <summary>
        /// Modelos adjuntados para seguir la trayectoria de algún hueso
        /// </summary>
        public List<TgcSkeletalBoneAttach> Attachments
        {
            get { return attachments; }
        }


        #region Eventos

        /// <summary>
        /// Indica que la animación actual ha finalizado.
        /// Se llama cuando se acabaron los frames de la animación.
        /// Si se anima en Loop, se llama cada vez que termina.
        /// </summary>
        /// <param name="mesh">Malla animada</param>
        public delegate void AnimationEndsHandler(TgcSkeletalMesh mesh);
        /// <summary>
        /// Evento que se llama cada vez que la animación actual finaliza.
        /// Se llama cuando se acabaron los frames de la animación.
        /// Si se anima en Loop, se llama cada vez que termina.
        /// </summary>
        public event AnimationEndsHandler AnimationEnds;

        #endregion


        protected TgcSkeletalMesh parentInstance;
        /// <summary>
        /// Original desde el cual esta malla fue clonada.
        /// </summary>
        public TgcSkeletalMesh ParentInstance
        {
            get { return parentInstance; }
        }

        protected List<TgcSkeletalMesh> meshInstances;
        /// <summary>
        /// Lista de mallas que fueron clonadas a partir de este original
        /// </summary>
        public List<TgcSkeletalMesh> MeshInstances
        {
            get { return meshInstances; }
        }
		
		/// <summary>
        /// Cantidad de triángulos de la malla
        /// </summary>
        public int NumberTriangles
        {
            get { return d3dMesh.NumberFaces; }
        }

        /// <summary>
        /// Cantidad de vértices de la malla
        /// </summary>
        public int NumberVertices
        {
            get { return d3dMesh.NumberVertices; }
        }

        //Elementos para renderizar el esqueleto
        protected TgcBox[] skeletonRenderJoints;
        protected TgcLine[] skeletonRenderBones;

        /// <summary>
        /// Informacion del MeshData original que hay que guardar para poder alterar el VertexBuffer con la animacion
        /// </summary>
        protected OriginalData originalData;

        //Variables de animacion
        protected float animationTimeLenght;
        protected float currentTime;

        protected bool renderSkeleton;
        /// <summary>
        /// En true renderiza solo el esqueleto del modelo, en lugar de la malla.
        /// </summary>
        public bool RenderSkeleton
        {
            get { return renderSkeleton; }
            set { renderSkeleton = value; }
        }

        protected bool alphaBlendEnable;
        /// <summary>
        /// Habilita el renderizado con AlphaBlending para los modelos
        /// con textura o colores por vértice de canal Alpha.
        /// Por default está deshabilitado.
        /// </summary>
        public bool AlphaBlendEnable
        {
            get { return alphaBlendEnable; }
            set { alphaBlendEnable = value; }
        }
        
        /// <summary>
        /// Constructor vacio, para facilitar la herencia de esta clase.
        /// </summary>
        protected TgcSkeletalMesh()
        {
        }

        /// <summary>
        /// Crea una nueva malla.
        /// </summary>
        /// <param name="mesh">Mesh de DirectX</param>
        /// <param name="renderType">Formato de renderizado de la malla</param>
        /// <param name="origData">Datos originales de la malla que hay almacenar</param>
        /// <param name="bones">Datos de los huesos</param>
        /// <param name="verticesWeights">Datos de los Weights para cada vertice</param>
        public TgcSkeletalMesh(Mesh mesh, string name, MeshRenderType renderType, OriginalData origData, TgcSkeletalBone[] bones, TgcSkeletalVertexWeight[] verticesWeights)
        {
            this.initData(mesh, name, renderType, origData, bones, verticesWeights);
        }

        /// <summary>
        /// Crea una nueva malla que es una instancia de otra malla original.
        /// Reutiliza toda la geometría de la malla original sin duplicarla.
        /// Debe crearse luego de haber cargado todas las animaciones en la malla original
        /// </summary>
        /// <param name="name">Nombre de la malla</param>
        /// <param name="parentInstance">Malla original desde la cual basarse</param>
        /// <param name="translation">Traslación respecto de la malla original</param>
        /// <param name="rotation">Rotación respecto de la malla original</param>
        /// <param name="scale">Escala respecto de la malla original</param>
        public TgcSkeletalMesh(string name, TgcSkeletalMesh parentInstance, Vector3 translation, Vector3 rotation, Vector3 scale)
        {
            //Cargar iniciales datos en base al original
            this.initData(parentInstance.d3dMesh, name, parentInstance.renderType, parentInstance.originalData, parentInstance.bones, parentInstance.verticesWeights);
            this.diffuseMaps = parentInstance.diffuseMaps;
            this.materials = parentInstance.materials;

            //Almacenar transformación inicial
            this.translation = translation;
            this.rotation = rotation;
            this.scale = scale;

            //Agregar animaciones del original
            foreach (KeyValuePair<string, TgcSkeletalAnimation> entry in parentInstance.animations)
            {
                this.animations.Add(entry.Key, entry.Value);
            }

            //Agregar attachments del original, creando una instancia por cada attach
            foreach (TgcSkeletalBoneAttach parentAttach in parentInstance.attachments)
            {
                TgcSkeletalBoneAttach attach = new TgcSkeletalBoneAttach();
                attach.Bone = parentAttach.Bone;
                attach.Offset = parentAttach.Offset;
                //Crear instancia del mesh del attach del padre
                attach.Mesh = parentAttach.Mesh.createMeshInstance(name + "-" + parentAttach.Mesh.Name);
                attach.updateValues();
                this.attachments.Add(attach);
            }

            //almacenar instancia en el padre
            this.parentInstance = parentInstance;
            parentInstance.meshInstances.Add(this);
        }

        /// <summary>
        /// Cargar datos iniciales
        /// </summary>
        protected void initData(Mesh mesh, string name, MeshRenderType renderType, OriginalData originalData, TgcSkeletalBone[] bones, TgcSkeletalVertexWeight[] verticesWeights)
        {
            this.d3dMesh = mesh;
            this.name = name;
            this.renderType = renderType;
            this.originalData = originalData;
            this.enabled = false;
            this.autoUpdateBoundingBox = true;
            this.bones = bones;
            this.verticesWeights = verticesWeights;
            this.attachments = new List<TgcSkeletalBoneAttach>();
            this.meshInstances = new List<TgcSkeletalMesh>();
            this.renderSkeleton = false;
            this.alphaBlendEnable = false;

            //Crear vertexDeclaration
            vertexDeclaration = new VertexDeclaration(mesh.Device, mesh.Declaration);

            //variables de movimiento
            this.autoTransformEnable = true;
            this.translation = new Vector3(0f, 0f, 0f);
            this.rotation = new Vector3(0f, 0f, 0f);
            this.scale = new Vector3(1f, 1f, 1f);
            this.transform = Matrix.Identity;

            //variables de animacion
            this.isAnimating = false;
            this.currentAnimation = null;
            this.playLoop = false;
            this.frameRate = 0f;
            this.currentTime = 0f;
            this.animationTimeLenght = 0f;
            this.currentFrame = 0;
            this.animations = new Dictionary<string, TgcSkeletalAnimation>();

            //acomodar huesos
            setupSkeleton();
        }

        

        /// <summary>
        /// Configuracion inicial del esquleto
        /// </summary>
        protected void setupSkeleton()
        {
            //Actualizar jerarquia
            for (int i = 0; i < bones.Length; i++)
            {
                TgcSkeletalBone bone = bones[i];

                //Es hijo o padre
                if (bone.ParentBone == null)
                {
                    bone.MatFinal = bone.MatLocal;
                }
                else
                {
                    //Multiplicar por la matriz del padre
                    bone.MatFinal = bone.MatLocal * bone.ParentBone.MatFinal;                    
                }

                //Almacenar la inversa de la posicion original del hueso, para la referencia inicial de los vertices
                bone.MatInversePose = Matrix.Invert(bone.MatFinal);
            }
        }

        /// <summary>
        /// Crea mallas a modo Debug para visualizar la configuración del esqueleto
        /// </summary>
        public void buildSkletonMesh()
        {
            //Crear array para dibujar los huesos y joints
            Color jointsColor = Color.Violet;
            Color bonesColor = Color.Yellow;
            Vector3 jointsSize = new Vector3(2, 2, 2);
            Vector3 ceroVec = new Vector3(0, 0, 0);
            skeletonRenderJoints = new TgcBox[bones.Length];
            skeletonRenderBones = new TgcLine[bones.Length];
            int boneColor = Color.Yellow.ToArgb();

            //Actualizar jerarquia
            for (int i = 0; i < bones.Length; i++)
            {
                TgcSkeletalBone bone = bones[i];

                //Es hijo o padre
                if (bone.ParentBone == null)
                {
                    skeletonRenderBones[i] = null;
                }
                else
                {
                    //Crear linea de hueso para renderziar esqueleto
                    TgcLine boneLine = new TgcLine();
                    boneLine.PStart = Vector3.TransformCoordinate(ceroVec, bone.MatFinal);
                    boneLine.PEnd = Vector3.TransformCoordinate(ceroVec, bone.ParentBone.MatFinal);
                    boneLine.Color = bonesColor;
                    skeletonRenderBones[i] = boneLine;
                }
                //Crear malla de Joint para renderizar el esqueleto
                TgcBox jointBox = TgcBox.fromSize(jointsSize, jointsColor);
                jointBox.AutoTransformEnable = false;
                skeletonRenderJoints[i] = jointBox;
            }
        }


        /// <summary>
        /// Establece cual es la animacion activa de la malla.
        /// Si la animacion activa es la misma que ya esta siendo animada actualmente, no se para ni se reinicia.
        /// Para forzar que se reinicie es necesario hacer stopAnimation()
        /// </summary>
        /// <param name="animationName">Nombre de la animacion a activar</param>
        /// <param name="playLoop">Indica si la animacion vuelve a comenzar al terminar</param>
        /// <param name="userFrameRate">FrameRate personalizado. Con -1 se utiliza el default de la animación</param>
        public void playAnimation(string animationName, bool playLoop, float userFrameRate)
        {
            //ya se esta animando algo
            if (isAnimating)
            {
                //Si la animacion pedida es la misma que la actual no la quitamos
                if (currentAnimation.Name.Equals(animationName))
                {
                    //solo actualizamos el playLoop
                    this.playLoop = playLoop;
                }
                //es una nueva animacion
                else
                {
                    //parar animacion actual
                    stopAnimation();
                    //cargar nueva animacion
                    initAnimationSettings(animationName, playLoop, userFrameRate);
                }
            }

            //no se esta animando nada
            else
            {
                //cargar nueva animacion
                initAnimationSettings(animationName, playLoop, userFrameRate);
            }
        }

        /// <summary>
        /// Establece cual es la animacion activa de la malla.
        /// Si la animacion activa es la misma que ya esta siendo animada actualmente, no se para ni se reinicia.
        /// Para forzar que se reinicie es necesario hacer stopAnimation().
        /// Utiliza el FrameRate default de cada animación
        /// </summary>
        /// <param name="animationName">Nombre de la animacion a activar</param>
        /// <param name="playLoop">Indica si la animacion vuelve a comenzar al terminar</param>
        public void playAnimation(string animationName, bool playLoop)
        {
            playAnimation(animationName, playLoop, -1f);
        }

        /// <summary>
        /// Establece cual es la animacion activa de la malla.
        /// Si la animacion activa es la misma que ya esta siendo animada actualmente, no se para ni se reinicia.
        /// Para forzar que se reinicie es necesario hacer stopAnimation().
        /// Se reproduce con loop.
        /// Utiliza el FrameRate default de cada animación
        /// </summary>
        /// <param name="animationName">Nombre de la animacion a activar</param>
        public void playAnimation(string animationName)
        {
            playAnimation(animationName, true);
        }


        /// <summary>
        /// Prepara una nueva animacion para ser ejecutada
        /// </summary>
        protected void initAnimationSettings(string animationName, bool playLoop, float userFrameRate)
        {
            isAnimating = true;
            currentAnimation = animations[animationName];
            this.playLoop = playLoop;
            currentTime = 0;
            currentFrame = 0;

            //Cambiar BoundingBox
            boundingBox = currentAnimation.BoundingBox;
            updateBoundingBox();

            //Si el usuario no especifico un FrameRate, tomar el default de la animacion
            if (userFrameRate == -1f)
            {
                frameRate = (float)currentAnimation.FrameRate;
            }
            else
            {
                frameRate = userFrameRate;
            }

            //La duracion de la animacion.
            animationTimeLenght = ((float)currentAnimation.FramesCount - 1) / frameRate;

            
            //Configurar postura inicial de los huesos
            for (int i = 0; i < bones.Length; i++)
            {
                TgcSkeletalBone bone = bones[i];

                if (!currentAnimation.hasFrames(i))
                {
                    throw new Exception("El hueso " + bone.Name + " no posee KeyFrames");
                }

                //Determinar matriz local inicial
                TgcSkeletalAnimationFrame firstFrame = currentAnimation.BoneFrames[i][0];
                bone.MatLocal = Matrix.RotationQuaternion(firstFrame.Rotation) * Matrix.Translation(firstFrame.Position);

                //Multiplicar por matriz del padre, si tiene
                if (bone.ParentBone != null)
                {
                    bone.MatFinal = bone.MatLocal * bone.ParentBone.MatFinal;
                }
                else
                {
                    bone.MatFinal = bone.MatLocal;
                }
            }
            

            //Ajustar vertices a posicion inicial del esqueleto
            updateMeshVertices();
        }


        /// <summary>
        /// Desactiva la animacion actual
        /// </summary>
        public void stopAnimation()
        {
            isAnimating = false;
            boundingBox = staticMeshBoundingBox;

            //Invocar evento de finalización
            if (AnimationEnds != null)
            {
                AnimationEnds.Invoke(this);
            }
        }


        /// <summary>
        /// Actualiza el cuadro actual de la animacion.
        /// Debe ser llamado en cada cuadro antes de render()
        /// </summary>
        public void updateAnimation()
        {
            Device device = GuiController.Instance.D3dDevice;
            float elapsedTime = GuiController.Instance.ElapsedTime;

            //Ver que haya transcurrido cierta cantidad de tiempo
            if (elapsedTime < 0.0f)
            {
                return;
            }

            //Sumo el tiempo transcurrido
            currentTime += elapsedTime;

            //Se termino la animacion
            if (currentTime > animationTimeLenght)
            {
                //Ver si hacer loop
                if (playLoop)
                {
                    //Dejar el remanente de tiempo transcurrido para el proximo loop
                    currentTime = currentTime % animationTimeLenght;
                    //setSkleletonLastPose();
                    //updateMeshVertices();
                }
                else
                {

                    //TODO: Puede ser que haya que quitar este stopAnimation() y solo llamar al Listener (sin cargar isAnimating = false)

                    stopAnimation();
                }
            }

            //La animacion continua
            else
            {
                //Actualizar esqueleto y malla
                updateSkeleton();
                updateMeshVertices();
            }
        }

        /* CREO QUE NO HACE FALTA
        /// <summary>
        /// Cargar la posicion final de la animacion en cada hueso
        /// </summary>
        private void setSkleletonLastPose()
        {
            //Configurar postura inicial de los huesos
            for (int i = 0; i < bones.Length; i++)
            {
                TgcSkeletalBone bone = bones[i];

                if (!currentAnimation.hasFrames(i))
                {
                    continue;
                }

                List<TgcSkeletalAnimationFrame> frames = currentAnimation.BoneFrames[i];

                //Determinar posicion final del hueso para esta animacion
                TgcSkeletalAnimationFrame lastFrame = frames[frames.Count - 1];
                Matrix matPose = Matrix.RotationQuaternion(lastFrame.Rotation) * Matrix.Translation(lastFrame.Position);
                bone.MatLocal = matPose;

                //Multiplicar por matriz del padre
                if (bone.ParentBone != null)
                {
                    bone.MatFinal = bone.MatLocal * bone.ParentBone.MatFinal;
                }
                else
                {
                    bone.MatFinal = bone.MatLocal;
                }

            }
        }
        */

        /// <summary>
        /// Actualiza la posicion de cada hueso del esqueleto segun sus KeyFrames de la animacion
        /// </summary>
        protected void updateSkeleton()
        {
            for (int i = 0; i < bones.Length; i++)
            {
                TgcSkeletalBone bone = bones[i];

                //Tomar el frame actual para este hueso
                List<TgcSkeletalAnimationFrame> boneFrames = currentAnimation.BoneFrames[i];

                //Solo hay un frame, no hacer nada, ya se hizo en el init de la animacion
                if (boneFrames.Count == 1)
                {
                    continue;
                }

                //Obtener cuadro actual segun el tiempo transcurrido
                float currentFrameF = currentTime * frameRate;
                //Ve a que KeyFrame le corresponde
                int keyFrameIdx = getCurrentFrameBone(boneFrames, currentFrameF);
                this.currentFrame = keyFrameIdx;

                //Armar un intervalo entre el proximo KeyFrame y el anterior
                TgcSkeletalAnimationFrame frame1 = boneFrames[keyFrameIdx - 1];
                TgcSkeletalAnimationFrame frame2 = boneFrames[keyFrameIdx];

                //Calcular la cantidad que hay interpolar en base al la diferencia entre cuadros
                float framesDiff = frame2.Frame - frame1.Frame;
                float interpolationValue = (currentFrameF - frame1.Frame) / framesDiff;

                //Interpolar traslacion
                Vector3 frameTranslation = (frame2.Position - frame1.Position) * interpolationValue + frame1.Position;

                //Interpolar rotacion con SLERP
                Quaternion quatFrameRotation = Quaternion.Slerp(frame1.Rotation, frame2.Rotation, interpolationValue);

                //Unir ambas transformaciones de este frame
                Matrix frameMatrix = Matrix.RotationQuaternion(quatFrameRotation) * Matrix.Translation(frameTranslation);

                //Multiplicar por la matriz del padre, si tiene
                if (bone.ParentBone != null)
                {
                    bone.MatFinal = frameMatrix * bone.ParentBone.MatFinal;
                }
                else
                {
                    bone.MatFinal = frameMatrix;
                }
            }
        }


        /// <summary>
        /// Obtener el KeyFrame correspondiente a cada hueso segun el tiempo transcurrido
        /// </summary>
        protected int getCurrentFrameBone(List<TgcSkeletalAnimationFrame> boneFrames, float currentFrame)
        {
            for (int i = 0; i < boneFrames.Count; i++)
            {
                  if (currentFrame < boneFrames[i].Frame)
                {
                    return i;
                }
            }

            return boneFrames.Count - 1;
        }


        /// <summary>
        /// Actualizar los vertices de la malla segun las posiciones del los huesos del esqueleto
        /// </summary>
        protected void updateMeshVertices()
        {
            //Precalcular la multiplicación para llevar a un vertice a Bone-Space y luego transformarlo segun el hueso
            Matrix[] boneSpaceFinalTransforms = new Matrix[bones.Length];
            for (int i = 0; i < boneSpaceFinalTransforms.Length; i++)
            {
                TgcSkeletalBone bone = bones[i];
                boneSpaceFinalTransforms[i] = bone.MatInversePose * bone.MatFinal;
            }


            //TODO: Toda la siguiente parte de Skinning podria hacerse mas eficiente con un Vertex Shader


            //Cargar array de vertices interpolados
            float[] verticesFrameFinal = new float[originalData.verticesCoordinates.Length];
            int vertexCount = verticesFrameFinal.Length / 3;
            for (int i = 0; i < vertexCount; i++)
            {
                //vertice original
                Vector3 vOrig = new Vector3(
                    originalData.verticesCoordinates[i * 3],
                    originalData.verticesCoordinates[i * 3 + 1],
                    originalData.verticesCoordinates[i * 3 + 2]
                    );

                //Obtener los weights de este vertice
                TgcSkeletalVertexWeight vertexWeights = verticesWeights[i];
                Vector3 vTransformed = new Vector3();
                
                //Analizar como influye cada hueso en base a los Weights de este vertice
                foreach (TgcSkeletalVertexWeight.BoneWeight vertexWeight in vertexWeights.Weights)
	            {
                    //Llevar al vertice a Bone-Space y luego transformarlo segun como se modifico el hueso
                    Matrix m = boneSpaceFinalTransforms[vertexWeight.Bone.Index];

                    //Aplicar transformacion al vertice original
                    Vector3 vAux = Vector3.TransformCoordinate(vOrig, m);

                    //Ponderar resultado segun valor de Weight
                    vTransformed += vAux * vertexWeight.Weight;
	            }
                
                //Cargar valores modificados en array final de vertices
                verticesFrameFinal[i * 3] = vTransformed.X;
                verticesFrameFinal[i * 3 + 1] = vTransformed.Y;
                verticesFrameFinal[i * 3 + 2] = vTransformed.Z;
            }

            //expandir array para el vertex buffer
            fillVertexBufferData(verticesFrameFinal);
        }




        /// <summary>
        /// Llena la informacion del VertexBuffer con los vertices especificados
        /// </summary>
        /// <param name="verticesCoordinates"></param>
        protected void fillVertexBufferData(float[] verticesCoordinates)
        {
            switch (renderType)
            {
                case MeshRenderType.VERTEX_COLOR:
                    TgcSkeletalLoader.VertexColorVertex[] data1 = new TgcSkeletalLoader.VertexColorVertex[originalData.coordinatesIndices.Length];
                    for (int i = 0; i < originalData.coordinatesIndices.Length; i++)
                    {
                        TgcSkeletalLoader.VertexColorVertex v = new TgcSkeletalLoader.VertexColorVertex();

                        //vertices
                        int coordIdx = originalData.coordinatesIndices[i] * 3;
                        v.Position = new Vector3(
                            verticesCoordinates[coordIdx],
                            verticesCoordinates[coordIdx + 1],
                            verticesCoordinates[coordIdx + 2]
                            );

                        //color
                        int colorIdx = originalData.colorIndices[i];
                        v.Color = originalData.verticesColors[colorIdx];

                        data1[i] = v;
                    }
                    d3dMesh.SetVertexBufferData(data1, LockFlags.None);
                    break;

                case MeshRenderType.DIFFUSE_MAP:
                    TgcSkeletalLoader.DiffuseMapVertex[] data2 = new TgcSkeletalLoader.DiffuseMapVertex[originalData.coordinatesIndices.Length];
                    for (int i = 0; i < originalData.coordinatesIndices.Length; i++)
                    {
                        TgcSkeletalLoader.DiffuseMapVertex v = new TgcSkeletalLoader.DiffuseMapVertex();

                        //vertices
                        int coordIdx = originalData.coordinatesIndices[i] * 3;
                        v.Position = new Vector3(
                            verticesCoordinates[coordIdx],
                            verticesCoordinates[coordIdx + 1],
                            verticesCoordinates[coordIdx + 2]
                            );

                        //texture coordinates diffuseMap
                        int texCoordIdx = originalData.texCoordinatesIndices[i] * 2;
                        v.Tu = originalData.textureCoordinates[texCoordIdx];
                        v.Tv = originalData.textureCoordinates[texCoordIdx + 1];

                        //color
                        int colorIdx = originalData.colorIndices[i];
                        v.Color = originalData.verticesColors[colorIdx];

                        data2[i] = v;
                    }
                    d3dMesh.SetVertexBufferData(data2, LockFlags.None);
                    break;
            }
        }


        /// <summary>
        /// Renderiza la malla, si esta habilitada.
        /// Para que haya animacion se tiene que haber seteado una y haber
        /// llamado previamente al metodo updateAnimation()
        /// Sino se renderiza la pose fija de la malla
        /// </summary>
        public void render()
        {
            if (!enabled)
                return;

            Device device = GuiController.Instance.D3dDevice;
            TgcTexture.Manager texturesManager = GuiController.Instance.TexturesManager;

            //Actualizar transformacion de malla
            updateMeshTransform();

            //Cargar VertexDeclaration
            device.VertexDeclaration = vertexDeclaration;

            //Activar AlphaBlending si corresponde
            activateAlphaBlend();


            //Renderizar malla
            if (!renderSkeleton)
            {
                //Renderizar segun el tipo de render de la malla
                switch (renderType)
                {
                    case MeshRenderType.VERTEX_COLOR:

                        //Hacer reset de texturas y materiales
                        texturesManager.clear(0);
                        texturesManager.clear(1);
                        device.Material = TgcD3dDevice.DEFAULT_MATERIAL;

                        //Dibujar mesh
                        d3dMesh.DrawSubset(0);
                        break;

                    case MeshRenderType.DIFFUSE_MAP:

                        //Hacer reset de Lightmap
                        texturesManager.clear(1);

                        //Dibujar cada subset con su Material y DiffuseMap correspondiente
                        for (int i = 0; i < materials.Length; i++)
                        {
                            device.Material = materials[i];
                            texturesManager.set(0, diffuseMaps[i]);
                            d3dMesh.DrawSubset(i);
                        }
                        break;
                }
            }
            //Renderizar esqueleto
            else
            {
                this.renderSkeletonMesh();
            }

            //Desactivar alphaBlend
            resetAlphaBlend();


            //Renderizar attachments
            foreach (TgcSkeletalBoneAttach attach in attachments)
            {
                attach.updateMeshTransform(this.transform);
                attach.Mesh.render();
            }
        }

        /// <summary>
        /// Actualizar transformacion actual de la malla
        /// </summary>
        protected void updateMeshTransform()
        {
            if (autoTransformEnable)
            {
                this.transform = Matrix.Identity
                    * Matrix.Scaling(scale)
                    * Matrix.RotationYawPitchRoll(rotation.Y, rotation.X, rotation.Z)
                    * Matrix.Translation(translation);
            }
            GuiController.Instance.D3dDevice.Transform.World = this.transform;
        }

        /// <summary>
        /// Activar AlphaBlending, si corresponde
        /// </summary>
        protected void activateAlphaBlend()
        {
            Device device = GuiController.Instance.D3dDevice;
            if (alphaBlendEnable)
            {
                device.RenderState.AlphaTestEnable = true;
                device.RenderState.AlphaBlendEnable = true;
            }
        }

        /// <summary>
        /// Desactivar AlphaBlending
        /// </summary>
        protected void resetAlphaBlend()
        {
            Device device = GuiController.Instance.D3dDevice;
            device.RenderState.AlphaTestEnable = false;
            device.RenderState.AlphaBlendEnable = false;
        }

        /// <summary>
        /// Dibujar el esqueleto de la malla
        /// </summary>
        protected void renderSkeletonMesh()
        {
            Device device = GuiController.Instance.D3dDevice;
            Vector3 ceroVec = new Vector3(0, 0, 0);

            //Dibujar huesos y joints
            for (int i = 0; i < bones.Length; i++)
            {
                TgcSkeletalBone bone = bones[i];

                //Renderizar Joint
                TgcBox jointBox = skeletonRenderJoints[i];
                jointBox.Transform = bone.MatFinal * this.transform;
                jointBox.render();

                //Modificar línea del bone
                if (bone.ParentBone != null)
                {
                    TgcLine boneLine = skeletonRenderBones[i];

                    boneLine.PStart = Vector3.TransformCoordinate(ceroVec, bone.MatFinal * this.transform);
                    boneLine.PEnd = Vector3.TransformCoordinate(ceroVec, bone.ParentBone.MatFinal * this.transform);
                    boneLine.updateValues();
                }
            }

            //Dibujar bones
            foreach (TgcLine boneLine in skeletonRenderBones)
            {
                if (boneLine != null)
                {
                    boneLine.render();
                }
            }
        }



        /// <summary>
        /// Actualiza el cuadro actual de animacion y renderiza la malla.
        /// Es equivalente a llamar a updateAnimation() y luego a render()
        /// </summary>
        public void animateAndRender()
        {
            if (!enabled)
                return;

            updateAnimation();
            render();
        }


        /// <summary>
        /// Libera los recursos de la malla
        /// </summary>
        public void dispose()
        {
            this.enabled = false;
            if (boundingBox != null)
            {
                boundingBox.dispose();
            }

            //dejar de utilizar originalData
            originalData = null;

            //Si es una instancia no liberar nada, lo hace el original.
            if (parentInstance != null)
            {
                parentInstance = null;
                return;
            }

            //hacer dispose de instancias
            foreach (TgcSkeletalMesh meshInstance in meshInstances)
            {
                meshInstance.dispose();
            }
            meshInstances = null;

            //Dispose de mesh
            this.d3dMesh.Dispose();
            this.d3dMesh = null;

            //Dispose de texturas
            if (diffuseMaps != null)
            {
                for (int i = 0; i < diffuseMaps.Length; i++)
                {
                    diffuseMaps[i].dispose();
                }
                diffuseMaps = null;
            }
            

            //Dispose de Box de joints
            if (skeletonRenderJoints != null)
            {
                foreach (TgcBox jointBox in skeletonRenderJoints)
                {
                    jointBox.dispose();
                }
                skeletonRenderJoints = null;
            }
            
            //Dispose de lineas de Bones
            if (skeletonRenderBones != null)
            {
                foreach (TgcLine boneLine in skeletonRenderBones)
                {
                    if (boneLine != null)
                    {
                        boneLine.dispose();
                    }
                }
                skeletonRenderBones = null;
            }

            //Liberar attachments
            foreach (TgcSkeletalBoneAttach attach in attachments)
            {
                attach.Mesh.dispose();
            }
            attachments = null;

            //VertexDeclaration
            vertexDeclaration.Dispose();
            vertexDeclaration = null;
        }


        /// <summary>
        /// Desplaza la malla la distancia especificada, respecto de su posicion actual
        /// </summary>
        public void move(Vector3 v)
        {
            this.move(v.X, v.Y, v.Z);
        }

        /// <summary>
        /// Desplaza la malla la distancia especificada, respecto de su posicion actual
        /// </summary>
        public void move(float x, float y, float z)
        {
            this.translation.X += x;
            this.translation.Y += y;
            this.translation.Z += z;

            updateBoundingBox();
        }

        /// <summary>
        /// Mueve la malla en base a la orientacion actual de rotacion.
        /// Es necesario rotar la malla primero
        /// </summary>
        /// <param name="movement">Desplazamiento. Puede ser positivo (hacia adelante) o negativo (hacia atras)</param>
        public void moveOrientedY(float movement)
        {
            float z = (float)Math.Cos((float)rotation.Y) * movement;
            float x = (float)Math.Sin((float)rotation.Y) * movement;

            move(x, 0, z);
        }

        /// <summary>
        /// Obtiene la posicion absoluta de la malla, recibiendo un vector ya creado para
        /// almacenar el resultado
        /// </summary>
        /// <param name="pos">Vector ya creado en el que se carga el resultado</param>
        public void getPosition(Vector3 pos)
        {
            pos.X = translation.X;
            pos.Y = translation.Y;
            pos.Z = translation.Z;
        }

        /// <summary>
        /// Rota la malla respecto del eje X
        /// </summary>
        /// <param name="angle">Ángulo de rotación en radianes</param>
        public void rotateX(float angle)
        {
            this.rotation.X += angle;
        }

        /// <summary>
        /// Rota la malla respecto del eje Y
        /// </summary>
        /// <param name="angle">Ángulo de rotación en radianes</param>
        public void rotateY(float angle)
        {
            this.rotation.Y += angle;
        }

        /// <summary>
        /// Rota la malla respecto del eje Z
        /// </summary>
        /// <param name="angle">Ángulo de rotación en radianes</param>
        public void rotateZ(float angle)
        {
            this.rotation.Z += angle;
        }

        /// <summary>
        /// Devuelve un array con todas las posiciones de los vértices de la malla, en el estado actual
        /// </summary>
        /// <returns>Array creado</returns>
        public Vector3[] getVertexPositions()
        {
            Vector3[] points = null;
            switch (renderType)
            {
                case MeshRenderType.VERTEX_COLOR:
                    TgcSkeletalLoader.VertexColorVertex[] verts1 = (TgcSkeletalLoader.VertexColorVertex[])d3dMesh.LockVertexBuffer(
                        typeof(TgcSkeletalLoader.VertexColorVertex), LockFlags.ReadOnly, d3dMesh.NumberVertices);
                    points = new Vector3[verts1.Length];
                    for (int i = 0; i < points.Length; i++)
                    {
                        points[i] = verts1[i].Position;
                    }
                    d3dMesh.UnlockVertexBuffer();
                    break;

                case MeshRenderType.DIFFUSE_MAP:
                    TgcSkeletalLoader.DiffuseMapVertex[] verts2 = (TgcSkeletalLoader.DiffuseMapVertex[])d3dMesh.LockVertexBuffer(
                        typeof(TgcSkeletalLoader.DiffuseMapVertex), LockFlags.ReadOnly, d3dMesh.NumberVertices);
                    points = new Vector3[verts2.Length];
                    for (int i = 0; i < points.Length; i++)
                    {
                        points[i] = verts2[i].Position;
                    }
                    d3dMesh.UnlockVertexBuffer();
                    break;
            }
            return points;
        }

        /// <summary>
        /// Calcula el BoundingBox de la malla, en base a todos sus vertices.
        /// Llamar a este metodo cuando ha cambiado la estructura interna de la malla.
        /// </summary>
        public TgcBoundingBox createBoundingBox()
        {
            //Obtener vertices en base al tipo de malla
            Vector3[] points = getVertexPositions();
            this.boundingBox = TgcBoundingBox.computeFromPoints(points);
            return this.boundingBox;
        }

        /// <summary>
        /// Actualiza el BoundingBox de la malla, en base a su posicion actual.
        /// Solo contempla traslacion y escalado
        /// </summary>
        public void updateBoundingBox()
        {
            if (autoUpdateBoundingBox)
            {
                this.boundingBox.scaleTranslate(this.translation, this.scale);
            }
        }

        /// <summary>
        /// Cambia el color de todos los vértices de la malla.
        /// En modelos complejos puede resultar una operación poco performante.
        /// La actualización será visible la próxima vez que se haga updateAnimation().
        /// Si hay instnacias de este modelo, sea el original o una copia, todos los demás se verán
        /// afectados
        /// </summary>
        /// <param name="color">Color nuevo</param>
        public void setColor(Color color)
        {
            int c = color.ToArgb();
            for (int i = 0; i < originalData.verticesColors.Length; i++)
            {
                originalData.verticesColors[i] = c;
            }
        }

        /// <summary>
        /// Permite cambiar las texturas de DiffuseMap de esta malla
        /// </summary>
        /// <param name="newDiffuseMaps">Array de nuevas texturas. Tiene que tener la misma cantidad que el original</param>
        public void changeDiffuseMaps(TgcTexture[] newDiffuseMaps)
        {
            //Solo aplicar si la malla tiene texturas
            if (renderType == MeshRenderType.DIFFUSE_MAP)
            {
                if (diffuseMaps.Length != newDiffuseMaps.Length)
                {
                    throw new Exception("The new DiffuseMap array does not have the same length than the original.");
                }

                //Liberar texturas anteriores
                foreach (TgcTexture t in diffuseMaps)
                {
                    t.dispose();
                }

                //Asignar nuevas texturas
                this.diffuseMaps = newDiffuseMaps;
            }
        }

        /// <summary>
        /// Busca el hueso con el nombre especificado.
        /// </summary>
        /// <param name="boneName">Nombre del hueso buscado</param>
        /// <returns>Hueso encontrado o null si no lo encontró</returns>
        public TgcSkeletalBone getBoneByName(string boneName)
        {
            foreach (TgcSkeletalBone bone in bones)
            {
                if (bone.Name.Equals(boneName))
                {
                    return bone;
                }
            }
            return null;
        }

        /// <summary>
        /// Crea una nueva malla que es una instancia de esta malla original
        /// Reutiliza toda la geometría de la malla original sin duplicarla.
        /// Solo se puede crear instancias a partir de originales.
        /// Se debe crear después de haber agregado todas las animaciones al original.
        /// Los attachments de la malla original se duplican.
        /// </summary>
        /// <param name="name">Nombre de la malla</param>
        /// <param name="translation">Traslación respecto de la malla original</param>
        /// <param name="rotation">Rotación respecto de la malla original</param>
        /// <param name="scale">Escala respecto de la malla original</param>
        public TgcSkeletalMesh createMeshInstance(string name, Vector3 translation, Vector3 rotation, Vector3 scale)
        {
            if (this.parentInstance != null)
            {
                throw new Exception("No se puede crear una instancia de otra malla instancia. Hay que partir del original.");
            }

            //Crear instancia
            TgcSkeletalMesh instance = new TgcSkeletalMesh(name, this, translation, rotation, scale);

            //BoundingBox
            instance.boundingBox = new TgcBoundingBox(this.boundingBox.PMin, this.boundingBox.PMax);
            instance.updateBoundingBox();

            instance.enabled = true;
            return instance;
        }

        /// <summary>
        /// Crea una nueva malla que es una instancia de esta malla original
        /// Reutiliza toda la geometría de la malla original sin duplicarla.
        /// Solo se puede crear instancias a partir de originales.
        /// Se debe crear después de haber agregado todas las animaciones al original.
        /// Los attachments de la malla original se duplican.
        /// </summary>
        /// <param name="name">Nombre de la malla</param>
        public TgcSkeletalMesh createMeshInstance(string name)
        {
            return createMeshInstance(name, Vector3.Empty, Vector3.Empty, new Vector3(1, 1, 1));
        }


        /// <summary>
        /// Informacion del MeshData original que hay que guardar para poder alterar el VertexBuffer con la animacion
        /// </summary>
        public class OriginalData
        {
            public int[] coordinatesIndices;
            public float[] verticesCoordinates;
            public int[] texCoordinatesIndices;
            public int[] colorIndices;
            public float[] textureCoordinates;
            public int[] verticesColors;
        }

        public override string ToString()
        {
            return "Mesh: " + name;
        }


    }
}
