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
    /// TestDllCaso4
    /// </summary>
    public class TestDllCaso4 : TgcExample
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
            return "Test DLL Caso 4";
        }

        public override string getDescription()
        {
            return "Test DLL Caso 4.";
        }

        public override void init()
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            //Occlusion
            //viewport = new OcclusionViewport(d3dDevice.Viewport.Width / 4, d3dDevice.Viewport.Height / 4);
            viewport = new OcclusionViewport(295, 185);
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
            OcclusionDll.OccluderData[] data = new OcclusionDll.OccluderData[2];



            //Triangulo [0]	{0: [295,140,0.9963], 1: [295,120,0.9945], 2: [0,128,0.9951]}
            data[0] = createTriangleOccluderData(new Vector3(295, 140, 0.9963f), new Vector3(295, 120, 0.9945f), new Vector3(0, 128, 0.9951f));

            //Triangulo [1]	{0: [294,120,0.9945], 1: [0,0,0.9963], 2: [0,128,0.9951]}
            data[1] = createTriangleOccluderData(new Vector3(294, 120, 0.9945f), new Vector3(0, 0, 0.9963f), new Vector3(0, 128, 0.9951f));

            

            occlusionDll.addOccluders(data);



            
            //DepthBuffer
            occlusionDll.fillDepthBuffer();
            GuiController.Instance.Drawer2D.beginDrawSprite();
            depthBufferSprite.render();
            GuiController.Instance.Drawer2D.endDrawSprite();
            

            //drawGDI(data);
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
                OcclusionDll.OccluderPoint last = occ.points[occ.numberOfPoints - 1];
                for (int i = 0; i < occ.numberOfPoints; i++)
                {
                    OcclusionDll.OccluderPoint p = occ.points[i];
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
