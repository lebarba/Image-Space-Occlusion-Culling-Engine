using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX.Direct3D;
using TgcViewer;
using Microsoft.DirectX;
using System.Drawing;
using TgcViewer.Utils.TgcSceneLoader;
using TgcViewer.Utils;

namespace Examples.Voxelization2
{

    /// <summary>
    /// Herramienta de debug para renderizar un voxel
    /// </summary>
    public class VoxelMesh
    {

        VertexBuffer vertexBuffer;

        Effect effect;
        /// <summary>
        /// Shader de voxel
        /// </summary>
        public Effect Effect
        {
            get { return effect; }
            set { effect = value; }
        }

        Vector3 pMin;
        /// <summary>
        /// Punto minimo del voxel
        /// </summary>
        public Vector3 PMin
        {
            get { return pMin; }
            set { pMin = value; }
        }

        Vector3 pMax;
        /// <summary>
        /// Punto maximo del voxel
        /// </summary>
        public Vector3 PMax
        {
            get { return pMax; }
            set { pMax = value; }
        }

        Color color;
        /// <summary>
        /// Color del voxel
        /// </summary>
        public Color Color
        {
            get { return color; }
            set { color = value; }
        }

        float alphaBlendingValue;
        /// <summary>
        /// Transparencia (0, 1)
        /// </summary>
        public float AlphaBlendingValue
        {
            get { return alphaBlendingValue; }
            set { alphaBlendingValue = value; }
        }

        public VoxelMesh()
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            vertexBuffer = new VertexBuffer(typeof(CustomVertex.PositionColoredTextured), 36, d3dDevice,
                Usage.Dynamic | Usage.WriteOnly, CustomVertex.PositionColoredTextured.Format, Pool.Default);

            color = Color.White;
            alphaBlendingValue = 1;
        }

