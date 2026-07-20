// Created By: WangYu  Date: 2025-02-22

using System;
using System.Collections.Generic;
using UnityEngine;

namespace AirSticker.Runtime.Logic
{
    public interface IDecalMeshRenderer
    {
        public const string c_rendererName = "Air Sticker Renderer";
        
        void ReleaseRendering();
        
        void CreateLifecycle(long uniqueKey, AbsDecalConfig lifeConfig, Action<long> callback);
    }
}