// Created by: WangYu   Date: 2025-12-16

using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ToonPostProcessing
{
    [CustomEditor(typeof(OutlineDistortVolume))]
    public class OutlineDistortVolumeEditor : VolumeComponentEditor
    {
        private struct Styles
        {
            public static readonly GUIContent header = new("扭曲轮廓的后处理配置");
            
            public const string useTooltip = 
                "使用提示:" 
                + "\n   1，如果看不到效果，请检查摄像机的近/远平面距离是否合适，范围越大精度下降的就越厉害。";

            public static readonly GUIContent renderData = new("渲染数据"
                , "包含用来绘制效果的渲染资源，因为 Volume 上的参数都是写在材质球上的，所以其也具有记录参数的功能。我们可以为不同的 Volume 创建专属的 渲染数据配置。");
            
            // 遮罩参数 >>>>>>>>>>>>>>>>>>>>>>>
            public static readonly GUIContent maskHeader = new("遮罩参数", "为了增加对扭曲描边的控制而绘制的遮罩。");
            private const string c_SameAsCharacterShader = "角色着色器上的同款功能";
            public static readonly GUIContent meshPreview = new("未蒙皮的 Mesh 预览", c_SameAsCharacterShader);
            public static readonly GUIContent outlineWidth = new("轮廓宽度", c_SameAsCharacterShader);
            public static readonly GUIContent outlinePower = new("调整轮廓透视", c_SameAsCharacterShader);
            
            public static readonly GUIContent outlineFadeHeader = new("描边渐隐", c_SameAsCharacterShader);
            public static readonly GUIContent outlineFadeStart = new("开始距离", c_SameAsCharacterShader);
            public static readonly GUIContent outlineFadeEnd = new("结束距离", c_SameAsCharacterShader);
            
            public static readonly GUIContent yAxisOffset = new("Y 轴偏移（模型的世界空间）", "偏移绘制遮罩的对象在 Y 轴上的位置。");
            public static readonly GUIContent invertFadeDirection = new("反转与视空间法线产生的 dot 值", "在调整渐变曲线前，反转计算出来的渐变数值。");

            public static readonly GUIContent gradientHeader = new("调整渐变曲线", "因为最后产生的渐变数值，可能过于平缓，我们可以用这些参数来调整数值的分布曲线。");
            public static readonly GUIContent gradientScale = new("缩放");
            public static readonly GUIContent gradientLeft = new("左值");
            public static readonly GUIContent gradientRight = new("右值");
            public static readonly GUIContent gradientPower = new("power");
            
            // 后处理参数 >>>>>>>>>>>>>>>>>>>>>>>
            public static readonly GUIContent postProcessHeader = new("后处理参数", "最终绘制扭曲描边的后处理效果。");

            public static readonly GUIContent distortTextureHeader = new("扭曲纹理");
            public static readonly GUIContent distortTextureTiling = new("Tiling");
            public static readonly GUIContent distortTextureOffset = new("Offset");
            
            public static readonly GUIContent outlineColor = new("轮廓颜色");
            public static readonly GUIContent outlineAlpha = new("轮廓 Alpha");

            public static readonly GUIContent distortUVDeltaHeader = new("扭曲纹理 UV 的增量运动", "滚动采样扭曲纹理，产生流动的效果。");
            public static readonly GUIContent distortUVScrollSpeed = new("扭曲纹理的 UV 滚动速度（有方向性）");
            public static readonly GUIContent distortScreenScale = new("扭曲的屏幕缩放（有方向性）");
            public static readonly GUIContent distortDirectionFactor = new("方向系数（加法）");

            public static readonly GUIContent multipleSampleHeader = new("多次采样", "会增加2次对噪波纹理的采样，以叠加噪波的方式丰富最终扭曲数值的变化，产生更多样性的效果。");
            public static readonly GUIContent multipleSampleOn = new("启用多次采样");
            public static readonly GUIContent sample2 = new("第2次采样");
            public static readonly GUIContent sample3 = new("第3次采样");
            public static readonly GUIContent offsetSampleUV = new("偏移UV");
            public static readonly GUIContent offsetSampleTime = new("偏移时间");
            public static readonly GUIContent appendDistortStrength = new("叠加扰动强度", "控制最终产生的2次采样的结果往第1次结果上叠加的程度。");
            
            public static readonly GUIContent disturbanceIntensity = new("扰动强度", "扰动的整体强度。");
            public static readonly GUIContent yAxisStretch = new("Y 轴拉伸", "在屏幕空间对画面进行拉伸处理，效果不如在生成 Mask 阶段那么明显。");
            public static readonly GUIContent gradientIntensity = new("渐变强度", "Mask 图的 g 通道存储的就是渐变的系数，我们可以通过这个强度来控制整体的渐变强度。");
        }

        private SerializedDataParameter m_renderData;
        
        // 遮罩参数 >>>>>>>>>>>>>>>>>>>>>>>
        private SerializedDataParameter m_meshPreview;
        private SerializedDataParameter m_outlineWidth;
        private SerializedDataParameter m_outlinePower;
        private SerializedDataParameter m_outlineFadeStart, m_outlineFadeEnd;
        private SerializedDataParameter m_yAxisOffset;
        private SerializedDataParameter m_invertFadeDirection;
        private SerializedDataParameter m_gradientScale, m_gradientLeft, m_gradientRight, m_gradientPower;
        
        // 后处理参数 >>>>>>>>>>>>>>>>>>>>>>>
        private SerializedDataParameter m_distortTextureTiling, m_distortTextureOffset;
        private SerializedDataParameter m_outlineColor;
        // private SerializedDataParameter m_outlineAlpha;
        
        private SerializedDataParameter m_distortUVScrollSpeed, m_distortScreenScale;
        
        private SerializedDataParameter m_multipleSampleOn, m_offsetSampleUV1, m_offsetSampleTime1, m_offsetSampleUV2, m_offsetSampleTime2, m_appendDistortStrength;
        
        private SerializedDataParameter m_disturbanceIntensity;
        private SerializedDataParameter m_yAxisStretch;
        private SerializedDataParameter m_gradientIntensity;

        public override void OnEnable()
        {
            // base.OnEnable();
            
            var o = new PropertyFetcher<OutlineDistortVolume>(serializedObject);
            
            m_renderData = Unpack(o.Find(x => x.renderData));
            
            // 遮罩参数 >>>>>>>>>>>>>>>>>>>>>>>
            m_meshPreview = Unpack(o.Find(x => x.meshPreview));
            m_outlineWidth = Unpack(o.Find(x => x.outlineWidth));
            m_outlinePower = Unpack(o.Find(x => x.outlinePower));
            
            m_outlineFadeStart = Unpack(o.Find(x => x.outlineFadeStart));
            m_outlineFadeEnd = Unpack(o.Find(x => x.outlineFadeEnd));
            
            m_yAxisOffset = Unpack(o.Find(x => x.yAxisOffset));
            m_invertFadeDirection = Unpack(o.Find(x => x.invertFadeDirection));
            
            m_gradientScale = Unpack(o.Find(x => x.gradientScale));
            m_gradientLeft = Unpack(o.Find(x => x.gradientLeft));
            m_gradientRight = Unpack(o.Find(x => x.gradientRight));
            m_gradientPower = Unpack(o.Find(x => x.gradientPower));
            
            // 后处理参数 >>>>>>>>>>>>>>>>>>>>>>>
            m_distortTextureTiling = Unpack(o.Find(x => x.distortTextureTiling));
            m_distortTextureOffset = Unpack(o.Find(x => x.distortTextureOffset));
            m_outlineColor = Unpack(o.Find(x => x.outlineColor));
            // m_outlineAlpha = Unpack(o.Find(x => x.outlineAlpha));

            m_distortUVScrollSpeed = Unpack(o.Find(x => x.distortUVScrollSpeed));
            m_distortScreenScale = Unpack(o.Find(x => x.distortScreenScale));
            
            m_multipleSampleOn = Unpack(o.Find(x => x.multipleSampleOn));
            m_offsetSampleUV1 = Unpack(o.Find(x => x.offsetSampleUV1));
            m_offsetSampleTime1 = Unpack(o.Find(x => x.offsetSampleTime1));
            m_offsetSampleUV2 = Unpack(o.Find(x => x.offsetSampleUV2));
            m_offsetSampleTime2 = Unpack(o.Find(x => x.offsetSampleTime2));
            m_appendDistortStrength = Unpack(o.Find(x => x.appendDistortStrength));
            
            m_disturbanceIntensity = Unpack(o.Find(x => x.disturbanceIntensity));
            m_yAxisStretch = Unpack(o.Find(x => x.yAxisStretch));
            m_gradientIntensity = Unpack(o.Find(x => x.gradientIntensity));
        }
        
        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            
            EditorGUILayout.LabelField(Styles.header, EditorStyles.whiteLargeLabel);
            
            EditorGUILayout.Space();
            EditorGUILayoutExt.DrawScript(this.target, "脚本");
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(Styles.useTooltip, MessageType.Warning);
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Space();
            PropertyField(m_renderData, Styles.renderData);
            if (EditorGUI.EndChangeCheck())
            {
                ReadMaskMatParameter();
                ReadPSMatParameter();
            }
            
            // 遮罩参数 >>>>>>>>>>>>>>>>>>>>>>>
            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayoutExt.LabelFieldWithVolume(Styles.maskHeader);
                using (new EditorGUI.DisabledScope(!this.IsDrawMask))
                {
                    PropertyField(m_meshPreview, Styles.meshPreview);
                    PropertyField(m_outlineWidth, Styles.outlineWidth);
                    PropertyField(m_outlinePower, Styles.outlinePower);
                    
                    EditorGUILayoutExt.LabelFieldWithVolume(Styles.outlineFadeHeader);
                    using (new EditorGUI.IndentLevelScope(1))
                    {
                        PropertyField(m_outlineFadeStart, Styles.outlineFadeStart);
                        PropertyField(m_outlineFadeEnd, Styles.outlineFadeEnd);
                    }

                    PropertyField(m_yAxisOffset, Styles.yAxisOffset);
                    PropertyField(m_invertFadeDirection, Styles.invertFadeDirection);
                    
                    EditorGUILayoutExt.LabelFieldWithVolume(Styles.gradientHeader);
                    using (new EditorGUI.IndentLevelScope(1))
                    {
                        PropertyField(m_gradientScale, Styles.gradientScale);
                        PropertyField(m_gradientLeft, Styles.gradientLeft);
                        PropertyField(m_gradientRight, Styles.gradientRight);
                        PropertyField(m_gradientPower, Styles.gradientPower);
                    }
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                WriteMaskMatParameter();
            }
            
            // 后处理参数 >>>>>>>>>>>>>>>>>>>>>>>
            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            EditorGUILayoutExt.LabelFieldWithVolume(Styles.postProcessHeader);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayoutExt.LabelFieldWithVolume(Styles.distortTextureHeader);
                using (new EditorGUI.IndentLevelScope(1))
                {
                    PropertyField(m_distortTextureTiling, Styles.distortTextureTiling);
                    PropertyField(m_distortTextureOffset, Styles.distortTextureOffset);
                }
                
                PropertyField(m_outlineColor, Styles.outlineColor);
                // using (new EditorGUI.DisabledScope(true))
                // {
                //     PropertyField(m_outlineAlpha, Styles.outlineAlpha);
                // }
                
                EditorGUILayoutExt.LabelFieldWithVolume(Styles.distortUVDeltaHeader);
                using (new EditorGUI.IndentLevelScope(1))
                {
                    PropertyField(m_distortUVScrollSpeed, Styles.distortUVScrollSpeed);
                    PropertyField(m_distortScreenScale, Styles.distortScreenScale);
                }
                
                EditorGUILayoutExt.LabelFieldWithVolume(Styles.multipleSampleHeader);
                using (new EditorGUI.IndentLevelScope(1))
                {
                    PropertyField(m_multipleSampleOn, Styles.multipleSampleOn);
                    if (m_multipleSampleOn.value.boolValue)
                    {
                        EditorGUILayoutExt.LabelFieldWithVolume(Styles.sample2);
                        using (new EditorGUI.IndentLevelScope(1))
                        {
                            PropertyField(m_offsetSampleUV1, Styles.offsetSampleUV);
                            PropertyField(m_offsetSampleTime1, Styles.offsetSampleTime);
                        }
                        
                        EditorGUILayoutExt.LabelFieldWithVolume(Styles.sample3);
                        using (new EditorGUI.IndentLevelScope(1))
                        {
                            PropertyField(m_offsetSampleUV2, Styles.offsetSampleUV);
                            PropertyField(m_offsetSampleTime2, Styles.offsetSampleTime);
                        }
                        
                        PropertyField(m_appendDistortStrength, Styles.appendDistortStrength);
                    }
                }
                
                PropertyField(m_disturbanceIntensity, Styles.disturbanceIntensity);
                PropertyField(m_yAxisStretch, Styles.yAxisStretch);
                PropertyField(m_gradientIntensity, Styles.gradientIntensity);
            }
            if (EditorGUI.EndChangeCheck())
            {
                WritePSMatParameter();
            }
        }

        private bool IsDrawMask
        {
            get
            {
                // 这里假设了 Feature 一定是挂在默认 Renderer 上的
                var defaultRenderer = XKnightRenderPipeline.asset.scriptableRenderer;
                if (defaultRenderer == null) return false;
                
                var feature = defaultRenderer.GetFeature<OutlineDistortRendererFeature>();
                if (feature == null) return false;
                if (feature.settings == null) return false;
                
                return feature.settings.drawMask;
            }
        }

        private OutlineDistortRenderData GetOutlineDistortRenderData()
        {
            if (!m_renderData.overrideState.boolValue)
            {
                return null;
            }
            
            var dataObj = m_renderData.value.objectReferenceValue as OutlineDistortRenderData;
            return dataObj;
        }
        
        private void ReadMaskMatParameter()
        {
            var dataObj = GetOutlineDistortRenderData();
            if (dataObj == null)
            {
                return;
            }
            
            Material maskMat = dataObj.renderResources.outlineDistortMaskMat;
            if (maskMat == null)
            {
                return;
            }
            
            if (maskMat.HasProperty(OutlineDistortShaderProperties._MeshPreview))
            {
                int val = (int)maskMat.GetFloat(OutlineDistortShaderProperties._MeshPreview);
                m_meshPreview.value.floatValue = val;
            }
            
            if (maskMat.HasProperty(OutlineDistortShaderProperties._OutlineWidth))
            {
                float val = maskMat.GetFloat(OutlineDistortShaderProperties._OutlineWidth);
                m_outlineWidth.value.floatValue = val;
            }
            if (maskMat.HasProperty(OutlineDistortShaderProperties._OutlinePower))
            {
                float val = maskMat.GetFloat(OutlineDistortShaderProperties._OutlinePower);
                m_outlinePower.value.floatValue = val;
            }

            if (maskMat.HasProperty(OutlineDistortShaderProperties._OutlineFadeStart))
            {
                float val = maskMat.GetFloat(OutlineDistortShaderProperties._OutlineFadeStart);
                m_outlineFadeStart.value.floatValue = val;
            }
            if (maskMat.HasProperty(OutlineDistortShaderProperties._OutlineFadeEnd))
            {
                float val = maskMat.GetFloat(OutlineDistortShaderProperties._OutlineFadeEnd);
                m_outlineFadeEnd.value.floatValue = val;
            }

            if (maskMat.HasProperty(OutlineDistortShaderProperties._YAxisOffset))
            {
                float val = maskMat.GetFloat(OutlineDistortShaderProperties._YAxisOffset);
                m_yAxisOffset.value.floatValue = val;
            }

            if (maskMat.HasProperty(OutlineDistortShaderProperties._InvertFadeDirection))
            {
                int val = (int)maskMat.GetFloat(OutlineDistortShaderProperties._InvertFadeDirection);
                m_invertFadeDirection.value.boolValue = val == 1;
            }

            if (maskMat.HasProperty(OutlineDistortShaderProperties._GradientScale))
            {
                float val = maskMat.GetFloat(OutlineDistortShaderProperties._GradientScale);
                m_gradientScale.value.floatValue = val;
            }
            if (maskMat.HasProperty(OutlineDistortShaderProperties._GradientLeft))
            {
                float val = maskMat.GetFloat(OutlineDistortShaderProperties._GradientLeft);
                m_gradientLeft.value.floatValue = val;
            }
            if (maskMat.HasProperty(OutlineDistortShaderProperties._GradientRight))
            {
                float val = maskMat.GetFloat(OutlineDistortShaderProperties._GradientRight);
                m_gradientRight.value.floatValue = val;
            }
            if (maskMat.HasProperty(OutlineDistortShaderProperties._GradientPower))
            {
                float val = maskMat.GetFloat(OutlineDistortShaderProperties._GradientPower);
                m_gradientPower.value.floatValue = val;
            }
        }

        private void ReadPSMatParameter()
        {
            var dataObj = GetOutlineDistortRenderData();
            if (dataObj == null)
            {
                return;
            }
            
            Material psMat = dataObj.renderResources.outlineDistortPSMat;
            if (psMat == null)
            {
                return;
            }
            
            if (psMat.HasProperty(OutlineDistortShaderProperties._DistortTex))
            {
                Vector2 tiling = psMat.GetTextureScale(OutlineDistortShaderProperties._DistortTex);
                m_distortTextureTiling.value.vector2Value = tiling;

                Vector2 offset = psMat.GetTextureOffset(OutlineDistortShaderProperties._DistortTex);
                m_distortTextureOffset.value.vector2Value = offset;
            }

            if (psMat.HasProperty(OutlineDistortShaderProperties._OutlineColor))
            {
                Color val = psMat.GetColor(OutlineDistortShaderProperties._OutlineColor);
                m_outlineColor.value.colorValue = val;
            }
            // if (psMat.HasProperty(OutlineDistortShaderProperties._OutlineAlpha))
            // {
            //     float val = psMat.GetFloat(OutlineDistortShaderProperties._OutlineAlpha);
            //     m_outlineAlpha.value.floatValue = val;
            // }

            if (psMat.HasProperty(OutlineDistortShaderProperties._DistortUVScrollSpeed))
            {
                Vector4 val = psMat.GetVector(OutlineDistortShaderProperties._DistortUVScrollSpeed);
                m_distortUVScrollSpeed.value.vector2Value = new Vector2(val.x, val.y);
            }
            if (psMat.HasProperty(OutlineDistortShaderProperties._DistortScreenScale))
            {
                Vector4 val = psMat.GetVector(OutlineDistortShaderProperties._DistortScreenScale);
                m_distortScreenScale.value.vector2Value = new Vector2(val.x, val.y);
            }
            
            if (psMat.HasProperty(OutlineDistortShaderProperties._MultipleSampleOn))
            {
                int val = (int)psMat.GetFloat(OutlineDistortShaderProperties._MultipleSampleOn);
                m_multipleSampleOn.value.boolValue = val == 1;
            }
            if (psMat.HasProperty(OutlineDistortShaderProperties._OffsetSampleUV))
            {
                Vector4 val = psMat.GetVector(OutlineDistortShaderProperties._OffsetSampleUV);
                m_offsetSampleUV1.value.vector2Value = new Vector2(val.x, val.y);
                m_offsetSampleUV2.value.vector2Value = new Vector2(val.z, val.w);
            }
            if (psMat.HasProperty(OutlineDistortShaderProperties._OffsetSampleTime))
            {
                Vector4 val = psMat.GetVector(OutlineDistortShaderProperties._OffsetSampleTime);
                m_offsetSampleTime1.value.floatValue = val.x;
                m_offsetSampleTime2.value.floatValue = val.y;
            }
            if (psMat.HasProperty(OutlineDistortShaderProperties._AppendDistortStrength))
            {
                float val = psMat.GetFloat(OutlineDistortShaderProperties._AppendDistortStrength);
                m_appendDistortStrength.value.floatValue = val;
            }
            
            if (psMat.HasProperty(OutlineDistortShaderProperties._DisturbanceIntensity))
            {
                float val = psMat.GetFloat(OutlineDistortShaderProperties._DisturbanceIntensity);
                m_disturbanceIntensity.value.floatValue = val;
            }
            if (psMat.HasProperty(OutlineDistortShaderProperties._YAxisStretch))
            {
                float val = psMat.GetFloat(OutlineDistortShaderProperties._YAxisStretch);
                m_yAxisStretch.value.floatValue = val;
            }
            
            if (psMat.HasProperty(OutlineDistortShaderProperties._GradientIntensity))
            {
                float val = psMat.GetFloat(OutlineDistortShaderProperties._GradientIntensity);
                m_gradientIntensity.value.floatValue = val;
            }
        }
        
        private void WriteMaskMatParameter()
        {
            var dataObj = GetOutlineDistortRenderData();
            if (dataObj == null)
            {
                return;
            }
            
            Material maskMat = dataObj.renderResources.outlineDistortMaskMat;
            if (maskMat == null)
            {
                return;
            }

            bool meshPreview = m_meshPreview.value.boolValue;
            if (maskMat.HasProperty(OutlineDistortShaderProperties._MeshPreview))
            {
                maskMat.SetFloat(OutlineDistortShaderProperties._MeshPreview, meshPreview ? 1 : 0);
            }
            CoreUtils.SetKeyword(maskMat, OutlineDistortShaderKeywords._MESH_PREVIEW_MODE, meshPreview);

            if (maskMat.HasProperty(OutlineDistortShaderProperties._OutlineWidth))
            {
                float val = m_outlineWidth.value.floatValue;
                maskMat.SetFloat(OutlineDistortShaderProperties._OutlineWidth, val);
            }
            if (maskMat.HasProperty(OutlineDistortShaderProperties._OutlinePower))
            {
                float val = m_outlinePower.value.floatValue;
                maskMat.SetFloat(OutlineDistortShaderProperties._OutlinePower, val);
            }

            if (maskMat.HasProperty(OutlineDistortShaderProperties._OutlineFadeStart))
            {
                float val = m_outlineFadeStart.value.floatValue;
                maskMat.SetFloat(OutlineDistortShaderProperties._OutlineFadeStart, val);
            }
            if (maskMat.HasProperty(OutlineDistortShaderProperties._OutlineFadeEnd))
            {
                float val = m_outlineFadeEnd.value.floatValue;
                maskMat.SetFloat(OutlineDistortShaderProperties._OutlineFadeEnd, val);
            }

            if (maskMat.HasProperty(OutlineDistortShaderProperties._YAxisOffset))
            {
                float val = m_yAxisOffset.value.floatValue;
                maskMat.SetFloat(OutlineDistortShaderProperties._YAxisOffset, val);
            }

            if (maskMat.HasProperty(OutlineDistortShaderProperties._InvertFadeDirection))
            {
                float val = m_invertFadeDirection.value.boolValue ? 1 : 0;
                maskMat.SetFloat(OutlineDistortShaderProperties._InvertFadeDirection, val);
            }

            if(maskMat.HasProperty(OutlineDistortShaderProperties._GradientScale))
            {
                float val = m_gradientScale.value.floatValue;
                maskMat.SetFloat(OutlineDistortShaderProperties._GradientScale, val);
            }
            if (maskMat.HasProperty(OutlineDistortShaderProperties._GradientLeft))
            {
                float val = m_gradientLeft.value.floatValue;
                maskMat.SetFloat(OutlineDistortShaderProperties._GradientLeft, val);
            }
            if (maskMat.HasProperty(OutlineDistortShaderProperties._GradientRight))
            {
                float val = m_gradientRight.value.floatValue;
                maskMat.SetFloat(OutlineDistortShaderProperties._GradientRight, val);
            }
            if (maskMat.HasProperty(OutlineDistortShaderProperties._GradientPower))
            {
                float val = m_gradientPower.value.floatValue;
                maskMat.SetFloat(OutlineDistortShaderProperties._GradientPower, val);
            }
        }

        private void WritePSMatParameter()
        {
            var dataObj = GetOutlineDistortRenderData();
            if (dataObj == null)
            {
                return;
            }
            
            Material psMat = dataObj.renderResources.outlineDistortPSMat;
            if (psMat == null)
            {
                return;
            }
            
            if (psMat.HasProperty(OutlineDistortShaderProperties._DistortTex))
            {
                Vector2 tiling = m_distortTextureTiling.value.vector2Value;
                psMat.SetTextureScale(OutlineDistortShaderProperties._DistortTex, tiling);
            }
            if (psMat.HasProperty(OutlineDistortShaderProperties._DistortTex))
            {
                Vector2 offset = m_distortTextureOffset.value.vector2Value;
                psMat.SetTextureOffset(OutlineDistortShaderProperties._DistortTex, offset);
            }
            
            
            if (psMat.HasProperty(OutlineDistortShaderProperties._OutlineColor))
            {
                Color color = m_outlineColor.value.colorValue;
                psMat.SetColor(OutlineDistortShaderProperties._OutlineColor, color);
            }
            
            // if (m_outlineDistortPSMat.HasProperty(OutlineDistortShaderProperties._OutlineAlpha))
            // {
            //     float val = m_volume.outlineAlpha.value;
            //     m_outlineDistortPSMat.SetFloat(OutlineDistortShaderProperties._OutlineAlpha, val);
            // }

            if (psMat.HasProperty(OutlineDistortShaderProperties._DistortUVScrollSpeed))
            {
                Vector2 distortUVScrollSpeed = m_distortUVScrollSpeed.value.vector2Value;
                psMat.SetVector(OutlineDistortShaderProperties._DistortUVScrollSpeed, new Vector4(distortUVScrollSpeed.x, distortUVScrollSpeed.y, 0, 0));
            }

            if (psMat.HasProperty(OutlineDistortShaderProperties._DistortScreenScale))
            {
                Vector2 distortScreenScale = m_distortScreenScale.value.vector2Value;
                psMat.SetVector(OutlineDistortShaderProperties._DistortScreenScale, new Vector4(distortScreenScale.x, distortScreenScale.y, 0, 0));
            }
            
            // 需要累加的参数都在帧 tick 里完成赋值
            
            // 多次采样相关
            bool multipleSampleOn = m_multipleSampleOn.value.boolValue;
            if (psMat.HasProperty(OutlineDistortShaderProperties._MultipleSampleOn))
            {
                psMat.SetFloat(OutlineDistortShaderProperties._MultipleSampleOn, multipleSampleOn ? 1 : 0);
            }
            CoreUtils.SetKeyword(psMat, OutlineDistortShaderKeywords._MULTIPLE_SAMPLE_ON, multipleSampleOn);
            
            if (psMat.HasProperty(OutlineDistortShaderProperties._OffsetSampleUV))
            {
                Vector2 uv2 = m_offsetSampleUV1.value.vector2Value;
                Vector2 uv3 = m_offsetSampleUV2.value.vector2Value;
                Vector4 val = new Vector4(uv2.x, uv2.y, uv3.x, uv3.y);
                psMat.SetVector(OutlineDistortShaderProperties._OffsetSampleUV, val);
            }

            if (psMat.HasProperty(OutlineDistortShaderProperties._OffsetSampleTime))
            {
                float time2 = m_offsetSampleTime1.value.floatValue;
                float time3 = m_offsetSampleTime2.value.floatValue;
                Vector4 val = new Vector4(time2, time3, 0, 0);
                psMat.SetVector(OutlineDistortShaderProperties._OffsetSampleTime, val);
            }

            if (psMat.HasProperty(OutlineDistortShaderProperties._AppendDistortStrength))
            {
                float val = m_appendDistortStrength.value.floatValue;
                psMat.SetFloat(OutlineDistortShaderProperties._AppendDistortStrength, val);
            }
            
            
            if (psMat.HasProperty(OutlineDistortShaderProperties._DisturbanceIntensity))
            {
                float val = m_disturbanceIntensity.value.floatValue;
                psMat.SetFloat(OutlineDistortShaderProperties._DisturbanceIntensity, val);
            }
            
            if (psMat.HasProperty(OutlineDistortShaderProperties._YAxisStretch))
            {
                float val = m_yAxisStretch.value.floatValue;
                psMat.SetFloat(OutlineDistortShaderProperties._YAxisStretch, val);
            }
            
            if (psMat.HasProperty(OutlineDistortShaderProperties._GradientIntensity))
            {
                float val = m_gradientIntensity.value.floatValue;
                psMat.SetFloat(OutlineDistortShaderProperties._GradientIntensity, val);
            }
        }
        
    }
}