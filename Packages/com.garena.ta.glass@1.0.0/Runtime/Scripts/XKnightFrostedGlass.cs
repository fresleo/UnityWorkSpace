using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace XKnight.Glass
{
	[ExcludeFromPreset]
	public class XKnightFrostedGlass : ScriptableRendererFeature
	{
		public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
		public bool frustumCulling;
		
		private static bool _sIsActive;
		private static bool _sIsFrustumCullingEnabled;
		public static bool ShouldApplyFrustumCulling() => _sIsActive && _sIsFrustumCullingEnabled;
		
		private static bool _glassInFrustum;
		private GrabScreenBlurRendererPass _pass;

		public static void NotifyGlassInFrustum()
		{
			_glassInFrustum = true;
		}
		
		private int ratio0 = 2;
		private int ratio1 = 2;

		[Header("Blur")]
		[Range(1, 8)] public int Iterations = 2;
		public float BlurAmount = 1.0f;
		
		[Header("Bilateral Filtering")]
		[Tooltip("Enable depth-based bilateral filtering (requires camera depth texture).")]
		public bool bilateralFiltering;

		[Tooltip("Spatial sigma for bilateral weights. Larger = blurrier / less spatial edge preservation.")]
		[Range(0.01f, 8.0f)]
		public float bilateralSigmaSpatial = 0.8f;

		[Tooltip("Depth sigma for bilateral weighting (depth texture space). Smaller = stronger edge preservation.")]
		[Range(0.0001f, 1.0f)]
		public float bilateralSigmaDepth = 0.1f;

		[SerializeField]
		[Reload("Shaders/KawaseBlur.shader")]
		private Shader _kawaseBlurShader;
		private Material _kawaseMat;

		private const string KeywordBilateralDepth = "_BILATERAL_DEPTH_ON";
		private static readonly int SigmaSpatialId = Shader.PropertyToID("_SigmaSpatial");
		private static readonly int SigmaDepthId = Shader.PropertyToID("_SigmaDepth");
		
		private void OnEnable()
		{
			_sIsActive = true;
			_sIsFrustumCullingEnabled = frustumCulling;
			RenderPipelineManager.endFrameRendering += EndFrameRendering;
		}

		private void OnDisable()
		{
			RenderPipelineManager.endFrameRendering -= EndFrameRendering;
			_sIsActive = false;
			_sIsFrustumCullingEnabled = false;
			_glassInFrustum = false;
		}

		private void OnValidate()
		{
			_sIsFrustumCullingEnabled = frustumCulling;
		}

		private static void EndFrameRendering(ScriptableRenderContext context, Camera[] cameras)
		{
			_glassInFrustum = false;
		}

		public override void Create()
		{
#if UNITY_EDITOR
			ResourceReloader.TryReloadAllNullIn(this, "Packages/com.garena.ta.glass");
#endif
			if (_kawaseMat != null)
			{
				CoreUtils.Destroy(_kawaseMat);
				_kawaseMat = null;
			}

			if (_pass != null)
			{
				_pass.Dispose();
				_pass = null;
			}

			_kawaseMat = _kawaseBlurShader != null ? CoreUtils.CreateEngineMaterial(_kawaseBlurShader) : null;

			_pass = new GrabScreenBlurRendererPass()
			{
				renderPassEvent = renderPassEvent
			};
		}

		protected override void Dispose(bool disposing)
		{
			if (_pass != null)
			{
				_pass.Dispose();
				_pass = null;
			}

			if (_kawaseMat != null)
			{
				CoreUtils.Destroy(_kawaseMat);
				_kawaseMat = null;
			}

			base.Dispose(disposing);
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			if (_kawaseMat == null)
			{
				return;
			}

			if (frustumCulling && !_glassInFrustum)
			{
				return;
			}

			bool enableBilateral = bilateralFiltering;
			if (bilateralFiltering)
			{
				_kawaseMat.EnableKeyword(KeywordBilateralDepth);
				_kawaseMat.SetFloat(SigmaSpatialId, bilateralSigmaSpatial);
				_kawaseMat.SetFloat(SigmaDepthId, bilateralSigmaDepth);
			}
			else
			{
				_kawaseMat.DisableKeyword(KeywordBilateralDepth);
			}

			_pass.ConfigureBlur(BlurAmount, ratio0, ratio1, _kawaseMat, Iterations, enableBilateral);
			renderer.EnqueuePass(_pass);
		}
	}

	public class GrabScreenBlurRendererPass : ScriptableRenderPass
	{
		private float _blurAmount;
		private int _blur0Ratio = 2;
		private int _blur1Ratio = 2;

		private Material _layer1Mat;
		private int _iterations = 1;
		private bool _enableBilateralDepth;

		private readonly int _propIdBlurAmount = Shader.PropertyToID("_BlurAmount");

		private static readonly int CameraOpaqueTextureId = Shader.PropertyToID("_CameraOpaqueTexture");
		private static readonly int BluredTexture0Id = Shader.PropertyToID("_BluredTexture0");
		private static readonly int BluredTexture1Id = Shader.PropertyToID("_BluredTexture1");
		
		private RTHandle _layer0Rt;
		private RTHandle _layer1Rt;
		private RTHandle _pingPongRt;

		public GrabScreenBlurRendererPass()
		{
			profilingSampler = new ProfilingSampler(nameof(GrabScreenBlurRendererPass));
		}

		public void ConfigureBlur(
			float blurAmount,
			int blur0Ratio,
			int blur1Ratio,
			Material layer1Material,
			int iterations,
			bool enableBilateralDepth)
		{
			_blurAmount = Mathf.Max(0.0f, blurAmount);

			_blur0Ratio = Mathf.Max(blur0Ratio, 1);
			_blur1Ratio = Mathf.Max(blur1Ratio, 1);

			_layer1Mat = layer1Material;
			_iterations = Mathf.Clamp(iterations, 1, 8);

			_enableBilateralDepth = enableBilateralDepth;
			ConfigureInput(_enableBilateralDepth ? ScriptableRenderPassInput.Depth : ScriptableRenderPassInput.None);
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			var cmd = CommandBufferPool.Get();
			using (new ProfilingScope(cmd, profilingSampler))
			{
				var cameraDesc = renderingData.cameraData.cameraTargetDescriptor;
				
				var cameraOpaqueTex = Shader.GetGlobalTexture(CameraOpaqueTextureId);
				var canUseCameraOpaqueTexture = cameraOpaqueTex != null;

				var desc = cameraDesc;
				desc.msaaSamples = 1;
				desc.depthBufferBits = 0;
				desc.autoGenerateMips = false;
				desc.useMipMap = false;
				
				RTHandle layer0Source;
				bool ownsLayer0Source = false;
				if (canUseCameraOpaqueTexture)
				{
					layer0Source = RTHandles.Alloc(cameraOpaqueTex);
					ownsLayer0Source = true;
					cmd.SetGlobalTexture(BluredTexture0Id, cameraOpaqueTex);
				}
				else
				{
					desc.width = Mathf.Max(1, cameraDesc.width / _blur0Ratio);
					desc.height = Mathf.Max(1, cameraDesc.height / _blur0Ratio);

					ReAllocateRtIfNeeded(ref _layer0Rt, desc, "_dstBlurRT_0");

					Blit(cmd, renderingData.cameraData.renderer.cameraColorTargetHandle, _layer0Rt);
					layer0Source = _layer0Rt;

					cmd.SetGlobalTexture(BluredTexture0Id, _layer0Rt.nameID);
				}
				
				desc.width = Mathf.Max(1, cameraDesc.width / _blur1Ratio);
				desc.height = Mathf.Max(1, cameraDesc.height / _blur1Ratio);
				
				if (_layer1Mat == null || _iterations <= 0 || _blurAmount <= 0.0f)
				{
					cmd.SetGlobalTexture(BluredTexture1Id, layer0Source);
				}
				else
				{
					ReAllocateRtIfNeeded(ref _layer1Rt, desc, "_dstBlurRT_1");

					RTHandle blurResult = ExecuteKawaseIterations(cmd, layer0Source, _layer1Rt, desc, _iterations);

					cmd.SetGlobalTexture(BluredTexture1Id, blurResult);
				}

				if (ownsLayer0Source)
				{
					layer0Source?.Release();
				}
			}

			CoreUtils.SetRenderTarget(cmd,
				renderingData.cameraData.renderer.cameraColorTargetHandle,
				renderingData.cameraData.renderer.cameraDepthTargetHandle);

			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}

		private RTHandle ExecuteKawaseIterations(CommandBuffer cmd, RTHandle source, RTHandle targetRt, RenderTextureDescriptor desc, int iterations)
		{
			if (iterations <= 0)
				return source;

			float baseStepX = _blurAmount / Mathf.Max(1, desc.width);
			float baseStepY = _blurAmount / Mathf.Max(1, desc.height);
			
			if (iterations == 1)
			{
				cmd.SetGlobalVector(_propIdBlurAmount, new Vector4(baseStepX, baseStepY, 0, 0));
				cmd.Blit(source, targetRt, _layer1Mat);
				return targetRt;
			}

			ReAllocateRtIfNeeded(ref _pingPongRt, desc, "_glass_blurPing");

			RTHandle src = source;
			RTHandle dst = targetRt;

			for (int i = 0; i < iterations; i++)
			{
				float k = i + 1;
				cmd.SetGlobalVector(_propIdBlurAmount, new Vector4(baseStepX * k, baseStepY * k, 0, 0));
				cmd.Blit(src, dst, _layer1Mat);

				src = dst;
				dst = (dst == targetRt) ? _pingPongRt : targetRt;
			}

			return src;
		}

		private static void ReAllocateRtIfNeeded(ref RTHandle rt, in RenderTextureDescriptor desc, string rtName)
		{
			XKnightRenderingUtils.ReAllocateIfNeeded(ref rt, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, false, 1, 0f, rtName);
		}

		public void Dispose()
		{
            _layer0Rt?.Release();
            _layer0Rt = null;
            
            _layer1Rt?.Release();
            _layer1Rt = null;
            
            _pingPongRt?.Release();
            _pingPongRt = null;
		}
	}
}

