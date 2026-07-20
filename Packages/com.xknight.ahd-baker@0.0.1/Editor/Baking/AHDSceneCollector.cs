/*******************************************************************************
 * File: AHDSceneCollector.cs
 * Author: WangYu
 * Date: 2026-05-13
 * Description: AHD 烘焙场景数据收集。
 * Notice: 仅用于 Unity Editor 烘焙流程。
 *******************************************************************************/

using UnityEditor;
using Unity.Mathematics;
using UnityEngine;

namespace XKnight.AHDBaker.Editor
{
    internal static class AHDSceneCollector
    {
        public const string c_IgnoredLightTag = "NoAHDBakedSpecular";

        // Unity lightmapIndex 从 0 开始，小于该值表示 renderer 未绑定 lightmap。
        private const int C_MIN_VALID_LIGHTMAP_INDEX = 0;

        // Unity Mesh.triangles 每三个索引构成一个三角形。
        private const int C_TRIANGLE_INDEX_STEP = 3;

        // 三角形第二个顶点索引相对起始索引的偏移。
        private const int C_TRIANGLE_INDEX_1_OFFSET = 1;

        // 三角形第三个顶点索引相对起始索引的偏移。
        private const int C_TRIANGLE_INDEX_2_OFFSET = 2;

        // chartId 从 1 开始，避免和默认未初始化值 0 混淆。
        private const int C_INITIAL_CHART_ID = 1;

        // 向量长度平方低于该值时视为退化，避免归一化不稳定。
        private const float C_VECTOR_LENGTH_SQ_EPSILON = 0.000001f;

        // UV 三角形面积低于该值时视为退化，不参与 receiver 写入。
        private const float C_UV_TRIANGLE_AREA_EPSILON = 0.0000001f;

        // 聚光灯角度最小保护值，避免后续 cos 计算使用 0 度退化角。
        private const float C_MIN_SPOT_ANGLE = 0.001f;

        // Unity 聚光灯外角最大保护值，避免接近 180 度时数值不稳定。
        private const float C_MAX_SPOT_ANGLE = 179;

        // 未设置 innerSpotAngle 时，默认按外角的该比例推导内角。
        private const float C_DEFAULT_INNER_SPOT_ANGLE_RATIO = 0.8f;

        // 灯光 range 的最小保护值，用于源半径和距离相关计算。
        private const float C_MIN_LIGHT_RANGE = 0.001f;

        // 面光源尺寸最小保护值，避免 areaSize 的轴向长度为 0。
        private const float C_MIN_AREA_LIGHT_SIZE = 0.001f;

        // 角度转 cos 时取半角，匹配 spot cone 的内外半角语义。
        private const float C_SPOT_HALF_ANGLE_SCALE = 0.5f;

        // 区域光在烘焙数据中不额外使用 punctual source radius。
        private const float C_AREA_LIGHT_SOURCE_RADIUS = 0;
        
        private const int 
            C_POINT_LIGHT_TYPE_CODE = 1 // 运行时 Job 中 point light 使用的类型编码。
            , C_SPOT_LIGHT_TYPE_CODE = 2 // 运行时 Job 中 spot light 使用的类型编码。
            , C_AREA_LIGHT_TYPE_CODE = 3; // 运行时 Job 中 rectangle / disc area light 共用的类型编码。

        // 运行时 Job 中 rectangle area light 使用的形状编码。
        private const int C_RECTANGLE_LIGHT_SHAPE_CODE = 0;

        // 运行时 Job 中 disc area light 使用的形状编码。
        private const int C_DISC_LIGHT_SHAPE_CODE = 1;
        
        
        public static AHDSceneSummary CollectSummary(AHDBakeSettings settings)
        {
            AHDSceneSummary summary = new AHDSceneSummary();
            
            LightmapData[] lightmaps = LightmapSettings.lightmaps;
            summary.lightmapCount = lightmaps != null ? lightmaps.Length : 0;
            
            CollectRendererSummary(settings, ref summary);
            CollectLightSummary(settings, ref summary);
            
            return summary;
        }

