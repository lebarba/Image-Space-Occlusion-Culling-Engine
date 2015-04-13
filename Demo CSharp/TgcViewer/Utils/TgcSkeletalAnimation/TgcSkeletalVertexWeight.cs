using System;
using System.Collections.Generic;
using System.Text;

namespace TgcViewer.Utils.TgcSkeletalAnimation
{
    /// <summary>
    /// Influencias de huesos sobre un vertice.
    /// Un vertice puede estar influenciado por mas de un hueso
    /// </summary>
    public class TgcSkeletalVertexWeight
    {
        public TgcSkeletalVertexWeight()
        {
            this.weights = new List<BoneWeight>();
        }

        private List<BoneWeight> weights;
        /// <summary>
        /// Influencias del vertice
        /// </summary>
        public List<BoneWeight> Weights
        {
            get { return weights; }
        }


        /// <summary>
        /// Influencia de un hueso sobre un vertice
        /// </summary>
        public class BoneWeight
        {
            public BoneWeight(TgcSkeletalBone bone, float weight)
            {
                this.bone = bone;
                this.weight = weight;
            }
            
            
            private TgcSkeletalBone bone;
            /// <summary>
            /// Hueso que influye
            /// </summary>
            public TgcSkeletalBone Bone
            {
                get { return bone; }
                set { bone = value; }
            }
            
            private float weight;
            /// <summary>
            /// Influencia del hueso sobre el vertice. Valor normalizado entre 0 y 1
            /// </summary>
            public float Weight
            {
                get { return weight; }
                set { weight = value; }
            }
        }


    }
}
