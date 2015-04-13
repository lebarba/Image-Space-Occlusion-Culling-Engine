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
using System.Drawing.Imaging;

namespace Examples.OcclusionMap
{
    /// <summary>
    /// TestDrawOccluder
    /// </summary>
    public class TestDrawOccluder : TgcExample
    {
        TgcMesh meshOccluder;
        OcclusionViewport viewport;
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
            return "Test Draw Occluder";
        }

        public override string getDescription()
        {
            return "Test Draw Occluder";
        }

        public override void init()
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            //Box de occluder
            TgcBox box = TgcBox.fromSize(new Vector3(0, 0, 0), new Vector3(20, 20, 20), Color.Violet);
            meshOccluder = box.toMesh("occluder");


            //Camara        
            GuiController.Instance.FpsCamera.Enable = true;
            GuiController.Instance.FpsCamera.setCamera(new Vector3(0, 0, 40), new Vector3(0, 0, 0));


            //Occlusion
            viewport = new OcclusionViewport(256, 256);


            //GDI
            bitmap = new Bitmap(viewport.D3dViewport.Width, viewport.D3dViewport.Height, PixelFormat.Format32bppArgb);
            gBmp = Graphics.FromImage(bitmap);
            gBmp.FillRectangle(new SolidBrush(Color.Red), 0, 0, viewport.D3dViewport.Width, viewport.D3dViewport.Height);
            pen = new Pen(new SolidBrush(Color.White), 4f);

            //Draw texture
            dxSprite = new Microsoft.DirectX.Direct3D.Sprite(d3dDevice);
            drawTexture = Texture.FromBitmap(d3dDevice, bitmap, Usage.None, Pool.Managed);



            GuiController.Instance.UserVars.addVar("Occludee-Visible");
            GuiController.Instance.UserVars.addVar("cant_visibles");
            GuiController.Instance.UserVars.addVar("outputCount");
        }

        


        public override void render(float elapsedTime)
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            //Calcular los 8 vertices del AABB
            Vector3 min = meshOccluder.BoundingBox.PMin;
            Vector3 max = meshOccluder.BoundingBox.PMax;
            Vector3[] boxVerts = new Vector3[8];
            boxVerts[0] = new Vector3(min.X, max.Y, min.Z);
            boxVerts[1] = new Vector3(max.X, max.Y, min.Z);
            boxVerts[2] = new Vector3(min.X, max.Y, max.Z);
            boxVerts[3] = new Vector3(max.X, max.Y, max.Z);
            boxVerts[4] = new Vector3(min.X, min.Y, min.Z);
            boxVerts[5] = new Vector3(max.X, min.Y, min.Z);
            boxVerts[6] = new Vector3(min.X, min.Y, max.Z);
            boxVerts[7] = new Vector3(max.X, min.Y, max.Z);

            //Front face - ClockWise
            Occluder.OccluderQuad quad = new Occluder.OccluderQuad(boxVerts[2], boxVerts[3], boxVerts[7], boxVerts[6]);
            Vector3[] points = viewport.projectQuad(quad.Points);

            //Render meshes
            meshOccluder.render();
            meshOccluder.BoundingBox.render();



            //GDI
            gBmp.Clear(Color.Black);
            Vector3 last = points[points.Length - 1];
            const float SCALE = 20;
            foreach (Vector3 p in points)
            {
                gBmp.DrawLine(pen, 
                    new Point(
                        (int)(last.X / SCALE) + (int)(viewport.D3dViewport.Width / 2),
                        (int)(last.Y / SCALE) + (int)(viewport.D3dViewport.Height / 2)
                        ),
                    new Point(
                        (int)(p.X / SCALE) + (int)(viewport.D3dViewport.Width / 2),
                        (int)(p.Y / SCALE) + (int)(viewport.D3dViewport.Height / 2)
                        ));
                last = p;
            }


            drawTexture.Dispose();
            drawTexture = Texture.FromBitmap(d3dDevice, bitmap, Usage.None, Pool.Managed);


            dxSprite.Begin(SpriteFlags.AlphaBlend | SpriteFlags.SortDepthFrontToBack);
            dxSprite.Draw(drawTexture, new Rectangle(0, 0, viewport.D3dViewport.Width, viewport.D3dViewport.Height),
                Vector3.Empty, new Vector3(
                    0,
                    GuiController.Instance.Panel3d.Height - viewport.D3dViewport.Height, 0), Color.White);
            dxSprite.End();

        }

        public override void close()
        {
            
        }

    }
}
