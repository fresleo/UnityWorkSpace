// Created By: WangYu  Date: 2023-12-01

using com.xknight.mt.Lib.Runtime.MT.Utils;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.StaticObject
{
    public partial class StaticObjectDataGenerator
    {
        //绘制调试信息
        public bool debugChildren, debugChildrenName;
        public bool debugTree, debugTreeLabel;
        public bool debugVolume;
        
        private GUIStyle s_child_gs;

        protected override void OnDrawGizmos()
        {
            if (!this.enabled)
            {
                return;
            }
            
            var lastColor = Gizmos.color;
            {
                //画静态子对象
                if (debugChildren)
                {
                    Gizmos.color = Color.red;
                    foreach (var item in childrenGos)
                    {
                        Bounds childBounds = MTRuntimeUtils.GetWholeBounds(item);
                        Gizmos.DrawWireCube(childBounds.center, childBounds.size);
                    }
                }

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
