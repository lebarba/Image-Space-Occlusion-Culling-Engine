using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using TgcViewer.Utils.TgcSceneLoader;
using System.Drawing;

namespace TgcViewer.Utils.TgcGeometry
{
    /// <summary>
    /// Representa un polígono convexo plano en 3D de una sola cara, compuesto
    /// por varios vértices que lo delimitan.
    /// </summary>
    public class TgcConvexPolygon : IRenderObject
    {
        public TgcConvexPolygon()
        {
            this.enabled = true;
            this.alphaBlendEnable = false;
            this.color = Color.Purple;
        }


        private Vector3[] boundingVertices;
        /// <summary>
        /// Vertices que definen el contorno polígono.
        /// Están dados en clockwise-order.
        /// </summary>
        public Vector3[] BoundingVertices
        {
            get { return boundingVertices; }
            set { boundingVertices = value; }
        }

        private bool enabled;
        /// <summary>
        /// Indica si la flecha esta habilitada para ser renderizada
        /// </summary>
        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }


        # region Renderizado del poligono


        VertexBuffer vertexBuffer;

        /// <summary>
        /// Actualizar valores de renderizado.
        /// Hay que llamarlo al menos una vez para poder hacer render()
        /// </summary>
        public void updateValues()
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            //Crear VertexBuffer on demand
            if (vertexBuffer == null || vertexBuffer.Disposed)
            {
                vertexBuffer = new VertexBuffer(typeof(CustomVertex.PositionColored), boundingVertices.Length, d3dDevice,
                    Usage.Dynamic | Usage.WriteOnly, CustomVertex.PositionColored.Format, Pool.Default);
            }

            //Crear como TriangleFan
            int c = color.ToArgb();
            CustomVertex.PositionColored[] vertices = new CustomVertex.PositionColored[boundingVertices.Length];
            for (int i = 0; i < boundingVertices.Length; i++)
            {
                vertices[i] = new CustomVertex.PositionColored(boundingVertices[i], c);
            }

            //Cargar vertexBuffer
            vertexBuffer.SetData(vertices, 0, LockFlags.None);
        }

        /// <summary>
        /// Renderizar el polígono
        /// </summary>
        public void render()
        {
            if (!enabled)
                return;

            Device d3dDevice = GuiController.Instance.D3dDevice;
            TgcTexture.Manager texturesManager = GuiController.Instance.TexturesManager;

            texturesManager.clear(0);
            d3dDevice.Material = TgcD3dDevice.DEFAULT_MATERIAL;
            d3dDevice.Transform.World = Matrix.Identity;

            //Renderizar RenderFarm
            d3dDevice.VertexFormat = CustomVertex.PositionColored.Format;
            d3dDevice.SetStreamSource(0, vertexBuffer, 0);
            d3dDevice.DrawPrimitives(PrimitiveType.TriangleFan, 0, boundingVertices.Length - 2);
        }

        /// <summary>
        /// Liberar recursos del polígono
        /// </summary>
        public void dispose()
        {
            if (vertexBuffer != null && !vertexBuffer.Disposed)
            {
                vertexBuffer.Dispose();
            }
        }

        public Vector3 Position
        {
            //Lo correcto sería calcular el centro, pero con un extremo es suficiente.
            get { return boundingVertices[0]; }
        }

        Color color;
        /// <summary>
        /// Color del polígono
        /// </summary>
        public Color Color
        {
            get { return color; }
            set { color = value; }
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


        # endregion

    }
}
