namespace UnityEngine.Rendering.Universal
{
    public class SkyControl
    {
        AtmosphericScatteringProfile m_SkyProfile;
        TimeOfDay m_TimeOfDay;
        bool m_IsValid = false;

        public SkyControl()
        {
            m_TimeOfDay = new TimeOfDay();
        } 

        public void Update(AtmosphericScatteringProfile skyProfile, ref LightData lightData)
        {
            m_SkyProfile = skyProfile;
            m_IsValid = m_SkyProfile != null;
            if (!m_IsValid)
            {
                return;
            }

            m_TimeOfDay.Update(m_SkyProfile.timeProfile);

            Light light = lightData.mainLightIndex != -1 ? lightData.visibleLights[lightData.mainLightIndex].light : null;
            UpdateAtmosphere(light);

            UpdateCloudShadow();
            UpdateVolumetricLight();

            RenderSettings.skybox = m_SkyProfile.atmosphereProfile.SkyboxMaterial;
        }

        public void Dispose()
        {
        }

        void UpdateAtmosphere(Light light)
        {
            var profile = m_SkyProfile.atmosphereProfile;
            Material mat = profile.SkyboxMaterial;
            if (mat == null)
            {
                Debug.LogError("没有配置天空盒的材质，导致天空盒无法渲染！");
                return;
            }

            bool tod = m_TimeOfDay.ComputeDirection(out Vector3 SunDirection, out Vector3 MoonDirection);
            // TOD关闭时，太阳方向走场景灯光
            if (!tod && light != null)
            {
                SunDirection = -light.transform.forward;
                MoonDirection = -SunDirection;
            } 

            float SunE = (profile.SunBrightness * 3.0f);

            Vector3 FadeParams;
            float NightIntensity = Saturate(MoonDirection.y + profile.MoonIntensityFactor);
            FadeParams.x = Saturate(SunDirection.y + profile.SunIntensityFactor);
            FadeParams.y = NightIntensity;
            // Combined extinction factor fade(sunset/dawn color)
            FadeParams.z = Saturate(Mathf.Clamp01(1.0f - SunDirection.y));

            float MoonPhasesIntensityMultiplier = Mathf.Clamp01(Vector3.Dot(-SunDirection, MoonDirection) + 0.3f);

            ComputeBeta(out Vector3 BetaRay, out Vector3 BetaMie);

            // 太阳方向作为全局数据，对云有影响
            Shader.SetGlobalVector("_SunDirection", SunDirection);
            mat.SetVector("_MoonDirection", MoonDirection);

            mat.SetFloat("_HorizonOffset", profile.HorizonOffset);

            // Sun Mie
            mat.SetVector("_SunBetaMiePhase", BetaMiePhase(profile.SunMieAnisotropy, true));
            mat.SetFloat("_SunMieScattering", profile.SunMieScattering);
            mat.SetColor("_SunMieColor", profile.SunMieColor);

            // Moon Mie
            mat.SetVector("_MoonBetaMiePhase", BetaMiePhase(profile.MoonMieAnisotropy, false));
            mat.SetFloat("_MoonMieScattering", profile.MoonMieScattering * MoonPhasesIntensityMultiplier);
            mat.SetColor("_MoonMieColor", profile.MoonMieColor);

            mat.SetFloat("_SunE", SunE);
            mat.SetColor("_SunAtmosphereTint", profile.SunAtmosphereTint);
            mat.SetColor("_MoonAtmosphereTint", profile.MoonAtmosphereTint * profile.MoonBrightness * MoonPhasesIntensityMultiplier);

            mat.SetVector("_FadeParams", FadeParams);
            mat.SetFloat("_AtmosphereExponent", profile.AtmosphereExponent);

            mat.SetVector("_BetaRay", BetaRay);
            mat.SetVector("_BetaMie", BetaMie);
            mat.SetFloat("_RayleighZenithLength", profile.RayleighZenithLength);
            mat.SetFloat("_MieZenithLength", profile.MieZenithLength);
          
            bool SunLightEnable = SunDirection.y > 0f;
            CoreUtils.SetKeyword(mat, "_NIGHT", MoonDirection.y > 0f);
            // Light
            if (tod && light != null)
            {
                var timeProfile = m_SkyProfile.timeProfile;
                if (SunLightEnable)
                {
                    float EvaluateTimeBySunAboveHorizon = (1.0f - SunDirection.y);
                    light.transform.forward = -SunDirection;
                    light.color = timeProfile.SunLightColor;
                    light.intensity = timeProfile.SunLightIntensity.Evaluate(EvaluateTimeBySunAboveHorizon);
                }
                else
                {
                    float EvaluateTimeByMoonAboveHorizon = (1.0f - MoonDirection.y);
                    light.transform.forward = -MoonDirection;
                    light.color = timeProfile.MoonLightColor;
                    light.intensity = timeProfile.MoonLightIntensity.Evaluate(EvaluateTimeByMoonAboveHorizon) * MoonPhasesIntensityMultiplier;
                }
            }      
        }