        private static void CollectRendererSummary(AHDBakeSettings settings, ref AHDSceneSummary summary)
        {
            MeshFilter[] meshFilters = Resources.FindObjectsOfTypeAll<MeshFilter>();
            for (int rendererIndex = 0; rendererIndex < meshFilters.Length; rendererIndex++)
            {
                MeshFilter mf = meshFilters[rendererIndex];
                if (!TryGetSceneMeshRenderer(mf, out MeshRenderer renderer, out Mesh mesh))
                {
                    continue;
                }

                Vector3[] vertices = mesh.vertices;
                int[] triangles = mesh.triangles;
                if (vertices == null || triangles == null || vertices.Length == 0)
                {
                    continue;
                }

                // 与 CollectRenderers 保持一致：只接受 mesh.uv2，没有就不当 receiver。
                Vector2[] lightmapUVs = HasValidLightmapUV(mesh, vertices.Length) ? mesh.uv2 : null;

                bool hasReceiver = false;
                bool hasOccluder = !settings.onlyOccluderStaticBlockers || IsOccluderStatic(renderer.gameObject);
                if (hasOccluder)
                {
                    summary.occluderRendererCount++;
                }

                Matrix4x4 localToWorld = renderer.localToWorldMatrix;
                for (int i = 0; i < triangles.Length; i += C_TRIANGLE_INDEX_STEP)
                {
                    int index0 = triangles[i];
                    int index1 = triangles[i + C_TRIANGLE_INDEX_1_OFFSET];
                    int index2 = triangles[i + C_TRIANGLE_INDEX_2_OFFSET];
                    
                    Vector3 world0 = localToWorld.MultiplyPoint3x4(vertices[index0]);
                    Vector3 world1 = localToWorld.MultiplyPoint3x4(vertices[index1]);
                    Vector3 world2 = localToWorld.MultiplyPoint3x4(vertices[index2]);
                    
                    Vector3 faceNormal = Vector3.Cross(world1 - world0, world2 - world0);
                    if (faceNormal.sqrMagnitude <= C_VECTOR_LENGTH_SQ_EPSILON)
                    {
                        continue;
                    }

                    if (hasOccluder)
                    {
                        summary.occluderTriangleCount++;
                    }

                    if (renderer.lightmapIndex < C_MIN_VALID_LIGHTMAP_INDEX
                        || lightmapUVs == null
                        || lightmapUVs.Length != vertices.Length)
                    {
                        continue;
                    }

                    Vector4 scaleOffset = renderer.lightmapScaleOffset;
                    Vector2 lmUv0 = ApplyLightmapScaleOffset(lightmapUVs[index0], scaleOffset);
                    Vector2 lmUv1 = ApplyLightmapScaleOffset(lightmapUVs[index1], scaleOffset);
                    Vector2 lmUv2 = ApplyLightmapScaleOffset(lightmapUVs[index2], scaleOffset);
                    if (!IsValidUVTriangle(lmUv0, lmUv1, lmUv2))
                    {
                        continue;
                    }

                    hasReceiver = true;
                    summary.receiverTriangleCount++;
                }

                if (hasReceiver)
                {
                    summary.receiverRendererCount++;
                }
            }
        }

        private static void CollectLightSummary(AHDBakeSettings settings, ref AHDSceneSummary summary)
        {
            Light[] lights = Resources.FindObjectsOfTypeAll<Light>();
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                if (!TryGetSceneLight(light))
                {
                    continue;
                }

                if (settings.onlyActiveAndEnabledLights && !light.isActiveAndEnabled)
                {
                    continue;
                }

                if (!IsSupportedLightType(light.type))
                {
                    continue;
                }

                if (settings.onlyBakedOrMixedLights && !IsBakedOrMixed(light))
                {
                    continue;
                }

                if (settings.filterIgnoredLightTag && IsIgnoredAHDLight(light))
                {
                    summary.tagExcludedLightCount++;
                    continue;
                }

                summary.lightCount++;
            }
        }

        
        public static AHDSceneData Collect(AHDBakeSettings settings)
        {
            AHDSceneData data = new AHDSceneData();
            
            CollectRenderers(data, settings);
            CollectLights(data, settings);
            
            return data;
        }
        
