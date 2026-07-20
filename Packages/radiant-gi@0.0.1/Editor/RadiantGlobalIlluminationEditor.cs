using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;

namespace RadiantGI.Universal
{
#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(RadiantGlobalIllumination))]
#else
    [VolumeComponentEditor(typeof(RadiantGlobalIllumination))]
#endif
    public class RadiantGlobalIlluminationEditor : VolumeComponentEditor
    {
        private SerializedDataParameter m_indirectIntensity, m_maxIndirectSourceBrightness, m_indirectDistanceAttenuation, m_normalMapInfluence, m_lumaInfluence;
        private SerializedDataParameter m_nearFieldObscurance, m_nearFieldObscuranceSpread, m_nearFieldObscuranceOccluderDistance, m_nearFieldObscuranceMaxCameraDistance, m_nearFieldObscuranceTintColor;
        private SerializedDataParameter m_virtualEmitters;
        private SerializedDataParameter m_organicLight, m_organicLightThreshold, m_organicLightNormalsInfluence, m_organicLightSpread, m_organicLightTintColor, m_organicLightAnimationSpeed, m_organicLightDistanceScaling;
        private SerializedDataParameter m_brightnessThreshold, m_brightnessMax, m_specularContribution, m_sourceBrightness, m_giWeight, m_nearCameraAttenuation, m_saturation, m_limitToVolumeBounds, m_aoInfluence;
        private SerializedDataParameter m_stencilCheck, m_stencilValue, m_stencilCompareFunction;
        private SerializedDataParameter m_rayCount, m_rayMaxLength, m_rayMaxSamples, m_rayJitter, m_thickness, m_rayBinarySearch, m_rayReuse, m_rayBounce;
        private SerializedDataParameter m_fallbackReuseRays, m_fallbackReflectionProbes, m_probesIntensity, m_fallbackReflectiveShadowMap, m_reflectiveShadowMapIntensity;
        private SerializedDataParameter m_downsampling, m_raytracerAccuracy, m_smoothing;
        private SerializedDataParameter m_temporalReprojection, m_temporalResponseSpeed, m_temporalCameraTranslationResponse, m_temporalChromaThreshold, m_temporalDepthRejection;
        private SerializedDataParameter m_showInEditMode, m_showInSceneView, m_debugView, m_debugDepthMultiplier, m_compareMode, m_compareSameSide, m_comparePanning, m_compareLineAngle, m_compareLineWidth;

#if !UNITY_2021_2_OR_NEWER
        public override bool hasAdvancedMode => false;
#endif