        void ComputeBeta(out Vector3 BetaRay, out Vector3 BetaMie)
        {
            var profile = m_SkyProfile.atmosphereProfile;

            // Wavelengths.
            Vector3 lambda = 1e-9f * new Vector3(
                profile.WavelengthR,
                profile.WavelengthG,
                profile.WavelengthB);
            Vector3 wavelength = new Vector3(
                Mathf.Pow(lambda.x, 4.0f),
                Mathf.Pow(lambda.y, 4.0f),
                Mathf.Pow(lambda.z, 4.0f));

            // constant factors.
            const float n = 1.0003f;   // Index of air refraction(n);
            const float N = 2.545e25f; // Molecular density(N)
            const float pn = 0.035f;    // Depolatization factor for standart air.
            const float n2 = n * n;     // Molecular density exponentially squared.

            // Beta Rayleigh
            float ray = (8.0f * Mathf.Pow(Mathf.PI, 3.0f) * Mathf.Pow(n2 - 1.0f, 2.0f) * (6.0f + 3.0f * pn));
            Vector3 theta = 3.0f * N * wavelength * (6.0f - 7.0f * pn);

            BetaRay = new Vector3(
                (ray / theta.x) * profile.AtmosphereThickness * 0.5f,
                (ray / theta.y) * profile.AtmosphereThickness * 0.5f,
                (ray / theta.z) * profile.AtmosphereThickness * 0.5f);

            // Beta Mie.
            Vector3 k = new Vector3(0.685f, 0.679f, 0.670f);
            float c = (0.2f * profile.Turbidity) * 10e-18f;
            float mieFactor = 0.434f * c * Mathf.PI;
            const float v = 4.0f;
            float mie = (profile.Mie * 1e+1f); // Adjust.

            BetaMie = new Vector3(
                (mieFactor * Mathf.Pow((2.0f * Mathf.PI) / lambda.x, v - 2.0f) * k.x) * mie,
                (mieFactor * Mathf.Pow((2.0f * Mathf.PI) / lambda.y, v - 2.0f) * k.y) * mie,
                (mieFactor * Mathf.Pow((2.0f * Mathf.PI) / lambda.z, v - 2.0f) * k.z) * mie);
        }

        float Saturate(float x)
        {
            return Mathf.Max(0.0f, Mathf.Min(1.0f, x));
        }

        Vector3 BetaMiePhase(float g, bool HQ)
        {
            Vector3 result;
            {
                float g2 = g * g;
                result.x = HQ ? (1.0f - g2) / (2.0f + g2) : 1.0f - g2;
                result.y = 1.0f + g2;
                result.z = 2.0f * g;
            }
            return result;
        }

        public void UpdateCloudShadow()
        {
            var cloudProfile = m_SkyProfile.cloudProfile;

            Shader.SetGlobalTexture("_CloudShadowTex", cloudProfile.CloudShadowTex);
            Shader.SetGlobalFloat("_CloudShadowTiling", cloudProfile.CloudShadowTiling);
            Shader.SetGlobalVector("_CloudShadowSpeed", cloudProfile.CloudShadowSpeed);
            Shader.SetGlobalColor("_CloudShadowColor", cloudProfile.CloudShadowColor);
        }

