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
    /// TestOcclusionAABB
    /// </summary>
    public class TestOcclusionAABB : TgcExample
    {
        TgcMesh mesh;
        Occluder occluder;
        OcclusionDll occlusionDll;
        List<Occluder> occluders;
        TgcSprite depthBufferSprite;
        OcclusionViewport viewport;
 

        public override string getCategory()
        {
            return "OcclusionMap";
        }

        public override string getName()
        {
            return "Test OcclusionAABB";
        }

        public override string getDescription()
        {
            return "Test OcclusionAABB";
        }

        public override void init()
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            //Box
            TgcBox box = TgcBox.fromSize(new Vector3(0, 0, 0), new Vector3(2, 2, 2), Color.Red);
            mesh = box.toMesh("box");

            //Occluder
            occluder = new Occluder();
            occluder.Mesh = mesh;


            //Camara        
            GuiController.Instance.RotCamera.Enable = true;
            GuiController.Instance.RotCamera.CameraDistance = 20f;


            //Modifiers
            GuiController.Instance.Modifiers.addBoolean("showOccluder", "showOccluder", false);
            GuiController.Instance.Modifiers.addBoolean("showMesh", "showMesh", true);


            //Occlusion
            //occlusionDll = new OcclusionDll(d3dDevice.Viewport.Width, d3dDevice.Viewport.Height);
            viewport = new OcclusionViewport(256, 256);
            occlusionDll = new OcclusionDll(viewport.D3dViewport.Width, viewport.D3dViewport.Height);
            occlusionDll.clear();
            occlusionDll.fillDepthBuffer();
            occluders = new List<Occluder>();

            //DepthBuffer
            depthBufferSprite = new TgcSprite();
            depthBufferSprite.Position = new Vector2(0, viewport.D3dViewport.Height);
            depthBufferSprite.Texture = occlusionDll.DepthBufferTexture;
            //depthBufferSprite.Scaling = new Vector2(0.25f, 0.25f);
        }


        public override void render(float elapsedTime)
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;       

            //Project occluder
            occluder.computeProjectedQuads(GuiController.Instance.RotCamera.getPosition(), viewport);

            //Render mesh
            if ((bool)GuiController.Instance.Modifiers["showMesh"])
            {
                mesh.render();
                mesh.BoundingBox.render();
            }


            //Render occluder
            if ((bool)GuiController.Instance.Modifiers["showOccluder"])
            {
                int color = Color.Green.ToArgb();
                CustomVertex.TransformedColored[] verts = new CustomVertex.TransformedColored[occluder.ProjectedQuads.Count * 4 * 2];
                int vCount = 0;
                foreach (Occluder.OccluderQuad quad in occluder.ProjectedQuads)
                {
                    verts[vCount++] = new CustomVertex.TransformedColored(quad.Points[0].X, quad.Points[0].Y, 1, 1, color);
                    verts[vCount++] = new CustomVertex.TransformedColored(quad.Points[1].X, quad.Points[1].Y, 1, 1, color);

                    verts[vCount++] = new CustomVertex.TransformedColored(quad.Points[1].X, quad.Points[1].Y, 1, 1, color);
                    verts[vCount++] = new CustomVertex.TransformedColored(quad.Points[2].X, quad.Points[2].Y, 1, 1, color);

                    verts[vCount++] = new CustomVertex.TransformedColored(quad.Points[2].X, quad.Points[2].Y, 1, 1, color);
                    verts[vCount++] = new CustomVertex.TransformedColored(quad.Points[3].X, quad.Points[3].Y, 1, 1, color);

                    verts[vCount++] = new CustomVertex.TransformedColored(quad.Points[3].X, quad.Points[3].Y, 1, 1, color);
                    verts[vCount++] = new CustomVertex.TransformedColored(quad.Points[0].X, quad.Points[0].Y, 1, 1, color);
                }

                d3dDevice.Transform.World = Matrix.Identity;
                d3dDevice.VertexFormat = CustomVertex.TransformedColored.Format;
                d3dDevice.DrawUserPrimitives(PrimitiveType.LineList, occluder.ProjectedQuads.Count * 4, verts);
            }



            //Occlusion
            occlusionDll.clear();
            occluders.Clear();
            occluders.Add(occluder);
            occlusionDll.convertAndAddOccluders(occluders, occluder.ProjectedQuads.Count);



            //Dibujar depthBuffer
            occlusionDll.fillDepthBuffer();
            GuiController.Instance.Drawer2D.beginDrawSprite();
            depthBufferSprite.render();
            GuiController.Instance.Drawer2D.endDrawSprite();

        }

        public override void close()
        {
            occluder.dispose();
        }

    }
}
