// Created By: WangYu  Date: 2025-04-03

using System;
using UnityEditor;
using UnityEngine;
using XKT.TOD.Tag;

namespace XKT.TOD.Lightmap
{
    [CustomEditor(typeof(LightmapTag))]
    public class LightmapTagEditor : AbsTodTagEditor
    {
        LightmapTag CurrentTarget => this.target as LightmapTag;
        
    }
}