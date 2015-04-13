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

namespace Examples
{
    /// <summary>
    /// TestRenderObb
    /// </summary>
    public class TestRenderObb : TgcExample
    {

        TgcObb obb;

        public override string getCategory()
        {
            return "OBB";
        }

        public override string getName()
        {
            return "Render OBB";
        }

        public override string getDescription()
        {
            return "Render OBB";
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
            
        }

        public override void close()
        {
            obb.dispose();
        }

    }
}
