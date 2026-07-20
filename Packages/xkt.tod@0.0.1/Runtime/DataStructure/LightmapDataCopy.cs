// Created By: WangYu  Date: 2025-04-12

using System;
using UnityEngine;

namespace XKT.TOD.DataStructure
{
    [Serializable]
    public class LightmapDataCopy
    {
        public Texture2D lightmapColor;
        public Texture2D lightmapDir;
        public Texture2D shadowMask;
    }
}