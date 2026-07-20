using UnityEngine;

namespace RadiantGI.Universal
{
    /// <summary>
    /// 辐照度阴影贴图
    /// </summary>
    [ExecuteInEditMode]
    public class RadiantShadowMap : MonoBehaviour
    {
        private static class ShaderParams
        {
            public static int RadiantShadowMapColors = Shader.PropertyToID("_RadiantShadowMapColors");
            public static int RadiantShadowMapNormals = Shader.PropertyToID("_RadiantShadowMapNormals");
            public static int RadiantShadowMapWorldPos = Shader.PropertyToID("_RadiantShadowMapWorldPos");
            public static int RadiantWorldToShadowMap = Shader.PropertyToID("_RadiantWorldToShadowMap");
            public static int ClipToWorld = Shader.PropertyToID("_ClipToWorld");
            public static int ClipDir = Shader.PropertyToID("_ClipDir");
            public static int FarClipPlane = Shader.PropertyToID("_FarClipPlane");
        }

        public enum EShadowMapResolution
        {
            [InspectorName("64")] _64,
            [InspectorName("128")] _128,
            [InspectorName("256")] _256,
            [InspectorName("512")] _512,
            [InspectorName("1024")] _1024,
            [InspectorName("2048")] _2048
        }

        private const string RADIANT_GO_NAME = "RadiantGI Capture Camera";

        public static bool installed;

        public Transform target;

        [Tooltip("The capture extents around target")]
        public float targetCaptureSize = 25;

        public EShadowMapResolution resolution = EShadowMapResolution._512;
        public Camera captureCamera;
        public RenderTexture rtColors, rtWorldPos, rtNormals;
        
        private Light m_thisLight;
        private Material m_captureMat;
        private Quaternion m_lastRotation;
        private Vector3 m_lastTargetPos;
        private float m_lastCaptureSize;
        private bool m_needShoot;

        private void OnDestroy()
        {
            Remove();
        }
        
        private void OnEnable()
        {
            m_thisLight = GetComponent<Light>();
            if (m_thisLight == null || m_thisLight.type != LightType.Directional)
            {
                Debug.LogError("Radiant Shadow Map script must be added to a directional light!");
                return;
            }

            if (m_captureMat == null)
            {
                m_captureMat = new Material(Shader.Find("Hidden/Kronnect/RadiantGICapture"));
            }

            SetupCamera();
            m_lastTargetPos = new Vector3(float.MaxValue, 0, 0);
            installed = true;
        }

        private void OnValidate()
        {
            targetCaptureSize = Mathf.Max(targetCaptureSize, 5);
        }
        
        private void LateUpdate()
        {
            if (m_thisLight == null)
            {
                Remove();
                return;
            }

            if (target == null)
            {
                target = Camera.main.transform;
                if (target == null) return;
            }

            if (captureCamera == null)
            {
                SetupCamera();
                if (captureCamera == null) return;
            }

            Quaternion rotation = transform.rotation;
            if (m_lastCaptureSize != targetCaptureSize || m_lastRotation != rotation || (m_lastTargetPos - target.position).sqrMagnitude > 25)
            {
                m_needShoot = true;
            }

            int desiredSize = 1 << ((int)resolution + 6);
            if (rtColors == null || rtNormals == null || rtWorldPos == null || rtColors.width != desiredSize)
            {
                DestroyRT(rtColors);
                DestroyRT(rtNormals);
                DestroyRT(rtWorldPos);
                if (rtColors == null)
                {
                    RenderTextureDescriptor rtDesc = new RenderTextureDescriptor(desiredSize, desiredSize, RenderTextureFormat.ARGBHalf, 0);
                    rtDesc.msaaSamples = 1;
                    rtDesc.useMipMap = false;
                    // create rsm color target
                    rtColors = new RenderTexture(rtDesc);
                    rtColors.Create();
                    // create rsm normals target
                    rtNormals = new RenderTexture(rtDesc);
                    rtNormals.Create();
                    // create rsm world pos target
                    rtWorldPos = new RenderTexture(rtDesc);
                    rtWorldPos.Create();
                }

                captureCamera.targetTexture = rtColors;
                m_needShoot = true;
            }

            if (m_needShoot)
            {
                m_needShoot = false;
                CaptureScene();
            }
        }

        
        private void Remove()
        {
            installed = false;
            if (captureCamera != null && RADIANT_GO_NAME.Equals(captureCamera.name))
            {
                DestroyImmediate(captureCamera.gameObject);
            }

            if (m_captureMat != null)
            {
                DestroyImmediate(m_captureMat);
            }

            DestroyRT(rtColors);
            DestroyRT(rtWorldPos);
            DestroyRT(rtNormals);
        }

        private void SetupCamera()
        {
            if (captureCamera == null)
            {
                captureCamera = GetComponentInChildren<Camera>();
            }

            if (captureCamera != null) return;

            GameObject camGO = Instantiate(Resources.Load<GameObject>("RadiantGI/CaptureCamera"));
            camGO.name = RADIANT_GO_NAME;
            camGO.transform.SetParent(transform, false);
            captureCamera = camGO.GetComponent<Camera>();
        }
        
        private void CaptureScene()
        {
            m_lastRotation = transform.rotation;
            m_lastTargetPos = target.position;
            m_lastCaptureSize = targetCaptureSize;
            float farClipPlane = captureCamera.farClipPlane;
            Vector3 targetPosition = target != null ? target.transform.position : Vector3.zero;
            captureCamera.transform.localRotation = Quaternion.identity;
            captureCamera.transform.localPosition = targetPosition + new Vector3(0, 0, farClipPlane * -0.5f);
            captureCamera.orthographicSize = targetCaptureSize;
            captureCamera.Render();

            m_captureMat.SetMatrix(ShaderParams.ClipToWorld, captureCamera.cameraToWorldMatrix * captureCamera.projectionMatrix.inverse);
            m_captureMat.SetVector(ShaderParams.ClipDir, transform.forward);
            m_captureMat.SetFloat(ShaderParams.FarClipPlane, farClipPlane);

            Graphics.Blit(rtColors, rtWorldPos, m_captureMat, 0);

            Shader.SetGlobalTexture(ShaderParams.RadiantShadowMapWorldPos, rtWorldPos);
            Graphics.Blit(rtColors, rtNormals, m_captureMat, 1);

            Shader.SetGlobalTexture(ShaderParams.RadiantShadowMapColors, rtColors);
            Shader.SetGlobalTexture(ShaderParams.RadiantShadowMapNormals, rtNormals);
            Shader.SetGlobalMatrix(ShaderParams.RadiantWorldToShadowMap, captureCamera.projectionMatrix * captureCamera.worldToCameraMatrix);
        }

        private void DestroyRT(RenderTexture rt)
        {
            if (rt == null) return;
            rt.Release();
            DestroyImmediate(rt);
        }
    }
}