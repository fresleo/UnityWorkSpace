// Created by: WangYu   Date: 2025-12-15

using System;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace ToonPostProcessing
{
    public class WaterColorGroupRendererData : ScriptableObject
    {
#if UNITY_EDITOR
        [MenuItem("Assets/Create/卡通后处理/水彩后分组渲染数据")]
        private static void CreateAsset()
        {
            PackageUtils.CreateScriptableObject<WaterColorGroupRendererData>(nameof(WaterColorGroupRendererData));
        }
#endif // UNITY_EDITOR
        
        [Serializable, ReloadGroup]
        public sealed class RenderResources
        {
            [Reload("Shaders/PreObjectIdOutline.shader")]
            public Shader preObjectIdOutlineShader;

            [Reload("Shaders/SobelOutline.shader")]
            public Shader sobelOutlineShader;

            [Reload("Shaders/ViewSpaceNormals.shader")]
            public Shader viewSpaceNormalsShader;

            [Reload("Shaders/ViewSpaceNormalsOutline.shader")]
            public Shader viewSpaceNormalsOutlineShader;

            [Reload("Shaders/WaterColor.shader")]
            public Shader waterColorShader;

            [Reload("Shaders/WaterColorV2.shader")]
            public Shader waterColorV2Shader;

            public bool HasAllLoaded => 
                preObjectIdOutlineShader != null 
                && sobelOutlineShader != null 
                && viewSpaceNormalsShader != null && viewSpaceNormalsOutlineShader != null 
                && waterColorShader != null && waterColorV2Shader != null;
        }
        
        public RenderResources renderResources;
        
    }
}