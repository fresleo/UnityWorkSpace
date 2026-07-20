using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Garena.TA
{

    [Serializable]
    public class GPerObjectShadowSettings
    {
        public Color shadowColor = Color.black;

        public GPerObjectShadowPassSettings shadowPassSettings = new GPerObjectShadowPassSettings();
        public GPerObjectSelfShadowPassSettings selfShadowPassSetting = new GPerObjectSelfShadowPassSettings();
        public GPerObjectShadowResolvePassSettings resolvePassSettings = new GPerObjectShadowResolvePassSettings();
        public GPerObjectShadowApplyPassSettings applyPassSettings = new GPerObjectShadowApplyPassSettings();
    }

    /// <summary>
    /// 逐物体阴影Pass设置
    /// </summary>
    [Serializable]
    public class GPerObjectSelfShadowPassSettings // : GPerObjectShadowPassSettings
    {
        public bool enable;

        /// <summary>
        /// 最大物体个数
        /// </summary>
        [Range(1, 16)]
        public int MaxCount = 1;

        /// <summary> 阴影角度偏移 </summary>
        [Range(0.0f, 1.0f)] public float shadowOffset = 0.5f;

        /// <summary>
        /// 合并包围盒，将所有物体按一个物体渲染
        /// </summary>
        public bool CombineBounds;

        /// <summary>
        /// 每个物体的阴影贴图分辨率
        /// </summary>
        [Range(64, 1024)]
        public int SliceTextureSize = 512;

        /// <summary>
        /// 视椎体是否按百分比拓展
        /// </summary>
        public bool FrustumExtendUsePercent;

        /// <summary>
        /// 视椎体扩展
        /// </summary>
        public Vector3 FrustumExtend = new Vector3(0, 0, 0);

        /// <summary>
        /// 使用自定义光照方向
        /// </summary>
        public bool OverrideLightRotation;

        /// <summary>
        /// 自定义光照方向
        /// </summary>
        public Vector3 LightRotation;

        public Vector3 ShadowBias = new Vector3(-0.02f, 0.0f, 1);
    }

    /// <summary>
    /// 逐物体阴影Pass设置
    /// </summary>
    [Serializable]
    public class GPerObjectShadowPassSettings
    {
        public bool enable;
        /// <summary>
        /// 插入点
        /// </summary>
        public RenderPassEvent Event = RenderPassEvent.BeforeRenderingShadows;

        /// <summary>
        /// 最大物体个数
        /// </summary>
        [Range(1, 16)]
        public int MaxCount = 4;

        [Tooltip("开启SRP Batcher, 需要使用RenderingLayerMask 16-31")]
        public bool srpBatcher = false;

        [Tooltip("使用 ‘shadowOnlyShader’ 渲染 Shadow (仅 SRP Batcher 模式可用)")]
        public bool useShadowOnlyShader = false;
        
        /// <summary>
        /// 合并包围盒，将所有物体按一个物体渲染
        /// </summary>
        public bool CombineBounds;

        /// <summary>
        /// 每个物体的阴影贴图分辨率
        /// </summary>
        [Range(64, 1024)]
        public int SliceTextureSize = 256;

        /// <summary>
        /// 视椎体是否按百分比拓展
        /// </summary>
        public bool FrustumExtendUsePercent;

        /// <summary>
        /// 视椎体扩展
        /// </summary>
        public Vector3 FrustumExtend = new Vector3(0, 0, 0);

        /// <summary>
        /// 使用自定义光照方向
        /// </summary>
        public bool OverrideLightRotation;

        /// <summary>
        /// 自定义光照方向
        /// </summary>
        public Vector3 LightRotation;

        public Vector3 ShadowBias = new Vector3(-0.02f, 0.0f, 1);

        /// <summary>
        /// 物体最大距离
        /// </summary>
        public float Distance = 50;

        /// <summary>
        /// 使用视锥剔除
        /// </summary>
        public bool Cull;

        /// <summary>
        /// 排序
        /// </summary>
        public bool Sort;

        /// <summary>
        /// LOD偏移
        /// </summary>
        [Range(0, 8)]
        public int LODBias = 0;

        public bool UseFilter = false;

        /// <summary>
        /// 对象过滤设置
        /// </summary>
        public FilterSettings filterSettings = new FilterSettings();

        [System.Serializable]
        public class FilterSettings
        {
            /// <summary>
            /// 物体Layer
            /// </summary>
            //public LayerMask LayerMask;

            /// <summary>
            /// 物体标签
            /// </summary>
            public string Tag = "Player";
        }
        
        public Shader shadowOnlyShader;
    }

    [Serializable]
    public class GPerObjectShadowResolvePassSettings
    {
        public bool enable;
        public RenderPassEvent Event = RenderPassEvent.AfterRenderingGbuffer;

        public bool resolveToRenderTexture = true;

        public bool useCharacterMask = false;

        /// <summary>
        /// 是否同时resolve主平行光阴影
        /// </summary>
        public bool resolveMainLightShadow = false;

        public bool resolveToScreenSpaceShadow;

        public Shader resolveShader;

        /// <summary>
        /// 使用后处理方式
        /// </summary>
        public bool usePostMethod = false;
        public Shader postResolveShader;

        public BlendMode srcBlend;
        public BlendMode dstBlend;
    }

    [Serializable]
    public class GPerObjectShadowApplyPassSettings
    {
        public bool enable;
        public RenderPassEvent Event = RenderPassEvent.BeforeRenderingTransparents;
        public Shader applyShader;

        public BlendMode srcBlend;
        public BlendMode dstBlend;
    }
}
