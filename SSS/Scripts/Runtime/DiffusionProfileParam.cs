using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor.Rendering.HighDefinition;

namespace Garena.TA.SSS
{
    [CreateAssetMenu(
        fileName = "SSSResolveProfileParams",
        menuName = "Rendering/Knight Profile Params",
        order = 1100)]
    public sealed class DiffusionProfileParam : ScriptableObject
    {
        [SerializeField] public Color scatteringColor = new Color(0.6f, 0.3f, 0.2f);
        [SerializeField] public float scatteringMultiplier = 1.0f;
        [SerializeField] public float maxRadius = 5.0f;
        [SerializeField] public float worldScale = 1f;
        [SerializeField] public float indexOfRefraction = 1.38f;
        [SerializeField] public int kernelSampleCount = 32;
        [SerializeField] public float Fresnel0;
        [SerializeField] public float FresnelScale;
        [SerializeField] public Color TransmissionColor;
        [SerializeField] public float ThicknessRemapMin;
        [SerializeField] public float ThicknessRemapMax;

        
        [HideInInspector] [SerializeField] public int InputDiscSampleCount = 32;
        [HideInInspector] [SerializeField] public float InputMaxRadius = 15f;
        [HideInInspector] [SerializeField] public Vector4 InputShape = Vector4.zero;
        [HideInInspector][SerializeField] public float Input_d;
        [HideInInspector][SerializeField] public float InputWroldScale;
        
        [HideInInspector] [SerializeField] public float InputFresnel0;
        [HideInInspector][SerializeField] public float InputFresnelScale;
        [HideInInspector]  [SerializeField] public Vector3 InputTransmissionColor;
        [HideInInspector] [SerializeField] public float InputThicknessRemapMin;
        [HideInInspector] [SerializeField] public float InputThicknessRemapMax;
        [Header("Generated Textures")] public Texture2D discKernelTex;
        public RenderTexture discPreviewTexture;
        public uint hash;
        

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
            InputDiscSampleCount = kernelSampleCount;
            InputMaxRadius = maxRadius;
            Color LinearColor = scatteringColor.linear;
            Vector4 s = LinearColor * scatteringMultiplier;
            Input_d = GetMeanFreePath(maxRadius);
            InputWroldScale = worldScale;
            InputShape = new Vector4(
                ShapeParam(s.x),
                ShapeParam(s.y),
                ShapeParam(s.z),
                Input_d
            );
        }

        private void OnEnable()
        {
            updateKernel();
#if UNITY_EDITOR
            DiffusionProfileHashTable.UpdateDiffusionProfileHashNow(this);
#endif
        }

#if UNITY_EDITOR
        internal void Reset()
        {
            hash = DiffusionProfileHashTable.GenerateUniqueHash(this);
        }
#endif
    }
}