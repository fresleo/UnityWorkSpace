// Created By: WangYu  Date: 2024-09-28

using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SkinnedDecals
{
    /// <summary>
    /// 从 Transform 的 forward 方向上投射贴花
    /// </summary>
    public class ProjectFromTransform : MonoBehaviour
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
        /// 当前贴花的世界空间命中点标记
        /// </summary>
        public GameObject currentDecalWorldSpaceHitPointMarkGo;
        
        /// <summary>
        /// 贴花出现过程的进度
        /// </summary>
        [Range(0, 1)]
        public float decalShowProgress = 1;
        
        
        private Coroutine m_shootCoroutine;
        private List<Vector3> m_worldSpaceHitPoints = new();
        
        private List<GameObject> m_worldSpaceHitPointMarkGos = new();


        private void OnDestroy()
        {
            DestroyMarkGos();
        }

        
        [PropertySpace]
        [Button("发射贴花")]
        private void ShootBtn()
        {
            if(decal == null) return;
            
            for (int i = 0; i < skinnedDecalSystems.Length; i++)
            {
                var itemSystem = skinnedDecalSystems[i];
                if (itemSystem == null) continue;
                
                itemSystem.CreateDecal(decal, transform.position, transform.forward, transform.up);
            }

            // 当用来标记的 go 存在时，才继续往后走
            if (currentDecalWorldSpaceHitPointMarkGo != null)
            {
                if (m_shootCoroutine != null)
                {
                    StopCoroutine(m_shootCoroutine);
                    m_shootCoroutine = null;
                }
                m_shootCoroutine = StartCoroutine(ShootBtnAfter());
            }
        }

        private IEnumerator ShootBtnAfter()
        {
            // 因为多线程本身是在 LateUpdate 中启动的，所以最少等待2帧，或是直接等待1段时间才能有结果回来
            // yield return new WaitForSeconds(0.1f);
            yield return null;
            yield return null;

            DestroyMarkGos();
            
            for (int i = 0; i < skinnedDecalSystems.Length; i++)
            {
                var itemSystem = skinnedDecalSystems[i];
                if (itemSystem == null) continue;
                
                bool hasValid = itemSystem.EvaluateCurrentDecalWorldSpaceHitPoint(transform.position, ref m_worldSpaceHitPoints);
                Debug.LogError($"是否有有效的命中点: {hasValid}");

                if (hasValid)
                {
                    foreach (Vector3 worldSpaceHitPoint in m_worldSpaceHitPoints)
                    {
                        GameObject newMarkGo = Instantiate(currentDecalWorldSpaceHitPointMarkGo);
                        m_worldSpaceHitPointMarkGos.Add(newMarkGo);
                        
                        newMarkGo.SetActive(true);
                        newMarkGo.transform.position = worldSpaceHitPoint;
                    }
                }
            }
        }

        private void DestroyMarkGos()
        {
            foreach (var itemGo in m_worldSpaceHitPointMarkGos)
            {
                Destroy(itemGo);
            }
            m_worldSpaceHitPointMarkGos.Clear();
        }


        [PropertySpace]
        [Button("修改显示进度")]
        private void ClearMarkGos()
        {
            DestroyMarkGos();
        }
        
        [PropertySpace]
        [Button("修改显示进度")]
        private void ChangeShowProgressBtn()
        {
            for (int i = 0; i < skinnedDecalSystems.Length; i++)
            {
                var itemSystem = skinnedDecalSystems[i];
                if (itemSystem == null) continue;
                
                itemSystem.ChangeShowProgress(this.decalShowProgress);
            }
        }
        
    }
}