        private static void CollectRenderers(AHDSceneData data, AHDBakeSettings settings)
        {
            MeshFilter[] meshFilters = Resources.FindObjectsOfTypeAll<MeshFilter>();
            
            int chartId = C_INITIAL_CHART_ID;
            for (int rendererIndex = 0; rendererIndex < meshFilters.Length; rendererIndex++)
            {
                MeshFilter mf = meshFilters[rendererIndex];
                if (!TryGetSceneMeshRenderer(mf, out MeshRenderer renderer, out Mesh mesh))
                {
                    continue;
                }

                Vector3[] vertices = mesh.vertices;
                Vector3[] normals = mesh.normals;
                int[] triangles = mesh.triangles;
                if (vertices == null || triangles == null || vertices.Length == 0)
                {
                    continue;
                }

                // Lightmap UV 必须用 uv2（Unity 烘焙器实际写入的通道）。
                // mesh.uv 是 diffuse UV，几乎不可能与 lightmap pack 对齐，回退会写入完全错误的 direction map。
                // 没有 uv2 的 renderer 直接跳过 receiver 部分，但仍作为遮挡体参与 BVH。
                Vector2[] lightmapUVs = HasValidLightmapUV(mesh, vertices.Length) ? mesh.uv2 : null;
                if (lightmapUVs == null && renderer.lightmapIndex >= C_MIN_VALID_LIGHTMAP_INDEX)
                {
                    Debug.LogWarning("[AHD Baker] Renderer 没有有效 uv2，跳过 receiver。Renderer: " + renderer.name);
                }

                Matrix4x4 localToWorld = renderer.localToWorldMatrix;
                int ownerId = renderer.GetInstanceID();
                bool canOcclude = !settings.onlyOccluderStaticBlockers || IsOccluderStatic(renderer.gameObject);
                for (int i = 0; i < triangles.Length; i += C_TRIANGLE_INDEX_STEP)
                {
                    int index0 = triangles[i];
                    int index1 = triangles[i + C_TRIANGLE_INDEX_1_OFFSET];
                    int index2 = triangles[i + C_TRIANGLE_INDEX_2_OFFSET];
                    Vector3 world0 = localToWorld.MultiplyPoint3x4(vertices[index0]);
                    Vector3 world1 = localToWorld.MultiplyPoint3x4(vertices[index1]);
                    Vector3 world2 = localToWorld.MultiplyPoint3x4(vertices[index2]);
                    
                    Vector3 faceNormal = Vector3.Cross(world1 - world0, world2 - world0);
                    if (faceNormal.sqrMagnitude <= C_VECTOR_LENGTH_SQ_EPSILON)
                    {
                        continue;
                    }

                    faceNormal.Normalize();
                    if (canOcclude)
                    {
                        data.occluders.Add(new AHDOccluderTriangle
                        {
                            world0 = world0,
                            world1 = world1,
                            world2 = world2,
                            boundsMin = math.min(math.min((float3)world0, (float3)world1), (float3)world2),
                            boundsMax = math.max(math.max((float3)world0, (float3)world1), (float3)world2),
                            centroid = ((float3)world0 + (float3)world1 + (float3)world2) / C_TRIANGLE_INDEX_STEP,
                            ownerId = ownerId
                        });
                    }

                    if (renderer.lightmapIndex < C_MIN_VALID_LIGHTMAP_INDEX || lightmapUVs == null)
                    {
                        continue;
                    }

                    Vector4 scaleOffset = renderer.lightmapScaleOffset;
                    Vector2 lmUv0 = ApplyLightmapScaleOffset(lightmapUVs[index0], scaleOffset);
                    Vector2 lmUv1 = ApplyLightmapScaleOffset(lightmapUVs[index1], scaleOffset);
                    Vector2 lmUv2 = ApplyLightmapScaleOffset(lightmapUVs[index2], scaleOffset);
                    if (!IsValidUVTriangle(lmUv0, lmUv1, lmUv2))
                    {
                        continue;
                    }

                    Vector3 normal0 = GetNormal(normals, index0, faceNormal, localToWorld);
                    Vector3 normal1 = GetNormal(normals, index1, faceNormal, localToWorld);
                    Vector3 normal2 = GetNormal(normals, index2, faceNormal, localToWorld);
                    data.receivers.Add(new AHDMeshTriangle
                    {
                        world0 = world0,
                        world1 = world1,
                        world2 = world2,
                        normal0 = normal0,
                        normal1 = normal1,
                        normal2 = normal2,
                        uv0 = lmUv0,
                        uv1 = lmUv1,
                        uv2 = lmUv2,
                        lightmapIndex = renderer.lightmapIndex,
                        chartId = chartId,
                        ownerId = ownerId
                    });
                }

                chartId++;
            }
        }

