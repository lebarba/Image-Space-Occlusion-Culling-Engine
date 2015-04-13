using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System.Drawing;
using TgcViewer.Utils.TgcSceneLoader;

namespace TgcViewer.Utils.TgcGeometry
{
    /// <summary>
    /// Representa un volumen de esfera con un centro y un radio
    /// </summary>
    public class TgcBoundingSphere : IRenderObject
    {
        /// <summary>
        /// Cantidad de tramos que tendrá el mesh del BoundingSphere a dibujar
        /// </summary>
        public const int SPHERE_MESH_RESOLUTION = 10;

        bool dirtyValues;
        CustomVertex.PositionColored[] vertices;

        /// <summary>
        /// Crear BoundingSphere vacia
        /// </summary>
        public TgcBoundingSphere()
        {
            this.renderColor = Color.Yellow.ToArgb();
            this.dirtyValues = true;
            this.alphaBlendEnable = false;
        }

        /// <summary>
        /// Crear BoundingSphere con centro y radio
        /// </summary>
        /// <param name="center">Centro</param>
        /// <param name="radius">Radio</param>
        public TgcBoundingSphere(Vector3 center, float radius) : this()
        {
            setValues(center, radius);
        }

        /// <summary>
        /// Configurar valores del BoundingSphere
        /// </summary>
        /// <param name="center">Centro</param>
        /// <param name="radius">Radio</param>
        public void setValues(Vector3 center, float radius)
        {
            this.center = center;
            this.radius = radius;

            this.dirtyValues = true;
        }

        /// <summary>
        /// Configurar un nuevo centro del BoundingSphere
        /// </summary>
        /// <param name="center">Nuevo centro</param>
        public void setCenter(Vector3 center)
        {
            setValues(center, this.radius);
        }

        /// <summary>
        /// Desplazar el centro respecto de su posición actual
        /// </summary>
        /// <param name="movement">Movimiento relativo a realizar</param>
        public void moveCenter(Vector3 movement)
        {
            setValues(this.center + movement, this.radius);
        }

        Vector3 center;
        /// <summary>
        /// Centro de la esfera
        /// </summary>
        public Vector3 Center
        {
            get { return center; }
        }

        float radius;
        /// <summary>
        /// Radio de la esfera
        /// </summary>
        public float Radius
        {
            get { return radius; }
        }

        int renderColor;
        /// <summary>
        /// Color de renderizado del BoundingBox.
        /// </summary>
        public int RenderColor
        {
            get { return renderColor; }
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
        /// Configurar el color de renderizado del BoundingBox
        /// Ejemplo: Color.Yellow.ToArgb();
        /// </summary>
        public void setRenderColor(Color color)
        {
            this.renderColor = color.ToArgb();
            dirtyValues = true;
        }


        /* TODO: Cambiar para que use VertexDeclaration
        /// <summary>
        /// Crea un BoundingSphere en base a un mesh de DirectX
        /// </summary>
        /// <param name="d3dMesh">Mesh de DirectX</param>
        /// <returns>BoundingSphere creado</returns>
        public static TgcBoundingSphere computeFromMesh(Mesh d3dMesh)
        {
            TgcBoundingSphere sphere;
            using (VertexBuffer vb = d3dMesh.VertexBuffer)
            {
                GraphicsStream vertexData = vb.Lock(0, 0, LockFlags.None);
                Vector3 center;
                float radius = Geometry.ComputeBoundingSphere(vertexData, d3dMesh.NumberVertices, d3dMesh.VertexFormat, out center);
                sphere = new TgcBoundingSphere(center, radius);

                vb.Unlock();
            }
            return sphere;
        }
        */

        /// <summary>
        /// Construye el mesh del BoundingSphere
        /// </summary>
        private void updateValues()
        {
            if (vertices == null)
            {
                int verticesCount = (SPHERE_MESH_RESOLUTION * 2 + 2) * 3;
                this.vertices = new CustomVertex.PositionColored[verticesCount];
            }

            int index = 0;

            float step = FastMath.TWO_PI / (float)SPHERE_MESH_RESOLUTION;
            // Plano XY
            for (float a = 0f; a <= FastMath.TWO_PI; a += step)
            {
                vertices[index++] = new CustomVertex.PositionColored(new Vector3(FastMath.Cos(a) * radius, FastMath.Sin(a) * radius, 0f) + center, renderColor);
                vertices[index++] = new CustomVertex.PositionColored(new Vector3(FastMath.Cos(a + step) * radius, FastMath.Sin(a + step) * radius, 0f) + center, renderColor);
            }

            // Plano XZ
            for (float a = 0f; a <= FastMath.TWO_PI; a += step)
            {
                vertices[index++] = new CustomVertex.PositionColored(new Vector3(FastMath.Cos(a) * radius, 0f, FastMath.Sin(a) * radius) + center, renderColor);
                vertices[index++] = new CustomVertex.PositionColored(new Vector3(FastMath.Cos(a + step) * radius, 0f, FastMath.Sin(a + step) * radius) + center, renderColor);
            }

            // Plano YZ
            for (float a = 0f; a <= FastMath.TWO_PI; a += step)
            {
                vertices[index++] = new CustomVertex.PositionColored(new Vector3(0f, FastMath.Cos(a) * radius, FastMath.Sin(a) * radius) + center, renderColor);
                vertices[index++] = new CustomVertex.PositionColored(new Vector3(0f, FastMath.Cos(a + step) * radius, FastMath.Sin(a + step) * radius) + center, renderColor);
            }
        }


        /// <summary>
        /// Renderizar el BoundingSphere
        /// </summary>
        public void render()
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;
            TgcTexture.Manager texturesManager = GuiController.Instance.TexturesManager;

            texturesManager.clear(0);
            texturesManager.clear(1);
            d3dDevice.Material = TgcD3dDevice.DEFAULT_MATERIAL;
            d3dDevice.Transform.World = Matrix.Identity;

            //Actualizar vertices de BoundingSphere solo si hubo una modificación
            if (dirtyValues)
            {
                updateValues();
                dirtyValues = false;
            }

            d3dDevice.VertexFormat = CustomVertex.PositionColored.Format;
            d3dDevice.DrawUserPrimitives(PrimitiveType.LineList, vertices.Length / 2, vertices);
        }

        /// <summary>
        /// Libera los recursos del objeto
        /// </summary>
        public void dispose()
        {
            vertices = null;
        }

        public override string ToString()
        {
            return "Center[" + TgcParserUtils.printFloat(center.X) + ", " + TgcParserUtils.printFloat(center.Y) + ", " + TgcParserUtils.printFloat(center.Z) + "]" + " Radius[" + TgcParserUtils.printFloat(radius) + "]";
        }

    }
}
