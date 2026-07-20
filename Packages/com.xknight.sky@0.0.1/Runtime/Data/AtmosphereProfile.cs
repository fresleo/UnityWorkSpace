using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    public class AtmosphereProfile : ScriptableObject
    {
        #region Rayleigh
        public Material SkyboxMaterial;

        // Wavelengths
        [Range(0.0f, 1000f)] public float WavelengthR = 680f;
        [Range(0.0f, 1000f)] public float WavelengthG = 550f;
        [Range(0.0f, 1000f)] public float WavelengthB = 440f;

        // Density
        [Range(0.0f, 15f)] public float AtmosphereThickness = 1.0f;

        // Sun
        [Range(0.0f, 100f)] public float SunBrightness = 30f;
        public Color SunAtmosphereTint = Color.white;
        [Range(0.0f, 1f)] public float SunIntensityFactor = 0.25f; // for Preetham Atmosphere

        [Range(0.0f, 1f)] public float MoonBrightness = 0.3f;
        public Color MoonAtmosphereTint = new Color(0.1f, 0.1f, 0.1f, 1.0f);
        [Range(0.0f, 1f)] public float MoonIntensityFactor = 0.20f;
        #endregion

        #region Mie
        // Common
        [Range(0.0f, 0.10f)] public float Mie = 0.010f;
        [Range(0.0f, 1f)] public float Turbidity = 0.1f; // for Preetham Atmosphere

        // Sun
        public Color SunMieColor = new Color(1.0f, 0.84f, 0.61f, 1.0f);
        [Range(0.0f, 0.999f)] public float SunMieAnisotropy = 0.75f;
        public float SunMieScattering = 1f;

        // Moon
        public Color MoonMieColor = new Color(1.0f, 0.95f, 0.83f, 1.0f);
        [Range(0.0f, 0.999f)] public float MoonMieAnisotropy = 0.94f;
        public float MoonMieScattering = 1.15f;
        #endregion

        #region Other
        // Color Correction
        public float AtmosphereExponent = 1.5f;

        // Horizon
        [Range(0.0f, 1f)] public float HorizonOffset = 0.01f;

        // Zenith (for Preetham Atmosphere)
        [Range(0.0f, 8400f)] public float RayleighZenithLength = 8400f;
        [Range(0.0f, 1250f)] public float MieZenithLength = 1250f;
        #endregion
    }

    
}