        /// <summary>
        /// Actualiza el mesh en base a los valores configurados
        /// </summary>
        public void updateValues()
        {
            CustomVertex.PositionColoredTextured[] vertices = new CustomVertex.PositionColoredTextured[36];

            int c = color.ToArgb();
            Vector3 extent = (pMax - pMin) * 0.5f;
            float x = extent.X;
            float y = extent.Y;
            float z = extent.Z;
            float u = 1f;
            float v = 1f;
            float offsetU = 0;
            float offsetV = 0;

            // Front face
            vertices[0] = new CustomVertex.PositionColoredTextured(-x, y, z, c, offsetU, offsetV);
            vertices[1] = new CustomVertex.PositionColoredTextured(-x, -y, z, c, offsetU, offsetV + v);
            vertices[2] = new CustomVertex.PositionColoredTextured(x, y, z, c, offsetU + u, offsetV);
            vertices[3] = new CustomVertex.PositionColoredTextured(-x, -y, z, c, offsetU, offsetV + v);
            vertices[4] = new CustomVertex.PositionColoredTextured(x, -y, z, c, offsetU + u, offsetV + v);
            vertices[5] = new CustomVertex.PositionColoredTextured(x, y, z, c, offsetU + u, offsetV);

            // Back face (remember this is facing *away* from the camera, so vertices should be clockwise order)
            vertices[6] = new CustomVertex.PositionColoredTextured(-x, y, -z, c, offsetU, offsetV);
            vertices[7] = new CustomVertex.PositionColoredTextured(x, y, -z, c, offsetU + u, offsetV);
            vertices[8] = new CustomVertex.PositionColoredTextured(-x, -y, -z, c, offsetU, offsetV + v);
            vertices[9] = new CustomVertex.PositionColoredTextured(-x, -y, -z, c, offsetU, offsetV + v);
            vertices[10] = new CustomVertex.PositionColoredTextured(x, y, -z, c, offsetU + u, offsetV);
            vertices[11] = new CustomVertex.PositionColoredTextured(x, -y, -z, c, offsetU + u, offsetV + v);

            // Top face
            vertices[12] = new CustomVertex.PositionColoredTextured(-x, y, z, c, offsetU, offsetV);
            vertices[13] = new CustomVertex.PositionColoredTextured(x, y, -z, c, offsetU + u, offsetV + v);
            vertices[14] = new CustomVertex.PositionColoredTextured(-x, y, -z, c, offsetU, offsetV + v);
            vertices[15] = new CustomVertex.PositionColoredTextured(-x, y, z, c, offsetU, offsetV);
            vertices[16] = new CustomVertex.PositionColoredTextured(x, y, z, c, offsetU + u, offsetV);
            vertices[17] = new CustomVertex.PositionColoredTextured(x, y, -z, c, offsetU + u, offsetV + v);

            // Bottom face (remember this is facing *away* from the camera, so vertices should be clockwise order)
            vertices[18] = new CustomVertex.PositionColoredTextured(-x, -y, z, c, offsetU, offsetV);
            vertices[19] = new CustomVertex.PositionColoredTextured(-x, -y, -z, c, offsetU, offsetV + v);
            vertices[20] = new CustomVertex.PositionColoredTextured(x, -y, -z, c, offsetU + u, offsetV + v);
            vertices[21] = new CustomVertex.PositionColoredTextured(-x, -y, z, c, offsetU, offsetV);
            vertices[22] = new CustomVertex.PositionColoredTextured(x, -y, -z, c, offsetU + u, offsetV + v);
            vertices[23] = new CustomVertex.PositionColoredTextured(x, -y, z, c, offsetU + u, offsetV);

            // Left face
            vertices[24] = new CustomVertex.PositionColoredTextured(-x, y, z, c, offsetU, offsetV);
            vertices[25] = new CustomVertex.PositionColoredTextured(-x, -y, -z, c, offsetU + u, offsetV + v);
            vertices[26] = new CustomVertex.PositionColoredTextured(-x, -y, z, c, offsetU, offsetV + v);
            vertices[27] = new CustomVertex.PositionColoredTextured(-x, y, -z, c, offsetU + u, offsetV);
            vertices[28] = new CustomVertex.PositionColoredTextured(-x, -y, -z, c, offsetU + u, offsetV + v);
            vertices[29] = new CustomVertex.PositionColoredTextured(-x, y, z, c, offsetU, offsetV);

            // Right face (remember this is facing *away* from the camera, so vertices should be clockwise order)
            vertices[30] = new CustomVertex.PositionColoredTextured(x, y, z, c, offsetU, offsetV);
            vertices[31] = new CustomVertex.PositionColoredTextured(x, -y, z, c, offsetU, offsetV + v);
            vertices[32] = new CustomVertex.PositionColoredTextured(x, -y, -z, c, offsetU + u, offsetV + v);
            vertices[33] = new CustomVertex.PositionColoredTextured(x, y, -z, c, offsetU + u, offsetV);
            vertices[34] = new CustomVertex.PositionColoredTextured(x, y, z, c, offsetU, offsetV);
            vertices[35] = new CustomVertex.PositionColoredTextured(x, -y, -z, c, offsetU + u, offsetV + v);

            //Sumar posicion inicial a todos
            Vector3 center = pMin + extent;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].Position += center;
            }

            vertexBuffer.SetData(vertices, 0, LockFlags.None);
        }

        /// <summary>
        /// Renderizar voxel
        /// </summary>
        public void render()
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;
            TgcTexture.Manager texturesManager = GuiController.Instance.TexturesManager;
            texturesManager.clear(0);
            d3dDevice.Transform.World = Matrix.Identity;
            texturesManager.clear(1);
            d3dDevice.Material = TgcD3dDevice.DEFAULT_MATERIAL;
            d3dDevice.VertexFormat = CustomVertex.PositionColoredTextured.Format;

            //Cargar valores de shader de matrices
            Matrix matWorldView = d3dDevice.Transform.View;
            Matrix matWorldViewProj = matWorldView * d3dDevice.Transform.Projection;
            effect.SetValue("matWorld", Matrix.Identity);
            effect.SetValue("matWorldView", matWorldView);
            effect.SetValue("matWorldViewProj", matWorldViewProj);

            //transparencia
            effect.SetValue("alphaValue", alphaBlendingValue);
            d3dDevice.RenderState.AlphaTestEnable = true;
            d3dDevice.RenderState.AlphaBlendEnable = true;

            //Draw shader
            effect.Begin(0);
            effect.BeginPass(0);
            d3dDevice.SetStreamSource(0, vertexBuffer, 0);
            d3dDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, 12);
            effect.EndPass();
            effect.End();

            d3dDevice.RenderState.AlphaTestEnable = false;
            d3dDevice.RenderState.AlphaBlendEnable = false;
        }

        /// <summary>
        /// Liberar recursos
        /// </summary>
        public void dispose()
        {
            vertexBuffer.Dispose();
        }
        


    }
}
