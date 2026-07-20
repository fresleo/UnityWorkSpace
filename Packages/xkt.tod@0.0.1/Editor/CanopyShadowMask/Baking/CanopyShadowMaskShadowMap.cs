/*******************************************************************************
 * File: CanopyShadowMaskShadowMap.cs
 * Author: WangYu
 * Date: 2026-07-03
 * Description: 
 *******************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace XKT.TOD.CanopyShadowMask
{
    /// <summary>
    /// 将标记的树冠代理网格渲染到临时的 light-space shadow map 中。
    /// </summary>
    public sealed class CanopyShadowMaskShadowMap : IDisposable
    {
        private const string C_SHADER_NAME = "Hidden/XKnight/CanopyShadowMask/AlphaDepth";
        private const string C_SHADER_PATH =
            "Packages/xkt.tod/Editor/CanopyShadowMask/Baking/CanopyShadowMaskAlphaDepth.shader";
        
        private const string C_BASE_MAP = "_BaseMap";
        private const string C_MAIN_TEX = "_MainTex";
        private const string C_CUTOFF = "_Cutoff";
        private const string C_ALPHA_TEST_THRESHOLD = "_AlphaTestThreshold";
        
        private const string C_WORLD_TO_CAMERA = "_CanopyWorldToCamera";
        private const string C_FAR_CLIP = "_CanopyFarClip";
        
        private const int C_SHADOW_MAP_SIZE = 2048; // 尺寸
        private const float C_RELEVANT_CANOPY_PADDING = 4; // 填充

        private readonly Camera _camera;
        private readonly Texture2D _depthTexture;
        private readonly List<Material> _materials;
        private readonly float _farClip;
        
        /// <summary>
        /// 被绘制到临时 shadow map 中的渲染器数量。
        /// </summary>
        public int DrawnRendererCount { get; private set; }

        /// <summary>
        /// 临时 shadow map 的分辨率。
        /// </summary>
        public int Resolution => _depthTexture != null ? _depthTexture.width : 0;
        
        /// <summary>
        /// 释放临时摄像机，材质，和 CPU 纹理。
        /// </summary>
        public void Dispose()
        {
            DestroyMaterials(_materials);
            if (_depthTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(_depthTexture);
            }
            DestroyCamera(_camera);
        }
        
        // 销毁材质球
        private static void DestroyMaterials(List<Material> materials)
        {
            if (materials == null)
            {
                return;
            }

            for (int i = 0; i < materials.Count; i++)
            {
                var itemMaterial = materials[i];
                
                if (itemMaterial != null)
                {
                    UnityEngine.Object.DestroyImmediate(itemMaterial);
                }
            }
            materials.Clear();
        }

        // 销毁摄像机
        private static void DestroyCamera(Camera camera)
        {
            if (camera != null && camera.gameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(camera.gameObject);
            }
        }
        
        // 构造方法
        private CanopyShadowMaskShadowMap(
            Camera camera,
            Texture2D depthTexture,
            List<Material> materials,
            float farClip,
            int drawnRendererCount)
        {
            _camera = camera;
            _depthTexture = depthTexture;
            _materials = materials;
            _farClip = farClip;
            DrawnRendererCount = drawnRendererCount;
        }
        
        /// <summary>
        /// 判断当前 lightmap 接收器范围里是否存在可能投影到它的树冠代理。
        /// </summary>
        public static bool HasRelevantCanopyRenderers(
            IReadOnlyList<MeshRenderer> canopyRenderers,
            IReadOnlyList<MeshRenderer> receiverRenderers,
            Light mainLight)
        {
            List<MeshRenderer> relevantCanopyRenderers = CollectRelevantCanopyRenderers(canopyRenderers, receiverRenderers, mainLight);
            return relevantCanopyRenderers.Count > 0;
        }
        
        /// <summary>
        /// 从标记的树冠代理渲染器构建一个 light-space shadow map。
        /// </summary>
        public static CanopyShadowMaskShadowMap Render(
            IReadOnlyList<MeshRenderer> canopyRenderers,
            IReadOnlyList<MeshRenderer> receiverRenderers,
            Light mainLight)
        {
            if (canopyRenderers == null || canopyRenderers.Count == 0 || mainLight == null)
            {
                return null;
            }

            Shader shader = Shader.Find(C_SHADER_NAME);
            if (shader == null)
            {
                shader = AssetDatabase.LoadAssetAtPath<Shader>(C_SHADER_PATH);
            }

            if (shader == null)
            {
                Debug.LogError($"缺少树冠 shadow map 着色器: {C_SHADER_NAME}");
                return null;
            }

            List<MeshRenderer> relevantCanopyRenderers = CollectRelevantCanopyRenderers(canopyRenderers, receiverRenderers, mainLight);
            if (relevantCanopyRenderers.Count == 0)
            {
                return null;
            }

            Bounds bounds;
            if (!TryBuildRenderBounds(relevantCanopyRenderers, receiverRenderers, out bounds))
            {
                return null;
            }

            float farClip;
            Camera camera = CreateLightCamera(mainLight, bounds, out farClip);
            
            RenderTexture rt = RenderTexture.GetTemporary(
                C_SHADOW_MAP_SIZE, C_SHADOW_MAP_SIZE, 24,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            rt.name = "Canopy ShadowMask ShadowMap";
            rt.wrapMode = TextureWrapMode.Clamp;
            rt.filterMode = FilterMode.Point;

            List<Material> materials = new List<Material>();
            int drawnRenderers = 0;
            RenderTexture previous = RenderTexture.active;
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "Canopy ShadowMask ShadowMap";

            try
            {
                cmd.SetRenderTarget(rt);
                cmd.ClearRenderTarget(true, true, new Color(1, 0, 0, 1));
                cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);

                for (int i = 0; i < relevantCanopyRenderers.Count; i++)
                {
                    MeshRenderer renderer = relevantCanopyRenderers[i];
                    if (renderer == null)
                    {
                        continue;
                    }

                    MeshFilter filter = renderer.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null)
                    {
                        continue;
                    }

                    DrawRenderer(cmd, shader, materials, renderer, filter, camera, farClip);
                    drawnRenderers++;
                }

                Graphics.ExecuteCommandBuffer(cmd);
                RenderTexture.active = rt;
                Texture2D depthTexture = new Texture2D(
                    C_SHADOW_MAP_SIZE, C_SHADOW_MAP_SIZE,
                    TextureFormat.RGBAFloat, false, true);
                depthTexture.ReadPixels(new Rect(0, 0, C_SHADOW_MAP_SIZE, C_SHADOW_MAP_SIZE), 0, 0);
                depthTexture.Apply(false, false);

                return new CanopyShadowMaskShadowMap(
                    camera, depthTexture,
                    materials,
                    farClip,
                    drawnRenderers);
            }
            catch (Exception ex)
            {
                Debug.LogError($"树冠 shadow map 渲染失败: \n{ex.Message}");
                
                DestroyMaterials(materials);
                DestroyCamera(camera);
                return null;
            }
            finally
            {
                RenderTexture.active = previous;
                cmd.Release();
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        /// <summary>
        /// 当世界点被标记的树冠遮挡时返回 0，否则返回 1。
        /// </summary>
        public float SampleOcclusion(Vector3 worldPos)
        {
            if (_camera == null || _depthTexture == null)
            {
                return 1;
            }

            Vector3 viewport = _camera.WorldToViewportPoint(worldPos);
            if (viewport.x < 0 || viewport.x > 1 || viewport.y < 0 || viewport.y > 1)
            {
                return 1;
            }

            if (viewport.z <= 0 || viewport.z >= _farClip)
            {
                return 1;
            }

            int width = Mathf.FloorToInt(viewport.x * _depthTexture.width);
            int x = Mathf.Clamp(width, 0, _depthTexture.width - 1);

            int height = Mathf.FloorToInt(viewport.y * _depthTexture.height);
            int y = Mathf.Clamp(height, 0, _depthTexture.height - 1);
            
            Color shadow = _depthTexture.GetPixel(x, y);
            if (shadow.g < 0.5f)
            {
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// 写出一个可读的灯光空间树冠投影调试图像。
        /// </summary>
        public string WriteDebugTexture(string referenceAssetPath)
        {
            if (_depthTexture == null || string.IsNullOrEmpty(referenceAssetPath))
            {
                return string.Empty;
            }

            int dt_width = _depthTexture.width;
            int dt_height = _depthTexture.height;
            
            Texture2D debug = new Texture2D(dt_width, dt_height, TextureFormat.RGBA32, false, true);
            Color32[] pixels = new Color32[dt_width * dt_height];
            for (int y = 0; y < dt_height; y++)
            {
                for (int x = 0; x < dt_width; x++)
                {
                    Color source = _depthTexture.GetPixel(x, y);

                    int index = y * dt_width + x;
                    byte value = source.g > 0.5f ? (byte)255 : (byte)0;
                    
                    pixels[index] = new Color32(value, value, value, 255);
                }
            }

            debug.SetPixels32(pixels);
            debug.Apply(false, false);
            
            string debugPath = GetShadowMapDebugAssetPath(referenceAssetPath);
            File.WriteAllBytes(debugPath, debug.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(debug);
            AssetDatabase.ImportAsset(debugPath, ImportAssetOptions.ForceUpdate);
            
            return debugPath;
        }

        // 绘制渲染器
        private static void DrawRenderer(
            CommandBuffer cmd,
            Shader shader, List<Material> materials,
            MeshRenderer renderer, MeshFilter filter,
            Camera camera, float farClip)
        {
            Mesh mesh = filter.sharedMesh;
            Material[] sourceMaterials = renderer.sharedMaterials;
            int subMeshCount = Mathf.Max(1, mesh.subMeshCount);
            for (int subMesh = 0; subMesh < subMeshCount; subMesh++)
            {
                Material sourceMaterial = ResolveSourceMaterial(sourceMaterials, subMesh);
                Material bakeMaterial = CreateBakeMaterial(shader, sourceMaterial, camera, farClip);
                materials.Add(bakeMaterial);
                
                cmd.DrawMesh(mesh, filter.transform.localToWorldMatrix, bakeMaterial, subMesh, 0);
            }
        }

        // 解析资源材质
        private static Material ResolveSourceMaterial(Material[] materials, int subMesh)
        {
            if (materials == null || materials.Length == 0)
            {
                return null;
            }

            int index = Mathf.Clamp(subMesh, 0, materials.Length - 1);
            Material sourceMaterial = materials[index];

            return sourceMaterial;
        }

        // 创建 bake 材质
        private static Material CreateBakeMaterial(
            Shader shader, Material source,
            Camera camera, float farClip)
        {
            Material material = CoreUtils.CreateEngineMaterial(shader);
            
            material.SetMatrix(C_WORLD_TO_CAMERA, camera.worldToCameraMatrix);
            material.SetFloat(C_FAR_CLIP, farClip);

            Texture texture = ResolveBaseTexture(source);
            if (texture != null)
            {
                material.SetTexture(C_BASE_MAP, texture);
                material.SetTextureScale(C_BASE_MAP, ResolveTextureScale(source));
                material.SetTextureOffset(C_BASE_MAP, ResolveTextureOffset(source));
            }
            else
            {
                material.SetTexture(C_BASE_MAP, Texture2D.whiteTexture);
            }

            float cutoff = ResolveCutoff(source);
            material.SetFloat(C_CUTOFF, cutoff);
            material.SetFloat(C_ALPHA_TEST_THRESHOLD, cutoff);
            
            return material;
        }

        // 解析基础纹理
        private static Texture ResolveBaseTexture(Material source)
        {
            if (source == null)
            {
                return null;
            }

            if (source.HasProperty(C_BASE_MAP))
            {
                Texture texture = source.GetTexture(C_BASE_MAP);
                if (texture != null)
                {
                    return texture;
                }
            }

            if (source.HasProperty(C_MAIN_TEX))
            {
                return source.GetTexture(C_MAIN_TEX);
            }

            return null;
        }

        // 解析纹理比例
        private static Vector2 ResolveTextureScale(Material source)
        {
            if (source == null)
            {
                return Vector2.one;
            }

            if (source.HasProperty(C_BASE_MAP))
            {
                return source.GetTextureScale(C_BASE_MAP);
            }

            if (source.HasProperty(C_MAIN_TEX))
            {
                return source.GetTextureScale(C_MAIN_TEX);
            }

            return Vector2.one;
        }

        // 解析纹理偏移
        private static Vector2 ResolveTextureOffset(Material source)
        {
            if (source == null)
            {
                return Vector2.zero;
            }

            if (source.HasProperty(C_BASE_MAP))
            {
                return source.GetTextureOffset(C_BASE_MAP);
            }

            if (source.HasProperty(C_MAIN_TEX))
            {
                return source.GetTextureOffset(C_MAIN_TEX);
            }

            return Vector2.zero;
        }

        // 解析透明裁切
        private static float ResolveCutoff(Material source)
        {
            if (source == null)
            {
                return 0.5f;
            }

            if (source.HasProperty(C_CUTOFF))
            {
                return source.GetFloat(C_CUTOFF);
            }

            if (source.HasProperty(C_ALPHA_TEST_THRESHOLD))
            {
                return source.GetFloat(C_ALPHA_TEST_THRESHOLD);
            }

            return 0.5f;
        }

        // 根据当前 lightmap 接收器在灯光空间的投影范围筛选相关树冠。
        private static List<MeshRenderer> CollectRelevantCanopyRenderers(
            IReadOnlyList<MeshRenderer> canopyRenderers,
            IReadOnlyList<MeshRenderer> receiverRenderers,
            Light mainLight)
        {
            var result = new List<MeshRenderer>();
            if (canopyRenderers == null || receiverRenderers == null || mainLight == null)
            {
                return result;
            }

            Matrix4x4 worldToLight = BuildWorldToLightMatrix(mainLight);
            Rect receiverRect;
            if (!TryBuildLightSpaceRect(
                    receiverRenderers, worldToLight, C_RELEVANT_CANOPY_PADDING
                    , out receiverRect))
            {
                return result;
            }

            for (int i = 0; i < canopyRenderers.Count; i++)
            {
                MeshRenderer renderer = canopyRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    continue;
                }

                Rect canopyRect;
                if (!TryBuildLightSpaceRect(
                        renderer, worldToLight, C_RELEVANT_CANOPY_PADDING
                        , out canopyRect))
                {
                    continue;
                }

                if (receiverRect.Overlaps(canopyRect))
                {
                    result.Add(renderer);
                }
            }

            return result;
        }

        // 从主光解析灯光空间的 forward 和 up，避免接近世界 Y 轴时的万向锁。
        private static void ResolveLightSpaceForwardAndUp(
            Light mainLight
            , out Vector3 forward, out Vector3 up)
        {
            forward = mainLight.transform.forward.normalized;
            up = Vector3.up;
            // 防万向锁/数值不稳定处理：
            // 如果主光源的朝向几乎平行于世界 Y 轴（通过点积绝对值 > 0.95 判断，即夹角小于约 18° 或大于 162°），
            // 则把上方向改为 Vector3.forward（世界 Z 轴 (0,0,1)）。
            if (Mathf.Abs(Vector3.Dot(forward, up)) > 0.95f)
            {
                up = Vector3.forward;
            }
        }

        // 构造只用于 XY 投影判断的灯光空间矩阵。
        private static Matrix4x4 BuildWorldToLightMatrix(Light mainLight)
        {
            Vector3 forward, up;
            ResolveLightSpaceForwardAndUp(mainLight, out forward, out up);

            Quaternion rotation = Quaternion.LookRotation(forward, up);
            Matrix4x4 worldToLight = Matrix4x4.Rotate(Quaternion.Inverse(rotation));
            return worldToLight;
        }

        // 尝试构建一组渲染器在灯光空间的 XY 包围矩形。
        private static bool TryBuildLightSpaceRect(
            IReadOnlyList<MeshRenderer> renderers, Matrix4x4 worldToLight, float padding
            , out Rect rect)
        {
            rect = new Rect();
            if (renderers == null)
            {
                return false;
            }

            bool hasRect = false;
            for (int i = 0; i < renderers.Count; i++)
            {
                MeshRenderer renderer = renderers[i];
                Rect rendererRect;
                if (!TryBuildLightSpaceRect(renderer, worldToLight, padding, out rendererRect))
                {
                    continue;
                }

                if (!hasRect)
                {
                    rect = rendererRect;
                    hasRect = true;
                }
                else
                {
                    rect = EncapsulateRect(rect, rendererRect);
                }
            }

            return hasRect;
        }

        // 尝试构建单个渲染器在灯光空间的 XY 包围矩形。
        private static bool TryBuildLightSpaceRect(
            MeshRenderer renderer, Matrix4x4 worldToLight, float padding
            , out Rect rect)
        {
            rect = new Rect();
            if (renderer == null)
            {
                return false;
            }

            Bounds bounds = renderer.bounds;
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            for (int ix = -1; ix <= 1; ix += 2)
            {
                for (int iy = -1; iy <= 1; iy += 2)
                {
                    for (int iz = -1; iz <= 1; iz += 2)
                    {
                        Vector3 corner = center + new Vector3(extents.x * ix, extents.y * iy, extents.z * iz);
                        
                        Vector3 lightPos = worldToLight.MultiplyPoint3x4(corner);
                        minX = Mathf.Min(minX, lightPos.x);
                        minY = Mathf.Min(minY, lightPos.y);
                        maxX = Mathf.Max(maxX, lightPos.x);
                        maxY = Mathf.Max(maxY, lightPos.y);
                    }
                }
            }

            rect = Rect.MinMaxRect(
                minX - padding,
                minY - padding,
                maxX + padding,
                maxY + padding);
            return true;
        }

        // 合并灯光空间矩形。
        private static Rect EncapsulateRect(Rect a, Rect b)
        {
            float minX = Mathf.Min(a.xMin, b.xMin);
            float minY = Mathf.Min(a.yMin, b.yMin);
            float maxX = Mathf.Max(a.xMax, b.xMax);
            float maxY = Mathf.Max(a.yMax, b.yMax);
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }
        
        // 创建 light-space 摄像机
        private static Camera CreateLightCamera(Light mainLight, Bounds bounds, out float farClip)
        {
            GameObject cameraObject = EditorUtility.CreateGameObjectWithHideFlags(
                "Canopy ShadowMask Light Camera",
                HideFlags.HideAndDontSave, typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            
            Vector3 forward, up;
            ResolveLightSpaceForwardAndUp(mainLight, out forward, out up);
            
            float radius = Mathf.Max(1, bounds.extents.magnitude + 2);
            farClip = radius * 2 + 4;
            
            camera.transform.position = bounds.center - forward * (radius + 2);
            camera.transform.rotation = Quaternion.LookRotation(forward, up);
            camera.orthographic = true;
            camera.orthographicSize = radius;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = farClip;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(1, 0, 0, 1);
            camera.enabled = false;
            
            return camera;
        }

        // 尝试构建渲染边界
        private static bool TryBuildRenderBounds(
            IReadOnlyList<MeshRenderer> canopyRenderers,
            IReadOnlyList<MeshRenderer> receiverRenderers,
            out Bounds bounds)
        {
            bounds = new Bounds(Vector3.zero, Vector3.zero);
            
            bool hasBounds = false;
            EncapsulateRendererBounds(canopyRenderers, ref bounds, ref hasBounds);
            EncapsulateRendererBounds(receiverRenderers, ref bounds, ref hasBounds);
            
            return hasBounds;
        }

        // 封装渲染器边界
        private static void EncapsulateRendererBounds(
            IReadOnlyList<MeshRenderer> renderers,
            ref Bounds bounds, ref bool hasBounds)
        {
            if (renderers == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Count; i++)
            {
                MeshRenderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    continue;
                }

                Bounds rendererBounds = renderer.bounds;
                if (!hasBounds)
                {
                    bounds = rendererBounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(rendererBounds);
                }
            }
        }

        /// <summary>
        /// 获取 shadow map debug 纹理的资产路径
        /// </summary>
        /// <param name="referenceAssetPath">树冠 shadowmask 资源路径</param>
        /// <returns>debug 纹理资产路径</returns>
        public static string GetShadowMapDebugAssetPath(string referenceAssetPath)
        {
            string dir = Path.GetDirectoryName(referenceAssetPath);
            if (!string.IsNullOrEmpty(dir))
            {
                dir = dir.Replace('\\', '/');
            }

            string assetFileName = Path.GetFileNameWithoutExtension(referenceAssetPath);
            string debugFileName = $"{assetFileName}_shadowmap_debug.png";
            
            if (string.IsNullOrEmpty(dir))
            {
                return debugFileName;
            }

            string debugPath = $"{dir}/{debugFileName}";
            return debugPath;
        }
        
    }
}
