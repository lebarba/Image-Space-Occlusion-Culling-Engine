using System;
using System.Collections.Generic;
using System.Text;
using TgcViewer.Utils.TgcSceneLoader;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System.Drawing;
using TgcViewer;
using TgcViewer.Utils;
using TgcViewer.Utils.TgcGeometry;

namespace Examples.Obb
{
    public class TgcObb : IRenderObject
    {

        Vector3 center;
        /// <summary>
        /// Centro
        /// </summary>
        public Vector3 Center
        {
            get { return center; }
            set { center = value; }
        }

        Vector3[] orientation = new Vector3[3];
        /// <summary>
        /// Orientacion del OBB, expresada en local axes
        /// </summary>
        public Vector3[] Orientation
        {
            get { return orientation; }
            set { orientation = value; }
        }

        Vector3 extents;
        /// <summary>
        /// Radios
        /// </summary>
        public Vector3 Extents
        {
            get { return extents; }
            set { extents = value; }
        }


        int renderColor;
        /// <summary>
        /// Color de renderizado del BoundingBox.
        /// </summary>
        public int RenderColor
        {
            get { return renderColor; }
        }


        CustomVertex.PositionColored[] vertices;
        bool dirtyValues;

        /// <summary>
        /// Construir OBB vacio
        /// </summary>
        public TgcObb()
        {
            renderColor = Color.Yellow.ToArgb();
            dirtyValues = true;
            alphaBlendEnable = false;
        }

        /// <summary>
        /// Configurar el color de renderizado del OBB
        /// Ejemplo: Color.Yellow.ToArgb();
        /// </summary>
        public void setRenderColor(Color color)
        {
            this.renderColor = color.ToArgb();
            dirtyValues = true;
        }

        public Vector3 Position
        {
            get { return center; }
        }

        private bool alphaBlendEnable;
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
        /// Actualizar los valores de los vertices a renderizar
        /// </summary>
        public void updateValues()
        {
            if (vertices == null)
            {
                vertices = vertices = new CustomVertex.PositionColored[24];
            }

            Vector3[] corners = computeCorners(); 


            //Cuadrado de atras
            vertices[0] = new CustomVertex.PositionColored(corners[0], renderColor);
            vertices[1] = new CustomVertex.PositionColored(corners[4], renderColor);

            vertices[2] = new CustomVertex.PositionColored(corners[0], renderColor);
            vertices[3] = new CustomVertex.PositionColored(corners[2], renderColor);

            vertices[4] = new CustomVertex.PositionColored(corners[2], renderColor);
            vertices[5] = new CustomVertex.PositionColored(corners[6], renderColor);

            vertices[6] = new CustomVertex.PositionColored(corners[4], renderColor);
            vertices[7] = new CustomVertex.PositionColored(corners[6], renderColor);

            //Cuadrado de adelante
            vertices[8] = new CustomVertex.PositionColored(corners[1], renderColor);
            vertices[9] = new CustomVertex.PositionColored(corners[5], renderColor);

            vertices[10] = new CustomVertex.PositionColored(corners[1], renderColor);
            vertices[11] = new CustomVertex.PositionColored(corners[3], renderColor);

            vertices[12] = new CustomVertex.PositionColored(corners[3], renderColor);
            vertices[13] = new CustomVertex.PositionColored(corners[7], renderColor);

            vertices[14] = new CustomVertex.PositionColored(corners[5], renderColor);
            vertices[15] = new CustomVertex.PositionColored(corners[7], renderColor);

            //Union de ambos cuadrados
            vertices[16] = new CustomVertex.PositionColored(corners[0], renderColor);
            vertices[17] = new CustomVertex.PositionColored(corners[1], renderColor);

            vertices[18] = new CustomVertex.PositionColored(corners[4], renderColor);
            vertices[19] = new CustomVertex.PositionColored(corners[5], renderColor);

            vertices[20] = new CustomVertex.PositionColored(corners[2], renderColor);
            vertices[21] = new CustomVertex.PositionColored(corners[3], renderColor);

            vertices[22] = new CustomVertex.PositionColored(corners[6], renderColor);
            vertices[23] = new CustomVertex.PositionColored(corners[7], renderColor);
        }

        /// <summary>
        /// Crea un array con los 8 vertices del OBB
        /// </summary>
        private Vector3[] computeCorners()
        {
            Vector3[] corners = new Vector3[8];

            Vector3 eX = extents.X * orientation[0];
            Vector3 eY = extents.Y * orientation[1];
            Vector3 eZ = extents.Z * orientation[2];




            corners[0] = center - eX - eY - eZ;
            corners[1] = center - eX - eY + eZ;

            corners[2] = center - eX + eY - eZ;
            corners[3] = center - eX + eY + eZ;

            corners[4] = center + eX - eY - eZ;
            corners[5] = center + eX - eY + eZ;

            corners[6] = center + eX + eY - eZ;
            corners[7] = center + eX + eY + eZ;

            return corners;
        }

        /// <summary>
        /// Renderizar BoundingBox
        /// </summary>
        public void render()
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;
            TgcTexture.Manager texturesManager = GuiController.Instance.TexturesManager;

            texturesManager.clear(0);
            texturesManager.clear(1);
            d3dDevice.Material = TgcD3dDevice.DEFAULT_MATERIAL;
            d3dDevice.Transform.World = Matrix.Identity;

            //Actualizar vertices de BoundingBox solo si hubo una modificación
            if (dirtyValues)
            {
                updateValues();
                dirtyValues = false;
            }

            d3dDevice.VertexFormat = CustomVertex.PositionColored.Format;
            d3dDevice.DrawUserPrimitives(PrimitiveType.LineList, 12, vertices);
        }

