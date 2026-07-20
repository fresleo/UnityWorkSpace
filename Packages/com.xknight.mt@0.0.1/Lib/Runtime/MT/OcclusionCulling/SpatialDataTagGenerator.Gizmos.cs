// Created By: WangYu  Date: 2024-05-31

using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.OcclusionCulling
{
    public partial class SpatialDataTagGenerator
    {
        public bool debugTree, debugTreeLabel;
        public bool debugVolume;
        
        protected override void OnDrawGizmos()
        {
            if (!this.enabled)
            {
                return;
            }
            
            var lastColor = Gizmos.color;
            {
                //画树
                if (debugTree)
                {
                    quadTreeRoot?.DrawDebugGizmos();
                }
                
                //画体积
                if (debugVolume)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireCube(bnd.center, bnd.size);
                }
            }
            Gizmos.color = lastColor;
        }
    }
}