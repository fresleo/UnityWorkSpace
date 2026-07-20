// Created By: WangYu  Date: 2025-04-01

using UnityEngine;

namespace XKT.TOD.Lightmap
{
    public interface ILightmapVolume
    {
        Collider VolumeCollider { get; }
        
        Vector3 Position { get; }
    }
}