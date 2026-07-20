// Created By: WangYu  Date: 2025-02-18

using System;
using AirSticker.Runtime.Logic;
using UnityEngine;

namespace AirSticker.Runtime.Test
{
    [ExecuteAlways]
    public class DebugAirSticker : MonoBehaviour
    {
        public LayerMask layerMask;
        public AbsDecalConfig mdConfig;
        
        private RaycastHit m_hitInfo;
        private bool m_isHit;
        
        
        private void OnDrawGizmos()
        {
            if(mdConfig == null) return;

            Color gColor = m_isHit ? Color.red : Color.white;
            GizmosTools.DrawDecalBox(transform, gColor, mdConfig.boxWidth, mdConfig.boxHeight, mdConfig.boxDepth);
            
            Gizmos.DrawIcon(this.transform.position, "d_WelcomeScreen.AssetStoreLogo", true, Color.white);
        }

        private void Update()
        {
            if(mdConfig == null) return;
            
            Vector3 originPos = transform.position - transform.forward * mdConfig.boxDepth * 0.5f;
            
            // Vector3 boxSize = new Vector3(smdConfig.boxWidth, smdConfig.boxHeight, smdConfig.boxDepth);
            // m_isHit = Physics.BoxCast(originPos, boxSize / 2, transform.forward, out m_hitInfo, transform.rotation, smdConfig.boxDepth);
            
            m_isHit = Physics.Raycast(originPos, transform.forward, out m_hitInfo, mdConfig.boxDepth, layerMask);
        }
        

        /// <summary>
        /// 投射
        /// </summary>
        /// <param name="isImmortalized">永生的</param>
        /// <param name="order">顺序</param>
        public void Cast(bool isImmortalized, int order = 0)
        {
            if (mdConfig == null)
            {
                Debug.LogError("没配置");
                return;
            }
            if (!m_isHit)
            {
                Debug.LogError("没命中");
                return;
            }

            Transform hitTransform = m_hitInfo.transform;
            
            var mapping = hitTransform.GetComponent<NodeMapping>();
            if (mapping == null)
            {
                Debug.LogError($"没有找到映射脚本: {hitTransform.name}");
                return;
            }
            
            GameObject receiverObject = mapping.GetReceiverObject();
            if (receiverObject == null)
            {
                Debug.LogError($"没有获取到接收对象: {hitTransform.name}");
                return;
            }
            
            Debug.LogError($"接收对象: {receiverObject.name}");

            AbsDecalConfig fmdConfig = mdConfig;
            if (isImmortalized)
            {
                fmdConfig = Instantiate(mdConfig);
                fmdConfig.duration = -1;
            }

            Vector3 projectorPosition = transform.position;
            Quaternion projectorRotation = transform.rotation;
            
            var projector = AirStickerProjector.Create(
                projectorPosition, projectorRotation, 
                receiverObject, fmdConfig, 
                order);
            projector.Launch(
                result =>
                {
                    Debug.LogError($"贴花完成: {result}");
                });
        }
        
    }
}