        public override void OnEnable()
        {
            base.OnEnable();

            var sop = new PropertyFetcher<RadiantGlobalIllumination>(serializedObject);
            
            m_indirectIntensity = Unpack(sop.Find(x => x.indirectIntensity));
            m_maxIndirectSourceBrightness = Unpack(sop.Find(x => x.indirectMaxSourceBrightness));
            m_indirectDistanceAttenuation = Unpack(sop.Find(x => x.indirectDistanceAttenuation));
            m_normalMapInfluence = Unpack(sop.Find(x => x.normalMapInfluence));
            m_lumaInfluence = Unpack(sop.Find(x => x.lumaInfluence));
            m_nearFieldObscurance = Unpack(sop.Find(x => x.nearFieldObscurance));
            m_nearFieldObscuranceSpread = Unpack(sop.Find(x => x.nearFieldObscuranceSpread));
            m_nearFieldObscuranceOccluderDistance = Unpack(sop.Find(x => x.nearFieldObscuranceOccluderDistance));
            m_nearFieldObscuranceMaxCameraDistance = Unpack(sop.Find(x => x.nearFieldObscuranceMaxCameraDistance));
            m_nearFieldObscuranceTintColor = Unpack(sop.Find(x => x.nearFieldObscuranceTintColor));
            m_virtualEmitters = Unpack(sop.Find(x => x.virtualEmitters));
            m_organicLight = Unpack(sop.Find(x => x.organicLight));
            m_organicLightThreshold = Unpack(sop.Find(x => x.organicLightThreshold));
            m_organicLightSpread = Unpack(sop.Find(x => x.organicLightSpread));
            m_organicLightNormalsInfluence = Unpack(sop.Find(x => x.organicLightNormalsInfluence));
            m_organicLightTintColor = Unpack(sop.Find(x => x.organicLightTintColor));
            m_organicLightAnimationSpeed = Unpack(sop.Find(x => x.organicLightAnimationSpeed));
            m_organicLightDistanceScaling = Unpack(sop.Find(x => x.organicLightDistanceScaling));
            m_brightnessThreshold = Unpack(sop.Find(x => x.brightnessThreshold));
            m_brightnessMax = Unpack(sop.Find(x => x.brightnessMax));
            m_specularContribution = Unpack(sop.Find(x => x.specularContribution));
            m_sourceBrightness = Unpack(sop.Find(x => x.sourceBrightness));
            m_giWeight = Unpack(sop.Find(x => x.giWeight));
            m_nearCameraAttenuation = Unpack(sop.Find(x => x.nearCameraAttenuation));
            m_saturation = Unpack(sop.Find(x => x.saturation));
            m_limitToVolumeBounds = Unpack(sop.Find(x => x.limitToVolumeBounds));
            m_stencilCheck = Unpack(sop.Find(x => x.stencilCheck));
            m_stencilValue = Unpack(sop.Find(x => x.stencilValue));
            m_stencilCompareFunction = Unpack(sop.Find(x => x.stencilCompareFunction));
            m_aoInfluence = Unpack(sop.Find(x => x.aoInfluence));
            m_rayCount = Unpack(sop.Find(x => x.rayCount));
            m_rayMaxLength = Unpack(sop.Find(x => x.rayMaxLength));
            m_rayMaxSamples = Unpack(sop.Find(x => x.rayMaxSamples));
            m_rayJitter = Unpack(sop.Find(x => x.rayJitter));
            m_thickness = Unpack(sop.Find(x => x.thickness));
            m_rayBinarySearch = Unpack(sop.Find(x => x.rayBinarySearch));
            m_rayReuse = Unpack(sop.Find(x => x.rayReuse));
            m_rayBounce = Unpack(sop.Find(x => x.rayBounce));
            m_fallbackReuseRays = Unpack(sop.Find(x => x.fallbackReuseRays));
            m_fallbackReflectionProbes = Unpack(sop.Find(x => x.fallbackReflectionProbes));
            m_probesIntensity = Unpack(sop.Find(x => x.probesIntensity));
            m_fallbackReflectiveShadowMap = Unpack(sop.Find(x => x.fallbackReflectiveShadowMap));
            m_reflectiveShadowMapIntensity = Unpack(sop.Find(x => x.reflectiveShadowMapIntensity));
            m_downsampling = Unpack(sop.Find(x => x.downsampling));
            m_raytracerAccuracy = Unpack(sop.Find(x => x.raytracerAccuracy));
            m_smoothing = Unpack(sop.Find(x => x.smoothing));
            m_temporalReprojection = Unpack(sop.Find(x => x.temporalReprojection));
            m_temporalResponseSpeed = Unpack(sop.Find(x => x.temporalResponseSpeed));
            m_temporalCameraTranslationResponse = Unpack(sop.Find(x => x.temporalCameraTranslationResponse));
            m_temporalDepthRejection = Unpack(sop.Find(x => x.temporalDepthRejection));
            m_temporalChromaThreshold = Unpack(sop.Find(x => x.temporalChromaThreshold));
            m_showInEditMode = Unpack(sop.Find(x => x.showInEditMode));
            m_showInSceneView = Unpack(sop.Find(x => x.showInSceneView));
            m_debugView = Unpack(sop.Find(x => x.debugView));
            m_debugDepthMultiplier = Unpack(sop.Find(x => x.debugDepthMultiplier));
            m_compareMode = Unpack(sop.Find(x => x.compareMode));
            m_compareSameSide = Unpack(sop.Find(x => x.compareSameSide));
            m_comparePanning = Unpack(sop.Find(x => x.comparePanning));
            m_compareLineAngle = Unpack(sop.Find(x => x.compareLineAngle));
            m_compareLineWidth = Unpack(sop.Find(x => x.compareLineWidth));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUIContent label;
            
            //常规 ------------------------------------------
            label = new GUIContent("常规");
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            
            label = new GUIContent("间接光强度", "间接照明的强度");
            PropertyField(m_indirectIntensity, label);
            
            EditorGUI.indentLevel++;
            {
                label = new GUIContent("距离衰减", "应用于间接照明的距离衰减，按距离的平方降低间接强度");
                PropertyField(m_indirectDistanceAttenuation, label);

                label = new GUIContent("一次额外的弹跳", "添加一次光线反射");
                PropertyField(m_rayBounce, label);

                label = new GUIContent("间接光源的最大亮度");
                PropertyField(m_maxIndirectSourceBrightness, label);
                
                label = new GUIContent("法线图的影响", "当接收间接照明时，确定对表面法线图有多大的影响。");
                PropertyField(m_normalMapInfluence, label);
                
                label = new GUIContent("亮度影响", "仅在前向渲染模式下：基于你感觉的强弱，使用像素亮度来增加效果的多样性。将此值设置为 0 可禁用此功能。");
                PropertyField(m_lumaInfluence, label);
            }
            EditorGUI.indentLevel--;
            
            label = new GUIContent("近场遮蔽", "近场遮蔽效应的强度。使被附近其他表面遮挡的表面变暗。");
            PropertyField(m_nearFieldObscurance, label);
            if (m_nearFieldObscurance.value.floatValue > 0f)
            {
                EditorGUI.indentLevel++;
                label = new GUIContent("扩散", "近场遮蔽效应的扩散或半径");
                PropertyField(m_nearFieldObscuranceSpread, label);

                label = new GUIContent("遮蔽器的距离", "遮蔽器的距离阈值");
                PropertyField(m_nearFieldObscuranceOccluderDistance, label);

                label = new GUIContent("最大相机距离", "近场遮蔽效应的最大距离");
                PropertyField(m_nearFieldObscuranceMaxCameraDistance, label);

                label = new GUIContent("色调颜色", "近场遮蔽效果的色调颜色");
                PropertyField(m_nearFieldObscuranceTintColor, label);
                EditorGUI.indentLevel--;
            }
            
            label = new GUIContent("虚拟发射器", "在场景中启用用户定义的灯光发射器");
            PropertyField(m_virtualEmitters, label);

            label = new GUIContent("有机光", "有机光的强度。此选项将人工/程序化的光变化注入到 g-buffer 中，以产生更自然和有趣的照明环境。这种增加的照明也用作间接照明的光源。");
            PropertyField(m_organicLight, label);
            if (m_organicLight.value.floatValue > 0f)
            {
                EditorGUI.indentLevel++;
                if (!RadiantRenderFeature.isRenderingInDeferred)
                {
                    EditorGUILayout.HelpBox("有机光需要延迟渲染路径。", MessageType.Warning);
                }

                label = new GUIContent("扩散", "有机光扩散");
                PropertyField(m_organicLightSpread, label);

                label = new GUIContent("阈值", "有机光噪声计算阈值");
                PropertyField(m_organicLightThreshold, label);
                
                label = new GUIContent("法线影响", "有机光的法线影响保留了纹理上的法线效果");
                PropertyField(m_organicLightNormalsInfluence, label);

                label = new GUIContent("色调颜色", "有机光的色调颜色");
                PropertyField(m_organicLightTintColor, label);

                label = new GUIContent("动画速度");
                PropertyField(m_organicLightAnimationSpeed, label);

                label = new GUIContent("距离缩放", "减少远处的有机光图案重复");
                PropertyField(m_organicLightDistanceScaling, label);
                EditorGUI.indentLevel--;
            }

            //质量 ------------------------------------------
            EditorGUILayout.LabelField("质量", EditorStyles.miniLabel);

            label = new GUIContent("光线数量", "每像素的光线数");
            PropertyField(m_rayCount, label);
            
            label = new GUIContent("光线最大距离", "增加此值可能还需要增加“最大采样数”以避免质量损失");
            PropertyField(m_rayMaxLength, label);
            
            label = new GUIContent("光线最大采样数", "在光线步进期间的最大采样数");
            PropertyField(m_rayMaxSamples, label);
            
            label = new GUIContent("抖动", "抖动会向光线方向添加随机偏移，以减少条带。这在使用低采样数时很有用。");
            PropertyField(m_rayJitter, label);
            
            label = new GUIContent("厚度", "任何几何图形的假定厚度。用于确定光线是否穿过表面。");
            PropertyField(m_thickness, label);
            
            label = new GUIContent("2进制搜索", "用2进制搜索提高光线步进的精度");
            PropertyField(m_rayBinarySearch, label);

            label = new GUIContent("平滑", "额外的模糊通道");
            PropertyField(m_smoothing, label);
            
            label = new GUIContent("时间滤波器", "使用运动矢量混合到历史缓冲区中以减少闪烁。仅工作在运行时。");
            PropertyField(m_temporalReprojection, label);
            if (m_temporalReprojection.value.boolValue)
            {
                EditorGUI.indentLevel++;
                if (m_temporalReprojection.value.boolValue && !Application.isPlaying && m_showInEditMode.value.boolValue)
                {
                    EditorGUILayout.HelpBox("如果不在播放模式下，则时间滤波器在场景视图中不起作用。", MessageType.Info);
                }
                
                label = new GUIContent("响应速度", "对屏幕变化的反应速度。较高的值可减少重影，但也会减少平滑。");
                PropertyField(m_temporalResponseSpeed, label);
                
                label = new GUIContent("色度阈值", "历史和当前 GI 缓冲区之间允许的颜色差异。");
                PropertyField(m_temporalChromaThreshold, label);
                
                label = new GUIContent("相机平移响应", "对相机位置变化的反应速度。较高的值可减少相机移动时的重影。");
                PropertyField(m_temporalCameraTranslationResponse, label);
                EditorGUI.indentLevel--;
            }

            //备用计划 ------------------------------------------
            EditorGUILayout.LabelField("备用计划", EditorStyles.miniLabel);
            
            label = new GUIContent("重用光线", "万一光线错过了目标，重复使用前一帧的光线。");
            PropertyField(m_fallbackReuseRays, label);
            if (m_fallbackReuseRays.value.boolValue)
            {
                EditorGUI.indentLevel++;
                
                label = new GUIContent("强度", "如果光线错过了目标，重用历史缓冲区的结果。该值是之前颜色的强度，以防光线错过目标。");
                PropertyField(m_rayReuse, label);
                if (m_rayReuse.value.floatValue > 0)
                {
                    if (!m_temporalReprojection.value.boolValue)
                    {
                        EditorGUILayout.HelpBox("重用光线在启用时间过滤器的情况下工作。", MessageType.Info);
                    }
                    
                    label = new GUIContent("深度抑制", "与当前帧的深度差异，当在重复使用光线时丢弃历史缓冲区。");
                    PropertyField(m_temporalDepthRejection, label);
                }
                EditorGUI.indentLevel--;
            }
            
            label = new GUIContent("使用反射探针", "万一射线错过了目标，使用附近的探针（如果有的话）");
            PropertyField(m_fallbackReflectionProbes, label);
            if (m_fallbackReflectionProbes.value.boolValue)
            {
                EditorGUI.indentLevel++;
                label = new GUIContent("探针强度", "自定义全局探头强度倍增器。注意，每个探头还具有强度属性。");
                PropertyField(m_probesIntensity, label);
                EditorGUI.indentLevel--;
            }
            
            label = new GUIContent("启用 Shadow Map 反射", "万一射线错过了目标，使用主方向灯上的 Shadow Map 反射。需要将 ReflectiveShadowMap 脚本添加到方向光上才能使用此功能。");
            PropertyField(m_fallbackReflectiveShadowMap, label);
            if (m_fallbackReflectiveShadowMap.value.boolValue)
            {
                EditorGUI.indentLevel++;
                if (!RadiantShadowMap.installed)
                {
                    EditorGUILayout.HelpBox("将 Radiant Shadow Map 脚本添加到主方向光上。", MessageType.Warning);
                }

                label = new GUIContent("强度");
                PropertyField(m_reflectiveShadowMapIntensity, label);
                EditorGUI.indentLevel--;
            }

            //性能 ------------------------------------------
            EditorGUILayout.LabelField("性能", EditorStyles.miniLabel);
            
            label = new GUIContent("光线追踪器精度", "光线追踪精度。减小此值将缩小光线追踪期间使用的深度缓冲区，提高性能降低精度。");
            PropertyField(m_raytracerAccuracy, label);
            
            label = new GUIContent("降采样", "降低精度，提高GI全部阶段的性能。");
            PropertyField(m_downsampling, label);
            EditorGUILayout.HelpBox("注意：当在 PC 上使用 OpenGl 时，Depth Copy 会产生条纹状的深度结果，因为正式打包时不会有这种使用场景，故当前的处理方式是降采样对深度无效", MessageType.Warning);

            //美术控制 ------------------------------------------
            EditorGUILayout.LabelField("美术控制", EditorStyles.miniLabel);
            
            label = new GUIContent("亮度阈值", "计算具有最小光度的物体发出的 GI。");
            PropertyField(m_brightnessThreshold, label);

            label = new GUIContent("最大亮度", "最大 GI 亮度");
            PropertyField(m_brightnessMax, label);
            
            label = new GUIContent("镜面反射贡献", "增加镜面GI的量。减小此值以避免反光材料过度曝光。");
            PropertyField(m_specularContribution, label);
            
            label = new GUIContent("源的亮度", "原始图像的亮度。您可以降低此值以使 GI 更加突出。");
            PropertyField(m_sourceBrightness, label);
            
            label = new GUIContent("GI 权重", "增加最终 GI 贡献与源颜色像素。增加此值可根据接收到的 GI 量降低源像素颜色的强度，使应用的GI更加明显。");
            PropertyField(m_giWeight, label);

            label = new GUIContent("饱和", "调整计算出来的 GI 颜色饱和度。");
            PropertyField(m_saturation, label);

            label = new GUIContent("靠近相机衰减", "减弱附近表面的 GI 亮度。");
            PropertyField(m_nearCameraAttenuation, label);
            
            label = new GUIContent("限制到卷边界", "仅在后处理体积内应用 GI （仅当卷为本地卷时才使用）");
            PropertyField(m_limitToVolumeBounds, label);
            
            label = new GUIContent("模板检查", "在GI合成过程中启用模板检查。此选项允许您排除某些也使用模板缓冲区的对象的 GI。");
            PropertyField(m_stencilCheck, label);
            if (m_stencilCheck.value.boolValue)
            {
                label = new GUIContent("模板值");
                PropertyField(m_stencilValue, label);
                
                label = new GUIContent("模板 比较函数");
                PropertyField(m_stencilCompareFunction, label);
            }
            
            label = new GUIContent("AO 影响", "与 URP SSAO 集成（HBAO 也处于 Lit AO 模式）。影响 Radiant 创造的间接照明的环境光遮蔽量。");
            PropertyField(m_aoInfluence, label);

            //调试 ------------------------------------------
            EditorGUILayout.LabelField("Debug", EditorStyles.miniLabel);

            label = new GUIContent("在编辑模式下执行", "在编辑模式下（不在播放模式下）也渲染效果。");
            PropertyField(m_showInEditMode, label);

            label = new GUIContent("在场景视图中也执行", "在“场景视图”（Scene View） 中也渲染效果。");
            PropertyField(m_showInSceneView, label);

            label = new GUIContent("Debug 窗口");
            PropertyField(m_debugView, label);
            if (!m_temporalReprojection.value.boolValue && (m_debugView.value.intValue == (int)RadiantGlobalIllumination.EDebugView.TemporalAccumulationBuffer))
            {
                EditorGUILayout.HelpBox("时间滤波器未执行，没有可用的 Debug 输出。", MessageType.Warning);
            }
            else if (m_debugView.value.intValue == (int)RadiantGlobalIllumination.EDebugView.ReflectiveShadowMap && !m_fallbackReflectiveShadowMap.value.boolValue)
            {
                EditorGUILayout.HelpBox("反射阴影贴图的回退选项未启用，没有可用的 Debug 输出。", MessageType.Warning);
            }
            else if (m_debugView.value.intValue == (int)RadiantGlobalIllumination.EDebugView.Depth)
            {
                label = new GUIContent("Debug 深度乘数", "深度调试视图的深度值乘数，调节这个乘数可以更清楚的观察屏幕深度。");
                PropertyField(m_debugDepthMultiplier, label);
            }

            label = new GUIContent("对比模式", "增加 GI 的前后效果对比");
            PropertyField(m_compareMode, label);
            if (m_compareMode.value.boolValue)
            {
                EditorGUI.indentLevel++;

                label = new GUIContent("左右对比", "把屏幕切割成2部分，左边是原始画面，右边是GI效果。");
                PropertyField(m_compareSameSide, label);
                if (m_compareSameSide.value.boolValue)
                {
                    label = new GUIContent("平移", "决定比较的中心点在哪里");
                    PropertyField(m_comparePanning, label);
                }
                else
                {
                    label = new GUIContent("分割线的角度");
                    PropertyField(m_compareLineAngle, label);

                    label = new GUIContent("分割线的宽度");
                    PropertyField(m_compareLineWidth, label);
                }
                
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}