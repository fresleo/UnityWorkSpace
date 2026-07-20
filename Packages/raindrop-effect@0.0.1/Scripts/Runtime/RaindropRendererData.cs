// Created By: WangYu  Date: 2024-11-19

using System;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace RaindropEffect
{
    /// <summary>
    /// 全局渲染配置
    /// </summary>
    //[CreateAssetMenu(fileName = "RaindropRendererData", menuName = "屏幕雨滴特效/全局渲染配置")]
    public class RaindropRendererData : ScriptableObject
    {
#if UNITY_EDITOR
        [MenuItem("Assets/Create/屏幕雨滴特效/全局渲染配置")]
        public static void CreateMyScriptableObject()
        {
            RaindropEffectEditorUtils.CreateScriptableObject<RaindropRendererData>(nameof(RaindropRendererData));
        }
#endif // UNITY_EDITOR
        
        
        public const string c_packagePath = "Packages/raindrop-effect";
        
        [Serializable, ReloadGroup]
        public sealed class RenderResources
        {
            [Reload("Materials/Raindrop.mat")]
            public Material raindropMaterial;
            
            [Reload("Shaders/Droplet.shader")]
            public Shader dropletShader;
            
            [Reload("Shaders/RaindropEffect.shader")]
            public Shader raindropEffectShader;
        }
        
        /// <summary>
        /// 渲染资源
        /// </summary>
        public RenderResources renderResources = null;

        public void Cleanup()
        {
            renderResources = null;
        }
    }
}