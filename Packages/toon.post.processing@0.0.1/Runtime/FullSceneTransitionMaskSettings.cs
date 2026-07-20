/*******************************************************************************
 * File: FullSceneTransitionMaskSettings.cs
 * Author: WangYu
 * Date: 2026-01-23
 * Description: 类或文件功能描述
 *
 * Notice: 注意事项描述（无可省略）
 *******************************************************************************/

using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ToonPostProcessing
{
    [Serializable]
    public class FullSceneTransitionMaskSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        public Camera TargetCamera;

        // 展开 pass >>>>>>>>>>>>>>>>
        public Material raySphereMaskMaterial, transitionMaterial;
        
        public Transform sphereTransform;
        public float poleThresholdInner, poleThresholdOuter;
        public float irregularEdgeWidth;
        public Vector3 fillFogFlowDirection;
        public bool useBlendTex;
        public RenderTexture blendRT;

        // 叠加 pass >>>>>>>>>>>>>>>>>
        public Material backRaySphereMaskMaterial, backTransitionMaterial;
        
        public Transform backSphereTransform;
        public float backPoleThresholdInner, backPoleThresholdOuter;
        public bool backUseBlendTex;
        public RenderTexture backBlendRT;

        public void Reset()
        {
            sphereTransform = null;
            poleThresholdInner = 0;
            poleThresholdOuter = 0;
            irregularEdgeWidth = 0;
            useBlendTex = false;
            blendRT = null;

            backSphereTransform = null;
            backPoleThresholdInner = 0;
            backPoleThresholdOuter = 0;
            backUseBlendTex = false;
            backBlendRT = null;
        }
    }
}