using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX.Direct3D;

namespace TgcViewer.Utils.Shaders
{
    /// <summary>
    /// Utilidades generales de shaders
    /// </summary>
    public class ShaderUtils
    {
        private ShaderUtils()
        {
        }

        /// <summary>
        /// Cargar archivo .fx de Shaders
        /// </summary>
        /// <param name="path">Path del archivo .fx</param>
        /// <returns>Effect cargado</returns>
        public static Effect loadEffect(string path)
        {
            string compilationErrors;
            Effect effect = Effect.FromFile(GuiController.Instance.D3dDevice, path, null, null, ShaderFlags.None, null, out compilationErrors);
            if (effect == null)
            {
                throw new Exception("Error al cargar shader: " + path + ". Errores: " + compilationErrors);
            }
            return effect;
        }




    }
}
