using System;
using System.Collections.Generic;
using System.Text;
using TgcViewer.Example;
using TgcViewer;
using Microsoft.DirectX.Direct3D;
using System.Drawing;
using Microsoft.DirectX;
using TgcViewer.Utils.Modifiers;
using TgcViewer.Utils._2D;
using System.Drawing.Imaging;

namespace Examples.OcclusionMap.DLL
{
    /// <summary>
    /// TestDllCaso3
    /// </summary>
    public class TestDllCaso3 : TgcExample
    {

        OcclusionDll occlusionDll;
        TgcSprite depthBufferSprite;
        OcclusionViewport viewport;

        //GDI
        Graphics gBmp;
        Bitmap bitmap;
        Sprite dxSprite;
        Texture drawTexture;
        Pen pen;
        Brush brushVertex;


        public override string getCategory()
        {
            return "OcclusionMap";
        }

        public override string getName()
        {
            return "Test DLL Caso 3";
        }

        public override string getDescription()
        {
            return "Test DLL Caso 3.";
        }

        public override void init()
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            //Occlusion
            viewport = new OcclusionViewport(d3dDevice.Viewport.Width / 4, d3dDevice.Viewport.Height / 4);
            occlusionDll = new OcclusionDll(viewport.D3dViewport.Width, viewport.D3dViewport.Height, true);
            occlusionDll.clear();
            occlusionDll.fillDepthBuffer();

            //DepthBuffer
            depthBufferSprite = new TgcSprite();
            depthBufferSprite.Position = new Vector2(0, viewport.D3dViewport.Height);
            depthBufferSprite.Texture = occlusionDll.DepthBufferTexture;


            //GDI
            bitmap = new Bitmap(viewport.D3dViewport.Width, viewport.D3dViewport.Height, PixelFormat.Format32bppArgb);
            gBmp = Graphics.FromImage(bitmap);
            gBmp.FillRectangle(new SolidBrush(Color.Red), 0, 0, viewport.D3dViewport.Width, viewport.D3dViewport.Height);
            pen = new Pen(new SolidBrush(Color.Green), 4f);
            brushVertex = new SolidBrush(Color.Yellow);
            //Draw texture
            dxSprite = new Microsoft.DirectX.Direct3D.Sprite(d3dDevice);
            drawTexture = Texture.FromBitmap(d3dDevice, bitmap, Usage.None, Pool.Managed);


            GuiController.Instance.UserVars.addVar("result", "");
        }


        public override void render(float elapsedTime)
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;


            occlusionDll.clear();


            //Occluders
            OcclusionDll.OccluderData[] data = new OcclusionDll.OccluderData[3];

            



            //Triangulo 0 = {0: [17,0,0.9905], 1: [0,6,0.9889], 2: [0,128,0.9406]}
            data[0] = createTriangleOccluderData(new Vector3(17,0,0.9905f), new Vector3(0,6,0.9889f), new Vector3(0,128,0.9406f));

            //Triangulo 1 = {0: [17,0,0.9905], 1: [0,128,0.9406], 2: [240,127,0.9255]}
            data[1] = createTriangleOccluderData(new Vector3(17,0,0.9905f), new Vector3(0,128,0.9406f), new Vector3(240,127,0.9255f));

            //Triangulo 2 = {0: [17,0,0.9905], 1: [240,127,0.9255], 2: [240,0,0.9764]}
            data[2] = createTriangleOccluderData(new Vector3(17, 0, 0.9905f), new Vector3(240, 127, 0.9255f), new Vector3(240, 0, 0.9764f));

            occlusionDll.addOccluders(data);



            //Occludee
            /*
            min = (0, 0)
            max = (239, 127)
            depth = 0.9814454
            */
            bool result = occlusionDll.testOccludeeVisibility(createOccludeeData(new Vector2(0, 0), new Vector2(239, 127), 0.9814454f));
            GuiController.Instance.UserVars["result"] = result.ToString();


            //DepthBuffer
            occlusionDll.fillDepthBuffer();
            GuiController.Instance.Drawer2D.beginDrawSprite();
            depthBufferSprite.render();
            GuiController.Instance.Drawer2D.endDrawSprite();


            drawGDI(data);
        }

        private OcclusionDll.OccludeeData createOccludeeData(Vector2 min, Vector2 max, float depth)
        {
            OcclusionDll.OccludeeData data = new OcclusionDll.OccludeeData();
            data.boundingBox = new OcclusionDll.OccludeeAABB();
            data.boundingBox.xMin = (int)min.X;
            data.boundingBox.yMin = (int)min.Y;
            data.boundingBox.xMax = (int)max.X;
            data.boundingBox.yMax = (int)max.Y;
            data.depth = depth;
            return data;
        }

        private void drawGDI(OcclusionDll.OccluderData[] data)
        {
            //GDI
            gBmp.Clear(Color.Black);

            //Lineas de occluders
            foreach (OcclusionDll.OccluderData occ in data)
            {
                OcclusionDll.OccluderPoint last = occ.points[occ.points.Length - 1];
                foreach (OcclusionDll.OccluderPoint p in occ.points)
                {
                    gBmp.DrawLine(pen,
                        new Point(last.x, last.y),
                        new Point(p.x, p.y)
                        );
                    gBmp.FillEllipse(brushVertex, (int)(p.x - 5), (int)(p.y - 5), 10, 10);
                    last = p;
                }
            }
            

            //Textura
            drawTexture.Dispose();
            drawTexture = Texture.FromBitmap(GuiController.Instance.D3dDevice, bitmap, Usage.None, Pool.Managed);

            //DrawSprite
            dxSprite.Begin(SpriteFlags.AlphaBlend | SpriteFlags.SortDepthFrontToBack);
            dxSprite.Draw(drawTexture, new Rectangle(0, 0, viewport.D3dViewport.Width, viewport.D3dViewport.Height),
                Vector3.Empty, new Vector3(
                    0,
                    GuiController.Instance.Panel3d.Height - (viewport.D3dViewport.Height * 1.5f), 0), Color.White);
            dxSprite.End();
        }

        private OcclusionDll.OccluderData createTriangleOccluderData(Vector3 a, Vector3 b, Vector3 c)
        {
            OcclusionDll.OccluderPoint[] points = new OcclusionDll.OccluderPoint[4];

            points[0] = new OcclusionDll.OccluderPoint();
            points[0].x = (int)a.X;
            points[0].y = (int)a.Y;
            points[0].depth = a.Z;

            points[1] = new OcclusionDll.OccluderPoint();
            points[1].x = (int)b.X;
            points[1].y = (int)b.Y;
            points[1].depth = b.Z;

            points[2] = new OcclusionDll.OccluderPoint();
            points[2].x = (int)c.X;
            points[2].y = (int)c.Y;
            points[2].depth = c.Z;

            OcclusionDll.OccluderData data = new OcclusionDll.OccluderData();
            data.numberOfPoints = 3;
            data.points = points;
            return data;
        }


        public override void close()
        {

        }

    }
}
