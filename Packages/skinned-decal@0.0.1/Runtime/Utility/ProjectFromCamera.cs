// Created By: WangYu  Date: 2024-09-27

using Sirenix.OdinInspector;
using UnityEngine;

namespace SkinnedDecals
{
    /// <summary>
    /// 用鼠标通过摄像机来投射贴花
    /// </summary>
    public class ProjectFromCamera : MonoBehaviour
    {
        /// <summary>
        /// 要控制的所有蒙皮贴花系统
        /// </summary>
        public SkinnedDecalSystem[] skinnedDecalSystems;
        
        /// <summary>
        /// 贴花类型
        /// </summary>
        public SkinnedDecal decal;

        /// <summary>
        /// 旋转角度
        /// </summary>
        public float angle;

        [Range(0, 1)]
        public float progress = 1;
        
        
        private Camera m_camera;

        private void Start()
        {
            m_camera = GetComponent<Camera>();
        }

        private void Update()
        {
            if(decal == null || m_camera == null) return;
            
            if (Input.GetMouseButtonDown(0) || (Input.GetKey(KeyCode.LeftControl) && Input.GetMouseButton(0)))
            {
                // 屏幕触点射线
                Ray ray = m_camera.ScreenPointToRay(Input.mousePosition);
                
                for (int i = 0; i < skinnedDecalSystems.Length; i++)
                {
                    if (skinnedDecalSystems[i] == null) continue;
                    
                    skinnedDecalSystems[i].CreateDecal(decal, ray.origin, ray.direction, angle);
                }
            }
        }

        [PropertySpace]
        [Button("修改显示进度")]
        private void ChangeShowProgressBtn()
        {
            for (int i = 0; i < skinnedDecalSystems.Length; i++)
            {
                skinnedDecalSystems[i]?.ChangeShowProgress(progress);
            }
        }
        
    }
}