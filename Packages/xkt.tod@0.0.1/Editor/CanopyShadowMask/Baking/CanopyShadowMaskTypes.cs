/*******************************************************************************
 * File: CanopyShadowMaskTypes.cs
 * Author: WangYu
 * Date: 2026-06-30
 * Description: 
 *******************************************************************************/

using System;
using UnityEngine;

namespace XKT.TOD.CanopyShadowMask
{
    /// <summary>
    /// 用于混合灯光的 Shadowmask 纹理通道
    /// </summary>
    public enum EShadowMaskChannel
    {
        R = 0, G, B, A
    }

    /// <summary>
    /// 编辑器窗口的用户可配置烘焙参数
    /// </summary>
    [Serializable]
    public sealed class CanopyShadowMaskBakeParams
    {
        /// <summary>
        /// 混合光，拥有 shadowmask 通道。
        /// </summary>
        public Light mainLight;

        /// <summary>
        /// 可用的话，就自动使用 mainLight.bakingOutput.occlusionMaskChannel 
        /// </summary>
        public bool autoUseLightShadowmaskChannel = true;

        /// <summary>
        /// Shadowmask 通道用于合成。当自动通道被禁用时使用手动的。
        /// </summary>
        public EShadowMaskChannel channel = EShadowMaskChannel.R;

        /// <summary>
        /// 地面接收器的层 mask。0 = 所有层。
        /// </summary>
        public LayerMask groundLayerMask = ~0;

        /// <summary>
        /// 排除作为地面的着色器 path。这可以防止植被充当接收器。
        /// </summary>
        public string[] excludedShaderNames = new string[]
        {
            "XKnight/Scene/Tree",
            "XKnight/Scene/Grass",
            "XKnight/Scene/Plant"
        };

        /// <summary>
        /// 可选的手动地面列表。非空时，只使用这些渲染器。
        /// </summary>
        public MeshRenderer[] manualGroundRenderers = new MeshRenderer[0];

        /// <summary>
        /// 用于 shadow-map 合成的 Atlas texel 步长。
        /// 1 表示每个 texel。
        /// </summary>
        [Range(1, 8)]
        public int atlasTexelStep = 1;

        /// <summary>
        /// 针对标记的树冠 shadow-map 样本进行 2x2 超采样。
        /// </summary>
        public bool shadowMapSoftShadow;

        /// <summary>
        /// 对标记的树冠代理遮挡应用全局乘数。
        /// </summary>
        [Range(0f, 1f)]
        public float globalShadowStrength = 1;

        /// <summary>
        /// 为 true 时输出 shadow map 与采样遮罩等 debug 纹理。
        /// </summary>
        public bool writeDebugTextures;
    }
}
