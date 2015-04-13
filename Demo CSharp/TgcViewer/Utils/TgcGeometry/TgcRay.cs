using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX;
using TgcViewer.Utils.TgcSceneLoader;

namespace TgcViewer.Utils.TgcGeometry
{
    /// <summary>
    /// Representa un Ray 3D con un origen y una direccion, de la forma: r = p + td
    /// </summary>
    public class TgcRay
    {

        public TgcRay()
        {
        }

        /// <summary>
        /// Se normaliza la direccion
        /// </summary>
        public TgcRay(Vector3 origin, Vector3 direction)
        {
            this.origin = origin;
            this.direction = direction;
            this.direction.Normalize();
        }

        Vector3 origin;
        /// <summary>
        /// Punto de origen del Ray
        /// </summary>
        public Vector3 Origin
        {
            get { return origin; }
            set { origin = value; }
        }

        Vector3 direction;
        /// <summary>
        /// Direccion del Ray
        /// </summary>
        public Vector3 Direction
        {
            get { return direction; }
            set { 
                direction = value;
                direction.Normalize();
            }
        }

        public override string ToString()
        {
            return "Origin[" + TgcParserUtils.printFloat(origin.X) + ", " + TgcParserUtils.printFloat(origin.Y) + ", " + TgcParserUtils.printFloat(origin.Z) + "]" +
                " Direction[" + TgcParserUtils.printFloat(direction.X) + ", " + TgcParserUtils.printFloat(direction.Y) + ", " + TgcParserUtils.printFloat(direction.Z) + "]";
        }

    }
}
