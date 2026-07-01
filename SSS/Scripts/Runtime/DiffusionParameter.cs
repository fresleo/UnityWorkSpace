using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor.Rendering.HighDefinition;

namespace Garena.TA.SSS
{
    [CreateAssetMenu(
        fileName = "XKnightDiffusionParameter",
        menuName = "Rendering/DiffusionParameter",
        order = 1100)]
    public sealed class DiffusionParameter : ScriptableObject
    {
        [SerializeField] public Color scatteringColor = new Color(0.6f, 0.3f, 0.2f);
        [SerializeField] public float scatteringMultiplier = 1.0f;
 
        [SerializeField] public float worldScale = 1f;
        [SerializeField] public float indexOfRefraction = 1.38f;
        [SerializeField] public int kernelSampleCount = 32;

        [SerializeField] public float Fresnel0;
        [SerializeField] public float FresnelScale;
        [SerializeField] public Color TransmissionTint;
        [SerializeField] public float ThicknessRemapMin;
        [SerializeField] public float ThicknessRemapMax;
        [SerializeField] public float ThickOffset = 0f;
        [SerializeField] public float ShadowStrenthen = 0f;
        //=======================intpu =================================
        [HideInInspector] [SerializeField] public int InputDiscSampleCount = 32;
        [HideInInspector] [SerializeField] public float InputMaxRadius = 15f;
        [HideInInspector] [SerializeField] public Vector4 InputShape = Vector4.zero;
        [HideInInspector] [SerializeField] public float Input_d;
        [HideInInspector] [SerializeField] public float InputWroldScale;

        [HideInInspector] [SerializeField] public float InputFresnel0;
        [HideInInspector] [SerializeField] public float InputFresnelScale;
        [HideInInspector] [SerializeField] public Vector3 InputTransmissionTint;
        [HideInInspector] [SerializeField] public Vector4 InputThicknessRemap;
        [HideInInspector] [SerializeField] public float InputThickOffset;
        [HideInInspector] [SerializeField] public float InputShadowStrenthen;
        
        [Header("Generated Textures")] public Texture2D discKernelTex;
        public RenderTexture discPreviewTexture;
        public RenderTexture TransmistPreviewTexture;
        public uint hash;


        float SampleBurleyDiffusionProfile()
        {
            float u = 0.997f;//模拟光照幅度为0.997的半径，其中0.003为损耗量
            float rcpS = GetMeanFreePath();
            u = 1 - u; 

            float g = 1 + (4 * u) * (2 * u + Mathf.Sqrt(1 + (4 * u) * u));
            float n = Mathf.Pow(g, -1.0f / 3.0f); // g^(-1/3)
            float p = (g * n) * n; // g^(+1/3)
            float c = 1 + p + n; // 1 + g^(+1/3) + g^(-1/3)
            float x = 3 * Mathf.Log(c / (4 * u));

            return x * rcpS;
        }

        public float GetMeanFreePath()
        {
            Vector3 sd = (Vector3)(Vector4)scatteringColor * scatteringMultiplier;
            return Mathf.Max(sd.x, sd.y, sd.z);
        }
        

        public void updateKernel()
        {
            InputDiscSampleCount = kernelSampleCount;
         
            Color LinearColor = scatteringColor.linear;
            Vector4 s = LinearColor * scatteringMultiplier;
            Input_d = GetMeanFreePath();
            InputWroldScale = worldScale;
            InputShape = new Vector4(
                BurleyFunction.ShapeParam(s.x),
                BurleyFunction.ShapeParam(s.y),
                BurleyFunction.ShapeParam(s.z),
                Input_d
            );
            InputMaxRadius = SampleBurleyDiffusionProfile();
            InputShadowStrenthen = ShadowStrenthen;
            //transmit
            InputFresnel0 = Fresnel0;
            InputFresnelScale = FresnelScale;
            InputTransmissionTint = new Vector3(TransmissionTint.linear.r, TransmissionTint.linear.g,
                TransmissionTint.linear.b);
            InputThicknessRemap = new Vector4(ThicknessRemapMin, ThicknessRemapMax, 0, 0);
            InputThickOffset = ThickOffset;
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