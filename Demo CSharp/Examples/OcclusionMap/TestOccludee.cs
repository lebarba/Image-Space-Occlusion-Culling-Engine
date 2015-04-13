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
    /// TestOccludee
    /// </summary>
    public class TestOccludee : TgcExample
    {
        TgcMesh meshOccluder;
        TgcMesh meshOccludee;
        Occluder occluder;
        OcclusionDll occlusionDll;
        List<Occluder> enabledOccluders;
        TgcSprite depthBufferSprite;
        OcclusionViewport viewport;
        List<Occludee.BoundingBox2D> occludees;
 
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
            return "Test 1 Occludee";
        }

        public override string getDescription()
        {
            return "Test 1 Occludee";
        }

        public override void init()
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            //Box de occluder
            //TgcBox box = TgcBox.fromSize(new Vector3(0, 0, 0), new Vector3(20, 20, 20), Color.Green);
            TgcBox box = TgcBox.fromSize(new Vector3(0, 0, 0), new Vector3(100, 20, 5), Color.Green);
            meshOccluder = box.toMesh("occluder");

            //Occluder
            occluder = new Occluder();
            occluder.Mesh = meshOccluder;

            //Occludee
            meshOccludee = TgcBox.fromSize(new Vector3(0, 0, -50), new Vector3(10, 40, 10), Color.Red).toMesh("occludee");
            //meshOccludee = TgcBox.fromSize(new Vector3(0, -10, 0), new Vector3(400, 0, 400), Color.Red).toMesh("occludee");
            occludees = new List<Occludee.BoundingBox2D>();


            //Camara        
            GuiController.Instance.FpsCamera.Enable = true;
            //GuiController.Instance.FpsCamera.setCamera(new Vector3(0, 0, 40), new Vector3(0, 0, 0));
            //GuiController.Instance.FpsCamera.setCamera(new Vector3(41.9359f, -1.8248f, -20.4352f), new Vector3(41.2293f, -1.9502f, -19.7388f));
            GuiController.Instance.FpsCamera.setCamera(new Vector3(41.9359f, -1.8248f, -20.4352f), new Vector3(41.2348f, -1.9384f, -19.7313f));

            //Modifiers
            GuiController.Instance.Modifiers.addBoolean("showDepthBuffer", "showDepthBuffer", true);
            GuiController.Instance.Modifiers.addBoolean("showHidden", "showHidden", true);
            GuiController.Instance.Modifiers.addBoolean("drawQuads", "drawQuads", true);


            //Occlusion
            //viewport = new OcclusionViewport(256, 256);
            viewport = new OcclusionViewport(d3dDevice.Viewport.Width / 4, d3dDevice.Viewport.Height / 4);
            //viewport = new OcclusionViewport(d3dDevice.Viewport.Width, d3dDevice.Viewport.Height);
            occlusionDll = new OcclusionDll(viewport.D3dViewport.Width, viewport.D3dViewport.Height);
            occlusionDll.clear();
            occlusionDll.fillDepthBuffer();
            enabledOccluders = new List<Occluder>();
            

            //DepthBuffer
            depthBufferSprite = new TgcSprite();
            depthBufferSprite.Position = new Vector2(0, viewport.D3dViewport.Height);
            //depthBufferSprite.Position = new Vector2(0, 0);
            depthBufferSprite.Texture = occlusionDll.DepthBufferTexture;
            //depthBufferSprite.Scaling = new Vector2(0.25f, 0.25f);


            GuiController.Instance.UserVars.addVar("Occludee-Visible");
            GuiController.Instance.UserVars.addVar("OccluderFaces");



            //GDI
            bitmap = new Bitmap(viewport.D3dViewport.Width, viewport.D3dViewport.Height, PixelFormat.Format32bppArgb);
            gBmp = Graphics.FromImage(bitmap);
            gBmp.FillRectangle(new SolidBrush(Color.Red), 0, 0, viewport.D3dViewport.Width, viewport.D3dViewport.Height);
            pen = new Pen(new SolidBrush(Color.Green), 4f);
            brushVertex = new SolidBrush(Color.Yellow);
            //Draw texture
            dxSprite = new Microsoft.DirectX.Direct3D.Sprite(d3dDevice);
            drawTexture = Texture.FromBitmap(d3dDevice, bitmap, Usage.None, Pool.Managed);
        }


        public override void render(float elapsedTime)
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;       

            //Project occluder
            if (TgcCollisionUtils.classifyFrustumAABB(GuiController.Instance.Frustum, occluder.Mesh.BoundingBox) != TgcCollisionUtils.FrustumResult.OUTSIDE)
            {
                occluder.computeProjectedQuads(GuiController.Instance.FpsCamera.getPosition(), viewport);
                if (occluder.ProjectedQuads.Count > 0)
                {
                    enabledOccluders.Clear();
                    enabledOccluders.Add(occluder);

                   /*
                    //DEBUG: dejar solo la primera
                    if (occluder.ProjectedQuads.Count > 1)
                    {
                        Occluder.OccluderQuad q = occluder.ProjectedQuads[0];
                        occluder.ProjectedQuads.Clear();
                        occluder.ProjectedQuads.Add(q);
                    }
                    */


                    //Ejecutar Occlusion
                    occlusionDll.clear();
                    occlusionDll.convertAndAddOccluders(enabledOccluders, occluder.ProjectedQuads.Count);
                }
            }
            

            

            //FrustumCulling de occludee
            occludees.Clear();
            if (TgcCollisionUtils.classifyFrustumAABB(GuiController.Instance.Frustum, meshOccludee.BoundingBox) == TgcCollisionUtils.FrustumResult.OUTSIDE)
            {
                meshOccludee.Enabled = false;
            }
            //Occlusion de occludee
            else
            {
                Occludee.BoundingBox2D meshBox2D;
                if (Occludee.projectBoundingBox(meshOccludee.BoundingBox, viewport, out meshBox2D))
                {
                    meshOccludee.Enabled = occlusionDll.convertAndTestOccludee(meshBox2D);
                    meshBox2D.visible = meshOccludee.Enabled;
                    occludees.Add(meshBox2D);
                }
            }


            //Render meshes
            meshOccludee.render();
            meshOccluder.render();
            meshOccluder.BoundingBox.render();


            //Dibujar occludee oculto
            bool showHidden = (bool)GuiController.Instance.Modifiers["showHidden"];
            if (showHidden)
            {
                if (!meshOccludee.Enabled)
                {
                    d3dDevice.RenderState.ZBufferEnable = false;
                    meshOccludee.BoundingBox.render();
                    d3dDevice.RenderState.ZBufferEnable = true;
                }
            }






            //Dibujar depthBuffer
            bool showDepthBuffer = (bool)GuiController.Instance.Modifiers["showDepthBuffer"];
            if (showDepthBuffer)
            {
                //occlusionDll.fillDepthBuffer(occludees);
                occlusionDll.fillDepthBuffer();
                GuiController.Instance.Drawer2D.beginDrawSprite();
                depthBufferSprite.render();
                GuiController.Instance.Drawer2D.endDrawSprite();
            }

            //Dibujar quads en 2D
            bool drawQuads = (bool)GuiController.Instance.Modifiers["drawQuads"];
            if (drawQuads)
            {
                drawOccluderFaces(enabledOccluders);
            }




            GuiController.Instance.UserVars["Occludee-Visible"] = meshOccludee.Enabled.ToString();
            GuiController.Instance.UserVars["OccluderFaces"] = occluder.ProjectedQuads.Count.ToString();
        }

        /// <summary>
        /// Dibujar quads en 2D
        /// </summary>
        private void drawOccluderFaces(List<Occluder> occludersToDraw)
        {
            //GDI
            gBmp.Clear(Color.Black);

            //Lineas de occluders
            for (int i = 0; i < occludersToDraw.Count; i++)
			{
                for (int j = 0; j < occludersToDraw[i].ProjectedQuads.Count; j++)
                {
                    Occluder.OccluderQuad q = occludersToDraw[i].ProjectedQuads[j];
                    Vector3 last = q.Points[q.Points.Length - 1];
                    foreach (Vector3 p in q.Points)
                    {
                        gBmp.DrawLine(pen,
                            new Point((int)last.X,(int)last.Y),
                            new Point((int)p.X,(int)p.Y)
                            );
                        gBmp.FillEllipse(brushVertex, (int)(p.X - 5), (int)(p.Y - 5), 10, 10);
                        last = p;
                    }
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


        public override void close()
        {
            occluder.dispose();
        }

    }
}
