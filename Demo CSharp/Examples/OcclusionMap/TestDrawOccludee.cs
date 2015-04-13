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
    /// TestDrawOccludee
    /// </summary>
    public class TestDrawOccludee : TgcExample
    {
        TgcMesh meshOccludee;
        OcclusionViewport viewport;
        Graphics gBmp;
        Bitmap bitmap;
        Sprite dxSprite;
        Texture drawTexture;
        Pen pen;
        SolidBrush brush;
 

        public override string getCategory()
        {
            return "OcclusionMap";
        }

        public override string getName()
        {
            return "Test Draw Occludee";
        }

        public override string getDescription()
        {
            return "Test Draw Occludee";
        }

        public override void init()
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            //Box de occluder
            TgcBox box = TgcBox.fromSize(new Vector3(0, -40, 0), new Vector3(100, 0, 100), Color.Violet);
            meshOccludee = box.toMesh("occludee");


            //Camara        
            GuiController.Instance.FpsCamera.Enable = true;
            GuiController.Instance.FpsCamera.setCamera(new Vector3(0, 0, 40), new Vector3(0, 0, 0));


            //Occlusion
            //viewport = new OcclusionViewport(256, 256);
            viewport = new OcclusionViewport(d3dDevice.Viewport.Width / 4, d3dDevice.Viewport.Height / 4);

            //GDI
            bitmap = new Bitmap(viewport.D3dViewport.Width, viewport.D3dViewport.Height, PixelFormat.Format32bppArgb);
            gBmp = Graphics.FromImage(bitmap);
            gBmp.FillRectangle(new SolidBrush(Color.Red), 0, 0, viewport.D3dViewport.Width, viewport.D3dViewport.Height);
            pen = new Pen(new SolidBrush(Color.White), 4f);
            brush = new SolidBrush(Color.Gray);

            //Draw texture
            dxSprite = new Microsoft.DirectX.Direct3D.Sprite(d3dDevice);
            drawTexture = Texture.FromBitmap(d3dDevice, bitmap, Usage.None, Pool.Managed);

        }

        


        public override void render(float elapsedTime)
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            meshOccludee.render();


            
            gBmp.Clear(Color.Black);


            Occludee.BoundingBox2D meshBox2D;
            if (projectBoundingBox(meshOccludee.BoundingBox, viewport, out meshBox2D))
            {
                //gBmp.DrawRectangle(pen, meshBox2D.min.X, meshBox2D.min.Y, meshBox2D.max.X - meshBox2D.min.X, meshBox2D.max.Y - meshBox2D.min.Y);
                gBmp.FillRectangle(brush, meshBox2D.min.X, meshBox2D.min.Y, meshBox2D.max.X - meshBox2D.min.X, meshBox2D.max.Y - meshBox2D.min.Y);
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

        /// <summary>
        /// Proyectar AABB a 2D
        /// </summary>
        public static bool projectBoundingBox(TgcBoundingBox box3d, OcclusionViewport viewport, out Occludee.BoundingBox2D box2D)
        {
            box2D = new Occludee.BoundingBox2D();

            //Proyectar los 8 corners del BoundingBox
            Vector3[] projVertices = box3d.computeCorners();
            for (int i = 0; i < projVertices.Length; i++)
            {
                projVertices[i] = viewport.projectPointAndClip(projVertices[i]);
            }

            //Buscar los puntos extremos
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            float minDepth = float.MaxValue;
            foreach (Vector3 v in projVertices)
            {
                if (v.X < min.X)
                {
                    min.X = v.X;
                }
                if (v.Y < min.Y)
                {
                    min.Y = v.Y;
                }
                if (v.X > max.X)
                {
                    max.X = v.X;
                }
                if (v.Y > max.Y)
                {
                    max.Y = v.Y;
                }

                if (v.Z < minDepth)
                {
                    minDepth = v.Z;
                }
            }


            //Descartar rect2D que sale fuera de pantalla
            int w = viewport.D3dViewport.Width;
            int h = viewport.D3dViewport.Height;
            if (min.X >= w || max.X < 0 || min.Y >= h || max.Y < 0)
            {
                return false;
            }


            //Clamp
            if (min.X < 0f) min.X = 0f;
            if (min.Y < 0f) min.Y = 0f;
            if (max.X >= w) max.X = w - 1;
            if (max.Y >= h) max.Y = h - 1;

            //Cargar valores de box2D
            box2D.min = min;
            box2D.max = max;
            box2D.depth = minDepth;
            return true;
        }

        public override void close()
        {
            meshOccludee.dispose();
        }

    }
}
