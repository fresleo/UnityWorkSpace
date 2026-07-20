// Created By: WangYu  Date: 2023-11-30

using System;
using com.xknight.mt.Lib.Runtime.MT.Common;
using com.xknight.mt.Lib.Runtime.MT.Log;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.InstancedObject
{
    public class InstancedObjectMarker : MonoBehaviour, ICubeVolume
    {
        // 所属管理器 - 作用是为了能跳转
        [SerializeField]
        private InstancedObjectDataGenerator m_master;
        public static string Master_PropName = nameof(m_master);
        
        /// <summary>
        /// 目标对象
        /// </summary>
        public GameObject targetGo;
        
        // 目标包围盒
        [SerializeField]
        private Bounds m_targetBnd;
        public static string TargetBnd_PropName = nameof(m_targetBnd);
        
        // 自定义触发器包围盒
        [SerializeField]
        private bool m_customTriggerBnd;
        public static string CustomTriggerBnd_PropName = nameof(m_customTriggerBnd);
        
        /// <summary>
        /// 触发器包围盒
        /// </summary>
        public Bounds triggerBnd;
        
        
        [SerializeField]
        private int m_lightmapIndex;
        public static string LightmapIndex_PropName = nameof(m_lightmapIndex);
        
        [SerializeField]
        private Vector4 m_lightmapScaleOffset;
        public static string LightmapScaleOffset_PropName = nameof(m_lightmapScaleOffset);


        // 其实没什么要在这里面实现的，只是为了能在编辑器中开关脚本
        private void OnEnable()
        {
        }

        /// <summary>
        /// 刷新关系
        /// </summary>
        public bool RefreshRelation()
        {
            m_master = transform.GetComponentInParent<InstancedObjectDataGenerator>();
            if (m_master == null)
            {
                MTLogger.LogError($"无法在父中找到 InstancedObjectVolume 组件 : {gameObject.name}");
                return false;
            }

            UpdateTarget();
            UpdateTrigger();
            
            return true;
        }

        private void UpdateTarget()
        {
            // 首个 LODGroup
            var lg = transform.GetComponentInChildren<LODGroup>();
            if (lg != null && lg.lodCount > 0)
            {
                var lods = lg.GetLODs();
                var lod0 = lods[0];
                
                var renderers = lod0.renderers;
                if (renderers.Length > 0)
                {
                    var r0 = renderers[0];
                    if (r0 != null)
                    {
                        GetRendererData(r0);
                        return;
                    }
                }
            }

            // 首个 MeshRenderer
            var mr = transform.GetComponentInChildren<MeshRenderer>();
            if (mr != null)
            {
                GetRendererData(mr);
            }
        }

        private void GetRendererData(Renderer rend)
        {
            targetGo = rend.gameObject;

            m_targetBnd = new Bounds(targetGo.transform.position, Vector3.zero);
            m_targetBnd.Encapsulate(rend.bounds);
            
            m_lightmapIndex = rend.lightmapIndex;
            m_lightmapScaleOffset = rend.lightmapScaleOffset;
        }
        
        private void UpdateTrigger()
        {
            if (m_customTriggerBnd)
            {
                triggerBnd = new Bounds(m_cubeCenter, m_cubeSize);
            }
            else
            {
                triggerBnd = m_targetBnd;
                m_cubeCenter = m_targetBnd.center;
                m_cubeSize = m_targetBnd.size;
            }
        }
        
        
        // cube 触发包围盒控制 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        private Vector3[] m_cubeHandlePositions = new Vector3[6];
        
        [SerializeField]
        private Vector3 m_cubeCenter;
        public static string CubeCenter_PropName = nameof(m_cubeCenter);
        
        [SerializeField]
        private Vector3 m_cubeSize;
        public static string CubeSize_PropName = nameof(m_cubeSize);

        
        public Vector3[] CubeHandlePositions => m_cubeHandlePositions;

        public Vector3 CubeCenter
        {
            get => m_cubeCenter;
            set => m_cubeCenter = value;
        }

        public Vector3 CubeSize
        {
            get => m_cubeSize;
            set => m_cubeSize = value;
        }
        
    }
}