        public void UpdateVolumetricLight()
        {
            var volumetricLightProfile = m_SkyProfile.volumetricLightProfile;

            Shader.SetGlobalTexture("_DitheringTex", volumetricLightProfile.DitheringTex);
            Shader.SetGlobalFloat("_VolumetricLightRange", 1f - volumetricLightProfile.VolumetricLightRange);
            Shader.SetGlobalColor("_VolumetricLightColor", volumetricLightProfile.VolumetricLightColor);         
        }

        const float CLOUD_SCALE = 0.5f;
        const float LAYER_FAR_PLANE_SCALE = 0.8f;
        public void DrawClouds(Camera cam, CommandBuffer cmdList)
        {
            if (!m_IsValid)
            {
                return;
            }

            var cloudProfile = m_SkyProfile.cloudProfile;

            Shader.SetGlobalFloat("_CloudFrontAndBackBlendFactor", cloudProfile.CloudFrontAndBackBlendFactor);
            Shader.SetGlobalColor("_CloudDarkBackColor", cloudProfile.CloudDarkBackColor);
            Shader.SetGlobalColor("_CloudDarkFrontColor", cloudProfile.CloudDarkFrontColor);
            Shader.SetGlobalColor("_CloudLightBackColor", cloudProfile.CloudLightBackColor);
            Shader.SetGlobalColor("_CloudLightFrontColor", cloudProfile.CloudLightFrontColor);
            Shader.SetGlobalFloat("_CloudSunBrightenIntensity", cloudProfile.CloudSunBrightenIntensity);

            Mesh[] meshes = cloudProfile.CloudMeshes;
            Material[] mats = cloudProfile.CloudMaterials;
            if (meshes != null || mats != null || meshes.Length == mats.Length)
            {
                for (int i = 0, imax = meshes.Length; i < imax; ++i)
                {
                    Mesh mesh = meshes[i];
                    Material mat = mats[i];

                    mat.SetFloat("_CloudCurlSpeed", cloudProfile.CloudCurlSpeed);
                    mat.SetFloat("_CloudCurlTiling", cloudProfile.CloudCurlTiling);
                    mat.SetFloat("_CloudCurlAmplitude", cloudProfile.CloudCurlAmplitude);
                    mat.SetFloat("_CloudTransparency", cloudProfile.CloudTransparency);
                    mat.SetFloat("_CloudCoverage", cloudProfile.CloudCoverage);
                    mat.SetFloat("_CloudVolumeChangeSpeed", cloudProfile.CloudVolumeChangeSpeed);

                    Matrix4x4 cloudMatrix = Matrix4x4.TRS(Vector3.zero,
                        Quaternion.identity,
                        Vector3.one * CLOUD_SCALE);
                    cmdList.DrawMesh(mesh, cloudMatrix, mat);
                }
            }

            Mesh layerMesh = cloudProfile.CloudLayerMesh;
            Material layerMat = cloudProfile.CloudLayerMaterial;
            if (layerMesh != null && layerMat != null)
            {
                //layerMat.SetVector("_CloudDirection", cloudProfile.CloudDirection);
                //layerMat.SetFloat("_CloudHeight", cloudProfile.CloudHeight);
                layerMat.SetFloat("_CloudWispsSpeed", cloudProfile.CloudWispsSpeed);
                layerMat.SetFloat("_CloudWispsCoverage", cloudProfile.CloudWispsCoverage);
                layerMat.SetFloat("_CloudWispsOpacity", cloudProfile.CloudWispsOpacity);

                Matrix4x4 cloudMatrix = Matrix4x4.TRS(Vector3.zero,
                    Quaternion.identity,
                    Vector3.one * LAYER_FAR_PLANE_SCALE * cam.farClipPlane);
                cmdList.DrawMesh(layerMesh, cloudMatrix, layerMat);
            }
        }
    }
}