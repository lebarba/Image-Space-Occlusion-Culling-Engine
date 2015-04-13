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
    /// TestDllDrawOccluder
    /// </summary>
    public class TestDllDrawOccluder : TgcExample
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


        public override string getCategory()
        {
            return "OcclusionMap";
        }

        public override string getName()
        {
            return "Test DLL Draw Occluder";
        }

        public override string getDescription()
        {
            return "Test DLL Draw Occluder.";
        }

        public override void init()
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            //Occlusion
            viewport = new OcclusionViewport(d3dDevice.Viewport.Width / 4, d3dDevice.Viewport.Height / 4);
            occlusionDll = new OcclusionDll(viewport.D3dViewport.Width, viewport.D3dViewport.Height);
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
            //Draw texture
            dxSprite = new Microsoft.DirectX.Direct3D.Sprite(d3dDevice);
            drawTexture = Texture.FromBitmap(d3dDevice, bitmap, Usage.None, Pool.Managed);
        }


        public override void render(float elapsedTime)
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;


            occlusionDll.clear();


            OcclusionDll.OccluderPoint[] points = new OcclusionDll.OccluderPoint[4];
            int i = -1;

            points[++i] = new OcclusionDll.OccluderPoint();
            points[i].x = 0;
            points[i].y = 26;
            points[i].depth = 0.986057162f;

            points[++i] = new OcclusionDll.OccluderPoint();
            points[i].x = 0;
            points[i].y = 85;
            points[i].depth = 0.9873379f;

            points[++i] = new OcclusionDll.OccluderPoint();
            points[i].x = 0;
            points[i].y = 87;
            points[i].depth = 0.9873563f;

            points[++i] = new OcclusionDll.OccluderPoint();
            points[i].x = 295;
            points[i].y = 180;
            points[i].depth = 0.9369683f;


            OcclusionDll.OccluderData[] data = new OcclusionDll.OccluderData[1];
            data[0] = new OcclusionDll.OccluderData();
            data[0].numberOfPoints = points.Length;
            data[0].points = points;
            occlusionDll.addOccluders(data);


            //DepthBuffer
            occlusionDll.fillDepthBuffer();
            GuiController.Instance.Drawer2D.beginDrawSprite();
            depthBufferSprite.render();
            GuiController.Instance.Drawer2D.endDrawSprite();





            //GDI
            gBmp.Clear(Color.Black);

            //Lineas de occluders
            OcclusionDll.OccluderPoint last = points[points.Length - 1];
            foreach (OcclusionDll.OccluderPoint p in points)
            {
                gBmp.DrawLine(pen,
                    new Point(last.x, last.y),
                    new Point(p.x, p.y)
                    );
                last = p;
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

        public override void close()
        {

        }

    }
}
