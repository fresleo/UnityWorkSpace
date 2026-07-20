// Created By: WangYu  Date: 2023-12-01

using com.xknight.mt.Lib.Runtime.MT.Serialize;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.InstancedObject
{
    public partial class InstancedObjectDataGenerator
    {
        //绘制调试信息
        public bool debugTree;
        public bool debugChildren, debugChildrenRate;
        public bool debugVolume;
        /// <summary>
        /// 剔除摄像机
        /// </summary>
        public Camera cullCamera;
        
        private GUIStyle s_rate_gs;

        protected override void OnDrawGizmos()
        {
            if (!this.enabled)
            {
                return;
            }
            
            var lastColor = Gizmos.color;
            {
                //画树
                if (dataType == IOGroupConfig.EDataType.Tree && debugTree)
                {
                    quadTreeRoot?.DrawDebug();
                }

                //画子对象
                if (debugChildren)
                {
                    Gizmos.color = Color.red;
                    foreach (var marker in childrenMarkers)
                    {
                        var markerBnd = marker.triggerBnd;
                        Gizmos.DrawWireCube(markerBnd.center, markerBnd.size);
                    }
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
