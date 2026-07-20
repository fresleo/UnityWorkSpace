// Created By: WangYu  Date: 2024-11-20

using UnityEngine;

namespace RaindropEffect
{
    public struct RaindropEffectSPID
    {
        public static readonly int _MainTex = Shader.PropertyToID("_MainTex");
        public static readonly int _RaindropTex = Shader.PropertyToID("_RaindropTex");
        public static readonly int _DropletTex = Shader.PropertyToID("_DropletTex");

        public static readonly int _Refraction = Shader.PropertyToID("_Refraction");
        public static readonly int _LightPosition = Shader.PropertyToID("_LightPosition");
        public static readonly int _RaindropColor = Shader.PropertyToID("_RaindropColor");
        public static readonly int _AlphaSmoothRange = Shader.PropertyToID("_AlphaSmoothRange");
    }
}