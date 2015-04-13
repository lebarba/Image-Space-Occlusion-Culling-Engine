using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX.Direct3D;
using System.Drawing;
using Microsoft.DirectX;
using TgcViewer.Utils.TgcSceneLoader;

namespace TgcViewer.Utils.TgcGeometry
{
    /// <summary>
    /// Herramienta para crear una l�nea 3D y renderizarla con color.
    /// </summary>
    public class TgcLine : IRenderObject
    {

        #region Creacion

        /// <summary>
        /// Crea una l�nea en base a sus puntos extremos
        /// </summary>
        /// <param name="start">Punto de inicio</param>
        /// <param name="end">Punto de fin</param>
        /// <returns>L�nea creada</returns>
        public static TgcLine fromExtremes(Vector3 start, Vector3 end)
        {
            TgcLine line = new TgcLine();
            line.pStart = start;
            line.pEnd = end;
            line.updateValues();
            return line;
        }

        /// <summary>
        /// Crea una l�nea en base a sus puntos extremos, con el color especificado
        /// </summary>
        /// <param name="start">Punto de inicio</param>
        /// <param name="end">Punto de fin</param>
        /// <param name="color">Color de la l�nea</param>
        /// <returns>L�nea creada</returns>
        public static TgcLine fromExtremes(Vector3 start, Vector3 end, Color color)
        {
            TgcLine line = new TgcLine();
            line.pStart = start;
            line.pEnd = end;
            line.color = color;
            line.updateValues();
            return line;
        }

        #endregion


        CustomVertex.PositionColored[] vertices;

        Vector3 pStart;
        /// <summary>
        /// Punto de inicio de la linea
        /// </summary>
        public Vector3 PStart
        {
            get { return pStart; }
            set { pStart = value; }
        }

        Vector3 pEnd;
        /// <summary>
        /// Punto final de la linea
        /// </summary>
        public Vector3 PEnd
        {
            get { return pEnd; }
            set { pEnd = value; }
        }

        Color color;
        /// <summary>
        /// Color de la linea
        /// </summary>
        public Color Color
        {
            get { return color; }
            set { color = value; }
        }

        private bool enabled;
        /// <summary>
        /// Indica si la linea esta habilitada para ser renderizada
        /// </summary>
        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        public Vector3 Position
        {
            //Lo correcto ser�a calcular el centro, pero con un extremo es suficiente.
            get { return pStart; }
        }

        private bool alphaBlendEnable;
        /// <summary>
        /// Habilita el renderizado con AlphaBlending para los modelos
        /// con textura o colores por v�rtice de canal Alpha.
        /// Por default est� deshabilitado.
        /// </summary>
        public bool AlphaBlendEnable
        {
            get { return alphaBlendEnable; }
            set { alphaBlendEnable = value; }
        }

        public TgcLine()
        {
            vertices = new CustomVertex.PositionColored[2];
            this.color = Color.White;
            this.enabled = true;
            this.alphaBlendEnable = false;
        }

        /// <summary>
        /// Actualizar par�metros de la l�nea en base a los valores configurados
        /// </summary>
        public void updateValues()
        {
            int c = color.ToArgb();

            vertices[0] = new CustomVertex.PositionColored(pStart, c);
            vertices[1] = new CustomVertex.PositionColored(pEnd, c);
        }


        /// <summary>
        /// Renderizar la l�nea
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

            d3dDevice.VertexFormat = CustomVertex.PositionColored.Format;
            d3dDevice.DrawUserPrimitives(PrimitiveType.LineList, 1, vertices);
        }

        /// <summary>
        /// Liberar recursos de la l�nea
        /// </summary>
        public void dispose()
        {
        }


    }
}
