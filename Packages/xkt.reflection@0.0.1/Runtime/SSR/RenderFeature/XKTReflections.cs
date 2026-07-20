using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XKnight.Reflection.Runtime
{
    public enum Scope
    {
        OnlyThisObject = 0,
        IncludeChildren = 10
    }

    [ExecuteAlways]
    public class XKTReflections : MonoBehaviour
    {
        public bool ignore;

        public Scope scope;

        public LayerMask layerMask = -1;

        public string nameFilter;

        public int subMeshMask;

        public bool overrideGlobalSettings;

        [Range(4, 128)] public int sampleCount = 16;

        public float stepSize;

        public float maxRayLength;

        public float thickness = 0.3f;

        [Range(0, 16)] public int binarySearchIterations = 6;

        public bool refineThickness;
        [Range(0.005f, 1f)]
        public float thicknessFine = 0.05f;
        public string materialSmoothnessIntensityPropertyName = "_Smoothness";

        public bool overrideSmoothness = false;

        [Range(0, 1)] public float smoothness;

        public float decay = 2f;

        public float fresnel = 0.75f;
        [Min(0)] public float fuzzyness;

        public float contactHardening;

        [Range(0, 1f)] public float jitter = 0.3f;

        public Vector2 uvDistortionSpeed;

        /// <summary>
        /// 最终需要反射的renderer
        /// </summary>
        [NonSerialized] public readonly List<SSRRenderer> ssrRenderers = new List<SSRRenderer>();

        /// <summary>
        /// 当前组件包含的所有renderer
        /// </summary>
        [NonSerialized] public readonly List<Renderer> renderers = new List<Renderer>();

        public readonly static List<XKTReflections> instances = new List<XKTReflections>();
        public static bool needUpdateMaterials;
        public static XKTReflections currentEditingXktReflections;

        void OnEnable()
        {
            if (maxRayLength == 0)
            {
                maxRayLength = Mathf.Max(0.1f, stepSize * sampleCount);
            }

            Refresh();
            instances.Add(this);
        }

        void OnDisable()
        {
            if (instances != null)
            {
                int k = instances.IndexOf(this);
                if (k >= 0)
                {
                    instances.RemoveAt(k);
                }
            }
        }

        private void OnValidate()
        {
            fuzzyness = Mathf.Max(0, fuzzyness);
            thickness = Mathf.Max(0.01f, thickness);
            contactHardening = Mathf.Max(0, contactHardening);
            decay = Mathf.Max(1f, decay);
            if (maxRayLength == 0)
            {
                maxRayLength = stepSize * sampleCount;
            }

            maxRayLength = Mathf.Max(0.1f, maxRayLength);
            Refresh();
        }

        private void OnDestroy()
        {
            if (ssrRenderers == null) return;
            for (int k = 0; k < ssrRenderers.Count; k++)
            {
                Material[] mats = ssrRenderers[k].ssrMaterials;
                if (mats != null)
                {
                    for (int j = 0; j < mats.Length; j++)
                    {
                        if (mats[j] != null) DestroyImmediate(mats[j]);
                    }
                }
            }
        }

        public void Refresh()
        {
            //通过模式填充renderers
            switch (scope)
            {
                //只选取当前物体，将其添加到renderers中
                case Scope.OnlyThisObject:
                {
                    renderers.Clear();
                    Renderer r = GetComponent<Renderer>();
                    if (r != null)
                    {
                        renderers.Add(r);
                    }
                }
                    break;
                //选取了当前物体及其下面子物体
                case Scope.IncludeChildren:
                    //获取子物体所有render，添加到renderers中
                    GetComponentsInChildren(true, renderers);
                    //获取所有renderer数量
                    int rCount = renderers.Count;
                    //判断是否开启了名称过滤
                    bool usesNameFilter = !string.IsNullOrEmpty(nameFilter);
                    for (int k = 0; k < rCount; k++)
                    {
                        Renderer r = renderers[k];
                        //比较Layer
                        if (((1 << r.gameObject.layer) & layerMask) == 0)
                        {
                            renderers[k] = null;
                            continue;
                        }

                        //名称过滤
                        if (usesNameFilter)
                        {
                            if (!r.name.Contains(nameFilter))
                            {
                                renderers[k] = null;
                                continue;
                            }
                        }

                        //如果子物体已经挂在了反射组件，那么将其移除出renderers组
                        XKTReflections refl = r.GetComponent<XKTReflections>();
                        if (refl != null && refl != this)
                        {
                            renderers[k] = null;
                        }
                    }

                    break;
            }

            // Prepare required ssr materials slots
            ssrRenderers.Clear();
            if (ignore) return;

            //通过renderers组决定最终需要接受反射的物体
            int renderersCount = renderers.Count;
            for (int k = 0; k < renderersCount; k++)
            {
                Renderer r = renderers[k];
                //在上一步被设置为null的跳过
                if (r == null) continue;
                XKTReflections refl = r.GetComponent<XKTReflections>();
                if (refl != null && refl.ignore)
                {
                    continue;
                }

                //创建最终的反射对象
                SSRRenderer ssr = new SSRRenderer
                {
                    renderer = r
                };
                ssrRenderers.Add(ssr);
            }
        }

        /// <summary>
        /// 反射Renderer
        /// </summary>
        public class SSRRenderer
        {
            public Renderer renderer;
            public Material[] ssrMaterials;
            readonly public List<Material> originalMaterials = new List<Material>();
            readonly List<Material> tempMaterials = new List<Material>();
            public bool isInitialized;
            public bool exclude;
            public Collider collider;
            public bool hasStaticBounds;
            public Bounds staticBounds;

            /// <summary>
            /// 计算静态物体Bounds，使用传递进的新材质重新填充ssrMaterials
            /// </summary>
            /// <param name="ssrMat"></param>
            public void Init(Material ssrMat)
            {
                isInitialized = true;
                renderer.GetSharedMaterials(originalMaterials);
                collider = renderer.GetComponent<Collider>();
                hasStaticBounds = false;
                //如果renderer属于静态物体又没有Collider，则自己计算bounds
                if (renderer.isPartOfStaticBatch && collider == null)
                {
                    MeshFilter mf = renderer.GetComponent<MeshFilter>();
                    if (mf != null)
                    {
                        Mesh mesh = mf.sharedMesh;
                        if (mesh != null)
                        {
                            int subMeshStartIndex = ((MeshRenderer)renderer).subMeshStartIndex;
                            staticBounds = mesh.GetSubMesh(subMeshStartIndex).bounds;
                            int subMeshesCount = originalMaterials.Count;
                            for (int k = 1; k < subMeshesCount; k++)
                            {
                                staticBounds.Encapsulate(mesh.GetSubMesh(subMeshStartIndex + k).bounds);
                            }

                            hasStaticBounds = true;
                        }
                    }
                }

                if (ssrMaterials == null || ssrMaterials.Length != originalMaterials.Count)
                {
                    ssrMaterials = new Material[originalMaterials.Count];
                }

                //清空旧材质，重新赋值
                for (int k = 0; k < ssrMaterials.Length; k++)
                {
                    if (ssrMaterials[k] != null)
                    {
                        DestroyImmediate(ssrMaterials[k]);
                    }

                    ssrMaterials[k] = Instantiate(ssrMat);
                }
            }

            /// <summary>
            /// 检测材质变更,本质是重新调用Init方法
            /// </summary>
            /// <param name="shinyMat"></param>
            public void CheckMaterialChanges(Material mat)
            {
                //未初始化||未拥有renderer
                if (!isInitialized || renderer == null) return;

                renderer.GetSharedMaterials(tempMaterials);
                int tempMaterialsCount = tempMaterials.Count;
                if (tempMaterialsCount != originalMaterials.Count)
                {
                    Init(mat);
                    return;
                }

                for (int k = 0; k < tempMaterialsCount; k++)
                {
                    if (tempMaterials[k] != originalMaterials[k])
                    {
                        Init(mat);
                        return;
                    }

                    ;
                }
            }

            /// <summary>
            /// Forces a material update in next render frame
            /// </summary>
            public void UpdateMaterialProperties()
            {
                isInitialized = false;
            }

            public const string SMOOTHNESSMAP = "SSR_SMOOTHNESSMAP";
            public const string NORMALMAP = "SSR_NORMALMAP";
            public const string JITTER = "SSR_JITTER";
            public static int SSRSettings5 = Shader.PropertyToID("_SSRSettings5");
            public static int SSRSettings2 = Shader.PropertyToID("_SSRSettings2");
            public static int SSRSettings = Shader.PropertyToID("_SSRSettings");
            public static int MaterialData = Shader.PropertyToID("_MaterialData");
            public static int DistortionData = Shader.PropertyToID("_DistortionData");

            /// <summary>
            /// 更新材质属性
            /// </summary>
            /// <param name="go"></param>
            /// <param name="globalSettings"></param>
            public void UpdateMaterialProperties(XKTReflections go, XKTSSR globalSettings)
            {
                // if (go.subMeshSettings == null)
                // {
                //     go.subMeshSettings = new SubMeshSettingsData[0];
                // }
                float totalReflectivity = 0;
                int ssrMaterialsCount = ssrMaterials != null ? ssrMaterials.Length : 0;
                //NormalSource normalSource = go.normalSource;
                //if (SRR.isDeferredActive) normalSource = NormalSource.Material;
                for (int s = 0; s < ssrMaterialsCount; s++)
                {
                    Material ssrMat = ssrMaterials[s];
                    Material originalMaterial = originalMaterials[s];
                    Texture normalMap = null;
                    float smoothness = 0;
                    float reflectivity = 0;

                    bool hasSmoothnessMap = false;
                    bool hasNormalMap = false;

                    if (originalMaterial != null)
                    {
                        //TODO ：需要一个Smoothness
                        if (go.overrideSmoothness)
                        {
                            smoothness = go.smoothness;
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(go.materialSmoothnessIntensityPropertyName) &&
                                originalMaterial.HasProperty(go.materialSmoothnessIntensityPropertyName))
                            {
                                smoothness = 1 - originalMaterial.GetFloat(go.materialSmoothnessIntensityPropertyName);
                            }

                            // if (!string.IsNullOrEmpty(go.materialSmoothnessMapPropertyName) && originalMaterial.HasProperty(go.materialSmoothnessMapPropertyName)) {
                            //     metallicGlossMap = originalMaterial.GetTexture(go.materialSmoothnessMapPropertyName);
                            // } else if (originalMaterial.HasProperty(ShaderParams.MetallicGlossMap)) {
                            //     metallicGlossMap = originalMaterial.GetTexture(ShaderParams.MetallicGlossMap);
                            // }
                        }


                        // if (!string.IsNullOrEmpty(go.materialNormalMapPropertyName) && originalMaterial.HasProperty(go.materialNormalMapPropertyName)) {
                        //     normalMap = originalMaterial.GetTexture(go.materialNormalMapPropertyName);
                        // } else if (originalMaterial.HasProperty(ShaderParams.BumpMap)) {
                        //     normalMap = originalMaterial.GetTexture(ShaderParams.BumpMap);
                        // }
                        // if (!string.IsNullOrEmpty(go.materialNormalMapScalePropertyName) && originalMaterial.HasProperty(go.materialNormalMapScalePropertyName)) {
                        //     normalMapScale = originalMaterial.GetFloat(ShaderParams.BumpMap_Scale);
                        // }
                        //
                        // if (normalMap != null) {
                        //     ssrMat.SetTexture(ShaderParams.BumpMap, normalMap);
                        //     if (originalMaterial.HasProperty(ShaderParams.BaseMap_ST)) {
                        //         ssrMat.SetVector(ShaderParams.BumpMap_ST, originalMaterial.GetVector(ShaderParams.BaseMap_ST));
                        //     }
                        //     ssrMat.SetFloat(ShaderParams.BumpMap_Scale, normalMapScale);
                        //     ssrMat.EnableKeyword(ShaderParams.SKW_NORMALMAP);
                        //     hasNormalMap = true;
                        // }

                        totalReflectivity += smoothness;
                    }

                    if (!hasSmoothnessMap)
                    {
                        ssrMat.DisableKeyword(SMOOTHNESSMAP);
                    }

                    if (!hasNormalMap)
                    {
                        ssrMat.DisableKeyword(NORMALMAP);
                    }

                    if (go.overrideGlobalSettings)
                    {
                        if (go.jitter > 0)
                        {
                            ssrMat.EnableKeyword(JITTER);
                        }
                        else
                        {
                            ssrMat.DisableKeyword(JITTER);
                        }
                        if (go.refineThickness) {
                            ssrMat.EnableKeyword(XKTSSRShaderProperties.REFINE_THICKNESS);
                        } else {
                            ssrMat.DisableKeyword(XKTSSRShaderProperties.REFINE_THICKNESS);
                        }

                        ssrMat.SetVector(SSRSettings5,
                            new Vector4(go.thicknessFine *go.thickness, globalSettings.smoothnessThreshold.value,
                                globalSettings.skyboxIntensity.value, 0));
                        ssrMat.SetVector(SSRSettings2,
                            new Vector4(go.jitter, go.contactHardening + 0.0001f,
                                globalSettings.reflectionsMultiplier.value, reflectivity));
                        ssrMat.SetVector(SSRSettings,
                            new Vector4(go.thickness, go.sampleCount, go.binarySearchIterations, go.maxRayLength));
                        ssrMat.SetVector(MaterialData,
                            new Vector4(smoothness, go.fresnel, go.fuzzyness + 1f, go.decay));
                    }
                    else
                    {
                        if (globalSettings.jitter.value > 0)
                        {
                            ssrMat.EnableKeyword(JITTER);
                        }
                        else
                        {
                            ssrMat.DisableKeyword(JITTER);
                        }

                        if (globalSettings.refineThickness.value) {
                            ssrMat.EnableKeyword(XKTSSRShaderProperties.REFINE_THICKNESS);
                        } else {
                            ssrMat.DisableKeyword(XKTSSRShaderProperties.REFINE_THICKNESS);
                        }
                        ssrMat.SetVector(SSRSettings5,
                            new Vector4(globalSettings.thickness.value, globalSettings.smoothnessThreshold.value,
                                globalSettings.skyboxIntensity.value, 0));
                        ssrMat.SetVector(SSRSettings2,
                            new Vector4(globalSettings.jitter.value, globalSettings.contactHardening.value + 0.0001f,
                                globalSettings.reflectionsMultiplier.value, reflectivity));
                        ssrMat.SetVector(SSRSettings,
                            new Vector4(globalSettings.thickness.value*globalSettings.thicknessFine.value, globalSettings.sampleCount.value,
                                globalSettings.binarySearchIterations.value, globalSettings.maxRayLength.value));
                        ssrMat.SetVector(MaterialData,
                            new Vector4(smoothness, globalSettings.fresnel.value, globalSettings.fuzzyness.value + 1f,
                                globalSettings.decay.value));
                    }

                    ssrMat.SetVector(DistortionData, new Vector4(go.uvDistortionSpeed.x, go.uvDistortionSpeed.y, 0, 0));
                }

                exclude = (totalReflectivity == 0);
            }
        }
    }
}