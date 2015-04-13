using System;
using System.Collections.Generic;
using System.Text;
using TgcViewer.Example;
using TgcViewer;
using Microsoft.DirectX.Direct3D;
using System.Drawing;
using Microsoft.DirectX;
using TgcViewer.Utils.Modifiers;
using Examples.Obb;
using TgcViewer.Utils.TgcGeometry;
using TgcViewer.Utils.TgcSceneLoader;

namespace Examples
{
    /// <summary>
    /// TestRayObb
    /// </summary>
    public class TestRayObb : TgcExample
    {

        TgcObb obb;
        TgcPickingRay picking;
        TgcBox collisionBox;


        public override string getCategory()
        {
            return "OBB";
        }

        public override string getName()
        {
            return "Ray-OBB";
        }

        public override string getDescription()
        {
            return "Ray-OBB";
        }

        public override void init()
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            obb = new TgcObb();
            obb.Center = new Vector3(0, 0, 0);
            obb.Extents = new Vector3(10, 10, 10);
            obb.Orientation[0] = new Vector3(1, 0, 0);
            obb.Orientation[1] = new Vector3(0, 1, 0);
            obb.Orientation[2] = new Vector3(0, 0, 1);


            GuiController.Instance.Modifiers.addVertex3f("extents", new Vector3(0, 0, 0), new Vector3(20, 20, 20), new Vector3(10, 10, 10));
            GuiController.Instance.Modifiers.addVertex3f("orientation", new Vector3(0, 0, 0), new Vector3(360, 360, 360), new Vector3(0, 0, 0));
            GuiController.Instance.Modifiers.addVertex3f("center", new Vector3(-10, -10, -10), new Vector3(10, 10, 10), new Vector3(0, 0, 0));

            picking = new TgcPickingRay();
            collisionBox = TgcBox.fromSize(new Vector3(1, 1, 1), Color.Red);
            collisionBox.Enabled = false;


        }


        public override void render(float elapsedTime)
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;


            obb.Extents = (Vector3)GuiController.Instance.Modifiers["extents"];
            obb.Center = (Vector3)GuiController.Instance.Modifiers["center"];

            Vector3 orientation = (Vector3)GuiController.Instance.Modifiers["orientation"];
            orientation.X = FastMath.ToRad(orientation.X);
            orientation.Y = FastMath.ToRad(orientation.Y);
            orientation.Z = FastMath.ToRad(orientation.Z);
            Matrix rotM = Matrix.RotationYawPitchRoll(orientation.Y, orientation.X, orientation.Z);
            obb.Orientation[0] = new Vector3(rotM.M11, rotM.M12, rotM.M13);
            obb.Orientation[1] = new Vector3(rotM.M21, rotM.M22, rotM.M23);
            obb.Orientation[2] = new Vector3(rotM.M31, rotM.M32, rotM.M33);


            obb.updateValues();


            obb.render();


            if (GuiController.Instance.D3dInput.buttonPressed(TgcViewer.Utils.Input.TgcD3dInput.MouseButtons.BUTTON_RIGHT))
            {
                picking.updateRay();
                Vector3 q;

                if (intersectRayObb(picking.Ray, obb, out q))
                {
                    collisionBox.Position = q;
                    collisionBox.Enabled = true;
                }
                else
                {
                    collisionBox.Enabled = false;
                }
            }


            if (collisionBox.Enabled)
            {
                collisionBox.render();
            }

        }

        /// <summary>
        /// Interseccion Ray-OBB.
        /// Devuelve true y el punto q de colision si hay interseccion.
        /// </summary>
        public static bool intersectRayObb(TgcRay ray, TgcObb obb, out Vector3 q)
        {
            //Transformar Ray a OBB-space
            Vector3 a = ray.Origin;
            Vector3 b = ray.Origin + ray.Direction;
            a = obb.toObbSpace(a);
            b = obb.toObbSpace(b);
            TgcRay ray2 = new TgcRay(a, b - a);

            Vector3 min = -obb.Extents;
            Vector3 max = obb.Extents;
            TgcBoundingBox aabb = new TgcBoundingBox(min, max);

            if (TgcCollisionUtils.intersectRayAABB(ray2, aabb, out q))
            {
                //Pasar q a World-Space
                q = obb.toWorldSpace(q);
                return true;
            }

            return false;
        }



        public override void close()
        {
            obb.dispose();
            collisionBox.dispose();
        }

    }
}
