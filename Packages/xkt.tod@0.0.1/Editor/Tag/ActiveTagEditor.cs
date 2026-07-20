// Created By: WangYu  Date: 2025-04-10

using System;
using UnityEditor;
using UnityEngine;

namespace XKT.TOD.Tag
{
    [CustomEditor(typeof(ActiveTag))]
    public class ActiveTagEditor : AbsTodTagEditor
    {
        ActiveTag CurrentTarget => this.target as ActiveTag;
        
    }
}