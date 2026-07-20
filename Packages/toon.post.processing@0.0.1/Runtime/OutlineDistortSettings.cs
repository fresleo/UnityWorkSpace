// Created by: WangYu   Date: 2025-12-24

using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ToonPostProcessing
{
    [Serializable]
    public class OutlineDistortSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        
        public bool drawMask;
        // 屏蔽LayerMask, 只使用EffectID筛选.
        //public LayerMask targetLayerMask = -1;

        /// <summary>
        /// ScreenEffect ID 筛选（0 = 不筛选，使用 LayerMask；大于 0 = 只渲染由
        /// XKnightScreenEffectIdMaskPass.SetId 注册了对应 effectId 的 Renderer）。
        /// </summary>
        public int targetEffectId = 0;
    }
}