        /// <summary>
        /// Libera los recursos del objeto
        /// </summary>
        public void dispose()
        {
            vertices = null;
        }

        /// <summary>
        /// Mueve el centro del OBB
        /// </summary>
        /// <param name="movement">Movimiento relativo que se quiere aplicar</param>
        public void move(Vector3 movement)
        {
            center += movement;
            dirtyValues = true;
        }

        /// <summary>
        /// Rotar OBB en los 3 ejes
        /// </summary>
        /// <param name="movement">Ángulo de rotación de cada eje en radianes</param>
        public void rotate(Vector3 rotation)
        {
            Matrix rotM = Matrix.RotationYawPitchRoll(rotation.Y, rotation.X, rotation.Z);
            orientation[0] = new Vector3(rotM.M11, rotM.M12, rotM.M13);
            orientation[1] = new Vector3(rotM.M21, rotM.M22, rotM.M23);
            orientation[2] = new Vector3(rotM.M31, rotM.M32, rotM.M33);

            dirtyValues = true;
        }

        /// <summary>
        /// Calcula la matriz de rotacion 4x4 del Obb en base a su orientacion
        /// </summary>
        /// <returns>Matriz de rotacion de 4x4</returns>
        public Matrix computeRotationMatrix()
        {
            Matrix rot = Matrix.Identity;

            rot.M11 = orientation[0].X;
            rot.M12 = orientation[0].Y;
            rot.M13 = orientation[0].Z;

            rot.M21 = orientation[1].X;
            rot.M22 = orientation[1].Y;
            rot.M23 = orientation[1].Z;

            rot.M31 = orientation[2].X;
            rot.M32 = orientation[2].Y;
            rot.M33 = orientation[2].Z;

            return rot;
        }


        public static TgcObb computeFromPoints(Vector3[] points)
        {
            return TgcObb.computeFromPointsRecursive(points, new Vector3(0, 0, 0), new Vector3(360, 360, 360), 10f);
        }

        private static TgcObb computeFromPointsRecursive(Vector3[] points, Vector3 initValues, Vector3 endValues, float step)
        {
            TgcObb minObb = new TgcObb();
            float minVolume = float.MaxValue;
            Vector3 minInitValues = Vector3.Empty;
            Vector3 minEndValues = Vector3.Empty;
            Vector3[] transformedPoints = new Vector3[points.Length];
            float x, y, z;
            

            x = initValues.X;
            while(x <= endValues.X)
            {
                y = initValues.Y;
                float rotX = FastMath.ToRad(x);
                while (y <= endValues.Y)
                {
                    z = initValues.Z;
                    float rotY = FastMath.ToRad(y);
                    while (z <= endValues.Z)
                    {
                        //Matriz de rotacion
                        float rotZ = FastMath.ToRad(z);
                        Matrix rotM = Matrix.RotationYawPitchRoll(rotY, rotX, rotZ);
                        Vector3[] orientation = new Vector3[]{
                                new Vector3(rotM.M11, rotM.M12, rotM.M13),
                                new Vector3(rotM.M21, rotM.M22, rotM.M23),
                                new Vector3(rotM.M31, rotM.M32, rotM.M33)
                            };

                        //Transformar todos los puntos a OBB-space
                        for (int i = 0; i < transformedPoints.Length; i++)
                        {
                            transformedPoints[i].X = Vector3.Dot(points[i], orientation[0]);
                            transformedPoints[i].Y = Vector3.Dot(points[i], orientation[1]);
                            transformedPoints[i].Z = Vector3.Dot(points[i], orientation[2]);
                        }

                        //Obtener el AABB de todos los puntos transformados
                        TgcBoundingBox aabb = TgcBoundingBox.computeFromPoints(transformedPoints);

                        //Calcular volumen del AABB
                        Vector3 extents = aabb.calculateAxisRadius();
                        extents = TgcVectorUtils.abs(extents);
                        float volume = extents.X * 2 * extents.Y * 2 * extents.Z * 2;

                        //Buscar menor volumen
                        if (volume < minVolume)
                        {
                            minVolume = volume;
                            minInitValues = new Vector3(x, y, z);
                            minEndValues = new Vector3(x + step, y + step, z + step);

                            //Volver centro del AABB a World-space
                            Vector3 center = aabb.calculateBoxCenter();
                            center = center.X * orientation[0] + center.Y * orientation[1] + center.Z * orientation[2];

                            //Crear OBB
                            minObb.center = center;
                            minObb.extents = extents;
                            minObb.orientation = orientation;
                        }

                        z += step;
                    }
                    y += step;
                }
                x += step;
            }

            //Recursividad en mejor intervalo encontrado
            if (step > 0.01f)
            {
                minObb = computeFromPointsRecursive(points, minInitValues, minEndValues, step / 10f);
            }

            return minObb;
        }

        /// <summary>
        /// Convertir un punto al espacio de coordenadas del OBB
        /// </summary>
        /// <param name="p">Punto en World-space</param>
        /// <returns>Punto convertido a OBB-space</returns>
        public Vector3 toObbSpace(Vector3 p)
        {
            Vector3 t = p - center;
            return new Vector3(Vector3.Dot(t, orientation[0]), Vector3.Dot(t, orientation[1]), Vector3.Dot(t, orientation[2]));
        }

        /// <summary>
        /// Convertir un punto de OBB-space a World-space
        /// </summary>
        /// <param name="p">Punto en OBB-space</param>
        /// <returns>Punto convertido a World-space</returns>
        public Vector3 toWorldSpace(Vector3 p)
        {
            return center + p.X * orientation[0] + p.Y * orientation[1] + p.Z * orientation[2];
        }

    }
}
