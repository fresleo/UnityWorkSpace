// Created By: WangYu  Date: 2025-04-29

using UnityEngine;

namespace AirSticker.Runtime.Render
{
    public class RendererTask
    {
        public long uniqueKey;
        
        public float fadeinTimer;
        public float durationTimer;
        public float fadeoutTimer;
    }
    
    public class RendererTaskData
    {
        public long uniqueKey;
        
        public float fadeinTime;
        public AnimationCurve fadeinCurve;
        
        public float duration;
        
        public float fadeoutTime;
        public AnimationCurve fadeoutCurve;
    }
}