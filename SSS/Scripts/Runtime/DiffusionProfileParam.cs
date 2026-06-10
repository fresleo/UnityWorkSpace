using System;
using Unity.Mathematics;
using UnityEngine;

namespace Garena.TA.SSS
{
    [CreateAssetMenu(
        fileName = "SSSResolveProfileParams",
        menuName = "Rendering/SSS/Resolve Profile Params",
        order = 1100)]
    public sealed class DiffusionProfileParam : ScriptableObject
    {
        [SerializeField]
        public Color scatteringColor = new Color(0.6f, 0.3f, 0.2f);
        [SerializeField]
        public float scatteringMultiplier = 1.0f;
        [SerializeField]
        public float maxRadius = 5.0f;
        [SerializeField]
        public float worldScale = 1f;
        [SerializeField]
        public float indexOfRefraction = 1.38f;
        [SerializeField]
        public int kernelSampleCount = 32;
        
        private int importanceCdfResolution = 1024;
    
        [HideInInspector]
        public int discSampleCount = 32;
        public float discKernelMaxRadius = 15f;
        public float inputMaxRadius = 1f;
        public Vector4 shape =  Vector4.zero;
        public float d;
        
        [Header("Generated Textures")]
        public Texture2D discKernelTex;
        public Texture2D discPreviewTexture;

        
        private static float ShapeParam(float a)
        {
            float diff = a - 0.8f;
            return 1.85f - a + 7.0f * diff * diff * diff;
        }
        public float GetMeanFreePath(float _maxRadius)
        {
            float ell = _maxRadius / 3.0f;
            return ell;
        }
        public void updateKernel()
        {
            discSampleCount = kernelSampleCount;
            inputMaxRadius = maxRadius;
            Vector4 s = scatteringColor * scatteringMultiplier;
            d = GetMeanFreePath(maxRadius);
            shape = new Vector4(
                ShapeParam(s.x),
                ShapeParam(s.y),
                ShapeParam(s.z),
                d
            );
        }

        private void OnEnable()
        {
            updateKernel();
        }
    }
}
