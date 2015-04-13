using System;
using System.Collections.Generic;
using System.Text;
using TgcViewer.Example;
using TgcViewer;
using Microsoft.DirectX.Direct3D;
using System.Drawing;
using Microsoft.DirectX;
using TgcViewer.Utils.Modifiers;
using TgcViewer.Utils.TgcSceneLoader;
using TgcViewer.Utils.TgcGeometry;
using TgcViewer.Utils._2D;
using Examples.OcclusionMap.DLL;

namespace Examples.OcclusionMap
{
    /// <summary>
    /// TestWneg
    /// </summary>
    public class TestWneg : TgcExample
    {

        OcclusionViewport viewport;
        CustomVertex.TransformedColored[] vertices;
        Vector3[] v;
 

        public override string getCategory()
        {
            return "OcclusionMap";
        }

        public override string getName()
        {
            return "Test W negativo";
        }

        public override string getDescription()
        {
            return "Test W negativo";
        }

        public override void init()
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            viewport = new OcclusionViewport(d3dDevice.Viewport.Width, d3dDevice.Viewport.Height);

            v = new Vector3[4];
            v[0] = new Vector3(0, 0, 5);
            v[1] = new Vector3(0, 100, 5);
            v[2] = new Vector3(100, 100, 5);
            v[3] = new Vector3(100, 0, 5);


            vertices = new CustomVertex.TransformedColored[4];


            GuiController.Instance.FpsCamera.Enable = true;
        }


        public override void render(float elapsedTime)
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            for (int i = 0; i < v.Length; i++)
            {
                Vector3 vProj = viewport.projectPoint(v[i]);
                vertices[i] = new CustomVertex.TransformedColored(vProj.X, vProj.Y, vProj.Z, 1, Color.Red.ToArgb());
            }

            d3dDevice.VertexFormat = CustomVertex.TransformedColored.Format;
            d3dDevice.Transform.World = Matrix.Identity;
            d3dDevice.DrawUserPrimitives(PrimitiveType.LineList, 1, vertices);
        }

        public override void close()
        {
            
        }

    }
}
