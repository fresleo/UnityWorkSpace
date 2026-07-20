using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    public class CloudProfile: ScriptableObject
    {
        public Mesh[] CloudMeshes;
        public Material[] CloudMaterials;

        public float CloudCurlSpeed = 1f;
        public float CloudCurlTiling = 3f;
        public float CloudCurlAmplitude = 0.02f;
		public float CloudSunBrightenIntensity = 0.8299f;
		[Range(0f, 1f)] public float CloudTransparency = 1f;
        [Range(0f, 1f)] public float CloudCoverage = 0.11f;
		public float CloudVolumeChangeSpeed = 1f;       
        
        [Range(0f, 0.5f)] public float CloudFrontAndBackBlendFactor = 0.0881f;
        public Color CloudDarkBackColor = new Color(0.02257f, 0.23783f, 0.45227f);
        public Color CloudDarkFrontColor = new Color(0.08773f, 0.35994f, 0.58044f);
        public Color CloudLightBackColor = new Color(0.57591f, 0.79012f, 0.94779f);
        public Color CloudLightFrontColor = new Color(0.57203f, 0.70038f, 0.76956f);

        public Mesh CloudLayerMesh;
        public Material CloudLayerMaterial;

        //public Vector2 CloudDirection = new Vector2(-1f, 0f);
        //public float CloudHeight = 0.087f;
        public float CloudWispsSpeed = 0.05f;
        [Range(0.1f, 1f)] public float CloudWispsCoverage = 1f;
        [Range(0f, 2f)] public float CloudWispsOpacity = 0.6f;

        public Texture CloudShadowTex;
        public float CloudShadowTiling = 0.005f;
        public Vector2 CloudShadowSpeed = new Vector2(0.5f, 0.5f);
        public Color CloudShadowColor = new Color(0f, 0f, 0f, 0.7f);

        public CloudProfile CopyInstance(string name)
        {
            CloudProfile profile = CreateInstance<CloudProfile>();
            profile.name = string.Format("_Map_CloudSkyProfile_{0}", name);

            profile.CloudCurlSpeed = CloudCurlSpeed;
            profile.CloudCurlTiling = CloudCurlTiling;
            profile.CloudCurlAmplitude = CloudCurlAmplitude;
            profile.CloudSunBrightenIntensity = CloudSunBrightenIntensity;
            profile.CloudTransparency = CloudTransparency;
            profile.CloudCoverage = CloudCoverage;
            profile.CloudVolumeChangeSpeed = CloudVolumeChangeSpeed;
            profile.CloudFrontAndBackBlendFactor = CloudFrontAndBackBlendFactor;
            profile.CloudDarkBackColor = CloudDarkBackColor;
            profile.CloudDarkFrontColor = CloudDarkFrontColor;
            profile.CloudLightBackColor = CloudLightBackColor;
            profile.CloudLightFrontColor = CloudLightFrontColor;

            //profile.CloudDirection = CloudDirection;
            //profile.CloudHeight = CloudHeight;
            profile.CloudWispsSpeed = CloudWispsSpeed;
            profile.CloudWispsCoverage = CloudWispsCoverage;
            profile.CloudWispsOpacity = CloudWispsOpacity;

            return profile;
        }
    }  
}