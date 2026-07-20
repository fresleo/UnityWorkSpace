// Created By: WangYu  Date: 2024-11-20

using UnityEngine;

namespace RaindropEffect
{
    public struct DropletSPID
    {
        public static readonly int _MainTex = Shader.PropertyToID("_MainTex");

        public static readonly int _Seed = Shader.PropertyToID("_Seed");
        public static readonly int _SpawnRect = Shader.PropertyToID("_SpawnRect");
        public static readonly int _SizeRange = Shader.PropertyToID("_SizeRange");
    }
}