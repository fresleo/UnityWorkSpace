
using UnityEngine;

namespace XKnight.Reflection.Runtime
{
    public static class XKTSSRShaderProperties
    {
        public static int MainTex = Shader.PropertyToID("_MainTex");
        public static int NoiseTex = Shader.PropertyToID("_NoiseTex");

        public static int SSRSettings = Shader.PropertyToID("_SSRSettings");
        public static int SSRSettings2 = Shader.PropertyToID("_SSRSettings2");

        /// <summary>
        /// xy屏幕宽高,z-ldsFactor,w-depthBias
        /// </summary>
        public static int SSRSettings3 = Shader.PropertyToID("_SSRSettings3");

        public static int SSRSettings4 = Shader.PropertyToID("_SSRSettings4");
        public static int SSRSettings5 = Shader.PropertyToID("_SSRSettings5");
        public static int SSRSettings6 = Shader.PropertyToID("_SSRSettings6");

        public static int WorldToViewDir = Shader.PropertyToID("_WorldToViewDir");
        public static int ViewToWorldDir = Shader.PropertyToID("_ViewToWorldDir");

        public static int MinimumThickness = Shader.PropertyToID("_MinimumThickness");


        public static int SSRBlurStrength = Shader.PropertyToID("_SSRBlurStrength");
        public static int MinimumBlur = Shader.PropertyToID("_MinimumBlur");

        //RT
        public static int RayCast = Shader.PropertyToID("_RayCastRT");
        public static int DownscaledDepthRT = Shader.PropertyToID("_DownscaledDepthRT");
        public static int CameraViewDir = Shader.PropertyToID("_CameraViewDir");
        public static int ReflectionsTex = Shader.PropertyToID("_ReflectionsRT");
        public static int BlurRT = Shader.PropertyToID("_BlurRT");
        public static int NaNBuffer = Shader.PropertyToID("_NaNBuffer");
        public const string DownscaledBackDepthTextureName = "_DownscaledBackDepthRT";
        public static int DownscaledBackDepthRT = Shader.PropertyToID(DownscaledBackDepthTextureName);
        public const string REFINE_THICKNESS = "SSR_THICKNESS_FINE";
        public const string BACK_FACES = "SSR_BACK_FACES";
        public const string SKYBOX = "SSR_SKYBOX";



    }

}