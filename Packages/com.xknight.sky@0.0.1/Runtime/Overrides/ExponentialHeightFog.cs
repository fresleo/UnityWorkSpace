namespace UnityEngine.Rendering.Universal
{
    public class ExponentialHeightFog : VolumeComponent
    {
        public ClampedFloatParameter fogDensity = new ClampedFloatParameter(0f, 0f, 0.05f);
        public ClampedFloatParameter fogHeightFalloff = new ClampedFloatParameter(0.02f, -0.2f, 0.2f);
        public FloatParameter fogHeight = new FloatParameter(0f);
        public ClampedFloatParameter fogDensity2 = new ClampedFloatParameter(0f, 0f, 0.05f);
        public ClampedFloatParameter fogHeightFalloff2 = new ClampedFloatParameter(0.02f, -0.2f, 0.2f);
        public FloatParameter fogHeight2 = new FloatParameter(0f);

        public ColorParameter fogInscatteringColor = new ColorParameter(new Color(0.447f, 0.638f, 1.0f), true, false, false);
        public ClampedFloatParameter fogMaxOpacity = new ClampedFloatParameter(1f, 0f, 1f);
        public MinFloatParameter startDistance = new MinFloatParameter(0f, 0f);
        public MinFloatParameter fogCutoffDistance = new MinFloatParameter(0f, 0f);
        
        public Vector3Parameter directionalInscatteringDir= new Vector3Parameter(new Vector3(50f, -30f, 0f));
        public ClampedFloatParameter directionalInscatteringExponent = new ClampedFloatParameter(4f, 2f, 64f);
        public FloatParameter directionalInscatteringStartDistance = new FloatParameter(0f);
        public ColorParameter directionalInscatteringColor = new ColorParameter(new Color(0.25f, 0.25f, 0.125f), true, false, false);

        public static readonly int s_ExponentialFogParametersID = Shader.PropertyToID("ExponentialFogParameters");
        public static readonly int s_ExponentialFogParameters2ID = Shader.PropertyToID("ExponentialFogParameters2");
        public static readonly int s_ExponentialFogParameters3ID = Shader.PropertyToID("ExponentialFogParameters3");
        public static readonly int s_DirectionalInscatteringColorID = Shader.PropertyToID("DirectionalInscatteringColor");
        public static readonly int s_InscatteringLightDirectionID = Shader.PropertyToID("InscatteringLightDirection");
        public static readonly int s_ExponentialFogColorParameterID = Shader.PropertyToID("ExponentialFogColorParameter");

        public bool IsActive() => fogDensity.value > 0f || fogDensity2.value > 0f;

        private static float RayOriginTerm(Transform camTf, float density, float heightFalloff, float heightOffset)
        {
            float exponent = heightFalloff * (camTf.position.y - heightOffset);
            return density * Mathf.Pow(2.0f, -exponent);
        }

        public void PushFogParams(CommandBuffer cmd, ref RenderingData renderingData)
        {
            Transform camTf = renderingData.cameraData.camera.transform;

            const float USELESS_VALUE = 0.0f;

            var ExponentialFogParameters = new Vector4(
                RayOriginTerm(camTf, fogDensity.value, fogHeightFalloff.value, fogHeight.value), 
                fogHeightFalloff.value, USELESS_VALUE, startDistance.value);
            var ExponentialFogParameters2 = new Vector4(
                RayOriginTerm(camTf, fogDensity2.value, fogHeightFalloff2.value, fogHeight2.value), 
                fogHeightFalloff2.value, fogDensity2.value, fogHeight2.value);
            var ExponentialFogParameters3 = new Vector4(fogDensity.value, fogHeight.value, USELESS_VALUE, fogCutoffDistance.value);

            var DirectionalInscatteringColor = new Vector4(
                directionalInscatteringColor.value.r,
                directionalInscatteringColor.value.g,
                directionalInscatteringColor.value.b,
                directionalInscatteringExponent.value
            );

            Vector3 lightForward = -(Quaternion.Euler(directionalInscatteringDir.value) * Vector3.forward);
            if (!directionalInscatteringDir.overrideState)
            {
                LightData lightData = renderingData.lightData;
                Light light = lightData.mainLightIndex != -1 ? lightData.visibleLights[lightData.mainLightIndex].light : null;
                if (light != null)
                {
                    lightForward = -light.transform.forward;
                }
            }
            var InscatteringLightDirection = new Vector4(
                lightForward.x, lightForward.y, lightForward.z,
                directionalInscatteringStartDistance.value
            );
            
            var ExponentialFogColorParameter = new Vector4(
                fogInscatteringColor.value.r,
                fogInscatteringColor.value.g,
                fogInscatteringColor.value.b,
                1.0f - fogMaxOpacity.value
            );

            cmd.SetGlobalVector(s_ExponentialFogParametersID, ExponentialFogParameters);
            cmd.SetGlobalVector(s_ExponentialFogParameters2ID, ExponentialFogParameters2);
            cmd.SetGlobalVector(s_ExponentialFogParameters3ID, ExponentialFogParameters3);
            cmd.SetGlobalVector(s_DirectionalInscatteringColorID, DirectionalInscatteringColor);
            cmd.SetGlobalVector(s_InscatteringLightDirectionID, InscatteringLightDirection);
            cmd.SetGlobalVector(s_ExponentialFogColorParameterID, ExponentialFogColorParameter);
        }
    }
}