        private static void CollectLights(AHDSceneData data, AHDBakeSettings settings)
        {
            Light[] lights = Resources.FindObjectsOfTypeAll<Light>();
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                if (!TryGetSceneLight(light))
                {
                    continue;
                }

                if (settings.onlyActiveAndEnabledLights && !light.isActiveAndEnabled)
                {
                    continue;
                }

                if (!IsSupportedLightType(light.type))
                {
                    continue;
                }

                if (settings.onlyBakedOrMixedLights && !IsBakedOrMixed(light))
                {
                    continue;
                }

                if (settings.filterIgnoredLightTag && IsIgnoredAHDLight(light))
                {
                    continue;
                }

                data.lights.Add(CreateLightData(light, settings));
            }
        }
        

        private static bool TryGetSceneMeshRenderer(MeshFilter meshFilter, out MeshRenderer renderer, out Mesh mesh)
        {
            renderer = null;
            mesh = null;
            if (meshFilter == null || meshFilter.gameObject == null)
            {
                return false;
            }

            GameObject gameObject = meshFilter.gameObject;
            if (!gameObject.scene.IsValid()
                || !gameObject.scene.isLoaded
                || EditorUtility.IsPersistent(gameObject)
                || !gameObject.activeInHierarchy)
            {
                return false;
            }

            renderer = meshFilter.GetComponent<MeshRenderer>();
            if (renderer == null || !renderer.enabled)
            {
                return false;
            }

            mesh = meshFilter.sharedMesh;
            return mesh != null;
        }

        private static bool TryGetSceneLight(Light light)
        {
            if (light == null || light.gameObject == null)
            {
                return false;
            }

            GameObject gameObject = light.gameObject;
            return gameObject.scene.IsValid()
                && gameObject.scene.isLoaded
                && !EditorUtility.IsPersistent(gameObject);
        }

        private static bool IsOccluderStatic(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return false;
            }

            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(gameObject);
            return (flags & StaticEditorFlags.OccluderStatic) != 0;
        }

        private static AHDLightData CreateLightData(Light light, AHDBakeSettings settings)
        {
            float outerAngle = Mathf.Clamp(light.spotAngle, C_MIN_SPOT_ANGLE, C_MAX_SPOT_ANGLE);
            float innerAngle = light.innerSpotAngle > C_MIN_SPOT_ANGLE
                ? light.innerSpotAngle
                : outerAngle * C_DEFAULT_INNER_SPOT_ANGLE_RATIO;
            innerAngle = Mathf.Clamp(innerAngle, C_MIN_SPOT_ANGLE, outerAngle);
            Color lightColor = light.color.linear;
            Vector2 areaSize = GetAreaLightSize(light);
            // V1 等价：punctual/spot 的光源端 jitter 半径按 range 缩放，避免远光仍只抖动 5cm。
            float range = Mathf.Max(light.range, C_MIN_LIGHT_RANGE);
            bool isAreaLight = light.type == LightType.Area || light.type == LightType.Disc;
            float sourceRadius = isAreaLight
                ? C_AREA_LIGHT_SOURCE_RADIUS
                : range * Mathf.Max(settings.lightSourceRadiusRatio, 0f);
            
            return new AHDLightData
            {
                type = LightTypeToInt(light.type),
                positionWS = light.transform.position,
                directionToLightWS = -light.transform.forward.normalized,
                rightWS = light.transform.right.normalized,
                upWS = light.transform.up.normalized,
                color = new float3(lightColor.r, lightColor.g, lightColor.b),
                intensity = Mathf.Max(light.intensity, 0),
                range = range,
                spotInnerCos = Mathf.Cos(innerAngle * Mathf.Deg2Rad * C_SPOT_HALF_ANGLE_SCALE),
                spotOuterCos = Mathf.Cos(outerAngle * Mathf.Deg2Rad * C_SPOT_HALF_ANGLE_SCALE),
                sourceRadius = sourceRadius,
                areaSize = areaSize,
                areaShape = LightShapeToInt(light)
            };
        }

        private static Vector2 GetAreaLightSize(Light light)
        {
            if (light == null || (light.type != LightType.Area && light.type != LightType.Disc))
            {
                return Vector2.zero;
            }

            Vector2 areaSize = light.areaSize;
            areaSize.x = Mathf.Max(areaSize.x, C_MIN_AREA_LIGHT_SIZE);
            areaSize.y = Mathf.Max(areaSize.y, C_MIN_AREA_LIGHT_SIZE);
            
            return areaSize;
        }

        private static bool IsBakedOrMixed(Light light)
        {
            return light.lightmapBakeType == LightmapBakeType.Baked || light.lightmapBakeType == LightmapBakeType.Mixed;
        }

        private static bool IsIgnoredAHDLight(Light light)
        {
            if (light == null || light.gameObject == null)
            {
                return false;
            }

            return light.gameObject.CompareTag(c_IgnoredLightTag);
        }

        private static bool IsSupportedLightType(LightType lightType)
        {
            return lightType == LightType.Point
                || lightType == LightType.Spot
                || lightType == LightType.Area
                || lightType == LightType.Disc;
        }

        private static int LightTypeToInt(LightType lightType)
        {
            // Unity 2022+ 中 Rectangle/Disc 都是顶级 LightType，运行时都走 Job 内的 area 路径。
            if (lightType == LightType.Area || lightType == LightType.Disc)
            {
                return C_AREA_LIGHT_TYPE_CODE;
            }

            if (lightType == LightType.Spot)
            {
                return C_SPOT_LIGHT_TYPE_CODE;
            }

            return C_POINT_LIGHT_TYPE_CODE;
        }

        private static int LightShapeToInt(Light light)
        {
            // Unity 2022 中 Light.shape 只含聚光形状（Cone/Pyramid/Box），不再包含 Disc。
            // 区域光的圆盘/矩形区分通过 LightType.Disc / LightType.Area 直接判断。
            if (light == null)
            {
                return C_RECTANGLE_LIGHT_SHAPE_CODE;
            }

            return light.type == LightType.Disc
                ? C_DISC_LIGHT_SHAPE_CODE
                : C_RECTANGLE_LIGHT_SHAPE_CODE;
        }

        private static bool HasValidLightmapUV(Mesh mesh, int vertexCount)
        {
            if (mesh == null)
            {
                return false;
            }

            Vector2[] uv2 = mesh.uv2;
            return uv2 != null && uv2.Length == vertexCount;
        }

        private static Vector2 ApplyLightmapScaleOffset(Vector2 uv, Vector4 scaleOffset)
        {
            return new Vector2(uv.x * scaleOffset.x + scaleOffset.z, uv.y * scaleOffset.y + scaleOffset.w);
        }

        private static bool IsValidUVTriangle(Vector2 uv0, Vector2 uv1, Vector2 uv2)
        {
            float area = (uv1.x - uv0.x) * (uv2.y - uv0.y) - (uv1.y - uv0.y) * (uv2.x - uv0.x);
            return Mathf.Abs(area) > C_UV_TRIANGLE_AREA_EPSILON;
        }

        private static Vector3 GetNormal(Vector3[] normals, int index, Vector3 fallback, Matrix4x4 localToWorld)
        {
            if (normals == null || normals.Length <= index)
            {
                return fallback;
            }

            Vector3 normal = localToWorld.MultiplyVector(normals[index]);
            if (normal.sqrMagnitude <= C_VECTOR_LENGTH_SQ_EPSILON)
            {
                return fallback;
            }

            return normal.normalized;
        }
        
    }
}
