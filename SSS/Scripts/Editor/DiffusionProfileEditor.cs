using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Garena.TA.SSS
{
    [CustomEditor(typeof(DiffusionProfileParam))]
    public sealed class DiffusionProfileEditor : Editor
    {
        private const int DiscPreviewSize = 256;
        private const string DiscPreviewShaderName = "Hidden/DrawDiffusionProfile";
        private bool _kernelNeedsUpdate;
        private Material _discPreviewMaterial;

        private Material _TransmistPreviewMaterial;

        //====================== properties ============================
        private SerializedProperty scatteringColorProp;
        private SerializedProperty scatteringMultiplierProp;

        private SerializedProperty maxRadiusProp;
        private SerializedProperty worldScaleProp;

        private SerializedProperty kernelSampleCountProp;

        public SerializedProperty Fresnel0Prop;
        public SerializedProperty FresnelScaleProp;
        public SerializedProperty TransmissionTintProp;
        public SerializedProperty ThickOffsetProp;

        //==========================Editor properties ====================
        private static Styles _styles;

        private void OnEnable()
        {
            _styles ??= new Styles();
            scatteringColorProp = serializedObject.FindProperty("scatteringColor");
            scatteringMultiplierProp = serializedObject.FindProperty("scatteringMultiplier");
            maxRadiusProp = serializedObject.FindProperty("maxRadius");
            worldScaleProp = serializedObject.FindProperty("worldScale");
            kernelSampleCountProp = serializedObject.FindProperty("kernelSampleCount");

            Fresnel0Prop = serializedObject.FindProperty("Fresnel0");
            FresnelScaleProp = serializedObject.FindProperty("FresnelScale");
            TransmissionTintProp = serializedObject.FindProperty("TransmissionTint");
            ThickOffsetProp = serializedObject.FindProperty("ThickOffset");

            GetOrCreateDiscPreviewMaterial();
            // Ensure preview is generated immediately on selection
            DiscPreviewByShader((DiffusionProfileParam)target);
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            // Debug.Log("DiffusionProfileEditor enabled and disc preview material created.");
        }


        private bool showDebug = false;

        public override void OnInspectorGUI()
        {
            _styles ??= new Styles();

            serializedObject.Update();
            showDebug = EditorGUILayout.Foldout(showDebug, "Debug Options", true);

            var asset = (DiffusionProfileParam)target;
            
            if (showDebug)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.Vector4Field(_styles.DefaultStyle("shape"), asset.InputShape);
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.FloatField(_styles.DefaultStyle("WroldScale"), asset.worldScale);
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.FloatField(_styles.DefaultStyle("hash"), asset.hash);
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.Vector4Field(_styles.DefaultStyle("InputThicknessRemap"),
                        asset.InputThicknessRemap);
            }

            using (var cc = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(scatteringColorProp, _styles.ProfileScatteringColor);
                EditorGUILayout.PropertyField(scatteringMultiplierProp, _styles.ScatteringMultiplier);
                EditorGUILayout.PropertyField(maxRadiusProp, _styles.ProfileMaxRadius);
                EditorGUILayout.PropertyField(kernelSampleCountProp, _styles.ProfileKernelSampleCount);
                

                if (cc.changed)
                {
                    _kernelNeedsUpdate = true;
                }
            }

            // Transmittance properties
            EditorGUILayout.PropertyField(Fresnel0Prop, _styles.Fresnel0Prop);
            EditorGUILayout.PropertyField(FresnelScaleProp, _styles.FresnelScaleProp);
            EditorGUILayout.PropertyField(TransmissionTintProp, _styles.ProfileTransmissionTint);
            EditorGUILayout.Slider(ThickOffsetProp,0f,2f, _styles.ThickOffset);
            EditorGUILayout.BeginHorizontal();
            // EditorGUILayout.PropertyField(ThicknessRemapMinProp, new GUIContent("Thickness Remap Min"));
            // EditorGUILayout.PropertyField(ThicknessRemapMaxProp, new GUIContent("Thickness Remap Max"));
            EditorGUILayout.MinMaxSlider(_styles.ProfileThicknessRemap, ref asset.ThicknessRemapMin,
                ref asset.ThicknessRemapMax, 0f, 2f);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(worldScaleProp, _styles.ProfileWorldScale);


            serializedObject.ApplyModifiedProperties();

            asset.updateKernel();

            DiscPreviewByShader(asset);

            if (asset.discPreviewTexture != null)
                EditorUtility.SetDirty(asset.discPreviewTexture);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generated Preview", EditorStyles.boldLabel);

            DrawDiffuseionProffileGraph(asset);
            DrawTransmistPreview(asset);
            DrawPreview(asset);
            DrawTexture2DPreview("Disc Kernel", asset.discKernelTex);

            if (_kernelNeedsUpdate)
                EditorGUILayout.HelpBox("采样核的参数被改变，需要重新生成采样核贴图.",
                    MessageType.Warning);

            if (GUILayout.Button("Update Disc Kernel Texture"))
            {
                RegenerateDiscKernel();
                _kernelNeedsUpdate = false;
            }
        }


        private void DiscPreviewByShader(DiffusionProfileParam asset)
        {
            if (asset == null)
                return;

            Material material = GetOrCreateDiscPreviewMaterial();
            if (material == null)
            {
                Debug.LogWarning($"[SSS] Cannot find shader '{DiscPreviewShaderName}', skip disc preview update.");
                return;
            }

            if (asset.discPreviewTexture == null)
            {
                var newPreview = new RenderTexture(DiscPreviewSize, DiscPreviewSize, 0,
                    GraphicsFormat.R16G16B16A16_SFloat);
                ReplaceSubAssetTexture(asset, ref asset.discPreviewTexture, newPreview, "DiscPreview");
            }

            if (asset.TransmistPreviewTexture == null)
            {
                var newPreview = new RenderTexture(DiscPreviewSize, 28, 0,
                    GraphicsFormat.R16G16B16A16_SFloat);
                ReplaceSubAssetTexture(asset, ref asset.TransmistPreviewTexture, newPreview, "TransmitPreview");
            }
        }

        private void DrawDiffuseionProffileGraph(DiffusionProfileParam asset)
        {
            GetOrCreateDiscPreviewMaterial().SetFloat("_MaxRadius", asset.InputMaxRadius);
            GetOrCreateDiscPreviewMaterial().SetVector("_ShapeParam", asset.InputShape / asset.InputShape.w);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(4);
        }

        private void DrawPreview(DiffusionProfileParam asset)
        {
            Rect r = GUILayoutUtility.GetRect(DiscPreviewSize, DiscPreviewSize, GUILayout.ExpandWidth(false));
            EditorGUI.DrawPreviewTexture(r, asset.discPreviewTexture, GetOrCreateDiscPreviewMaterial(),
                ScaleMode.ScaleToFit, 1f);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generated Transmit", EditorStyles.boldLabel);

            if (asset.TransmistPreviewTexture != null)
            {
                float aspect = asset.TransmistPreviewTexture.width / (float)asset.TransmistPreviewTexture.height;
                // Use a rect that expands to the inspector width but keeps a fixed height for a long strip
                Rect r1 = GUILayoutUtility.GetRect(0, 48, GUILayout.ExpandWidth(true));
                EditorGUI.DrawPreviewTexture(r1, asset.TransmistPreviewTexture, GetOrCreateTransmitMaterial(),
                    ScaleMode.ScaleToFit, aspect);
            }
        }

        private void DrawTransmistPreview(DiffusionProfileParam asset)
        {
            var mat = GetOrCreateTransmitMaterial();
            mat.SetVector("_ShapeParams", asset.InputShape / asset.InputShape.w);
            mat.SetVector("_TransmissionTint", asset.InputTransmissionTint);
            mat.SetVector("_ThicknessRemap", asset.InputThicknessRemap);
            // Thickness remap is edited via the Thickness Remap Min/Max properties above in the inspector.
        }

        private Material GetOrCreateDiscPreviewMaterial()
        {
            if (_discPreviewMaterial != null)
                return _discPreviewMaterial;
            Shader shader = Shader.Find(DiscPreviewShaderName);
            if (shader == null)
                return null;

            _discPreviewMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            return _discPreviewMaterial;
        }

        private Material GetOrCreateTransmitMaterial()
        {
            if (_TransmistPreviewMaterial != null)
                return _TransmistPreviewMaterial;
            Shader shader = Shader.Find("Hidden/DrawTransmittance");
            if (shader == null)
                return null;

            _TransmistPreviewMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            return _TransmistPreviewMaterial;
        }

        private static void ReplaceSubAssetTexture(DiffusionProfileParam asset, ref RenderTexture current,
            RenderTexture replacement, string name)
        {
            if (current != null)
            {
                AssetDatabase.RemoveObjectFromAsset(current);
                GameObject.DestroyImmediate(current, true);
            }

            current = replacement;
            if (current == null)
                return;

            current.name = name;
            if (AssetDatabase.Contains(asset))
                AssetDatabase.AddObjectToAsset(current, asset);
        }

        private static void ReplaceSubAssetTexture2D(DiffusionProfileParam asset, ref Texture2D current,
            Texture2D replacement, string name)
        {
            if (current != null)
            {
                AssetDatabase.RemoveObjectFromAsset(current);
                GameObject.DestroyImmediate(current, true);
            }

            current = replacement;
            if (current == null)
                return;

            current.name = name;
            if (AssetDatabase.Contains(asset))
                AssetDatabase.AddObjectToAsset(current, asset);
        }

        private void DrawTexture2DPreview(string label, Texture texture)
        {
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);

            if (texture == null)
            {
                EditorGUILayout.HelpBox("Not assigned.", MessageType.Info);
                return;
            }

            float size = Mathf.Min(EditorGUIUtility.currentViewWidth - 40f, 256f);
            var rect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));
            EditorGUI.DrawPreviewTexture(rect, texture);
        }

        private void RegenerateDiscKernel()
        {
            var asset = (DiffusionProfileParam)target;
            asset.updateKernel();

            BurleyParameters burleyParams = new BurleyParameters();
            burleyParams._maxRadius = asset.InputMaxRadius;
            burleyParams._scatteringColor = asset.scatteringColor.linear;
            burleyParams._scatteringMultiplier = asset.scatteringMultiplier;

            Texture2D tempTexture = SSS_DiscSampling.GenerateDiscKernel(burleyParams, asset.InputDiscSampleCount);


            ReplaceSubAssetTexture2D(asset, ref asset.discKernelTex, tempTexture, "DiscKernel");


            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        private sealed class Styles
        {
            public readonly GUIContent Fresnel0Prop = new("边缘系数");
            public readonly GUIContent FresnelScaleProp = new("边缘缩放值,表现光滑");

            public readonly GUIContent TransmittancePreview1 =
                new("Shows the fraction of light passing through the object for thickness values from the remap.");

            public readonly GUIContent TransmittancePreview2 =
                new("Can be viewed as a cross section of a slab of material illuminated by white light from the left.");

            public readonly GUIContent ProfileScatteringColor = new("散射颜色",
                "确定散射的shape，用于算出哪种颜色的波被散射出去");

            public readonly GUIContent ScatteringMultiplier = new("散射强度",
                "乘以颜色，表示物体的散射吸收率");

            public readonly GUIContent ProfileTransmissionTint = new("透射SSS颜色",
                "透色颜色和shape值相关");

            public readonly GUIContent ProfileMaxRadius = new("散射径长",
                "光线从表面进入后，散射的弦长，以毫米为单位。这个值越大，散射越明显，物体看起来越厚重。");


            public readonly GUIContent ThickOffset = new("光照偏移值(背光的偏移)",
                "展示如何表现厚边缘");

            public readonly GUIContent ProfileThicknessRemap = new("厚度重采样 (最小-最大)",
                "重新采样厚度空间从 [0, 1] (in millimeters).");

            public readonly GUIContent hash = new("hash",
                "hash值");

            public readonly GUIContent ProfileWorldScale = new("世界单位1米", "世界的长度尺寸单位单位是米，也就是多少个单位对应shader中的1000毫米");

            public readonly GUIContent ProfileKernelSampleCount = new("核采样像素",
                "怎样计算光线进入物体后的采样cdf，越大越精确，但计算越慢。");

            public readonly GUIStyle CenteredMiniBoldLabel = new(GUI.skin.label);

            public GUIContent DefaultStyle([System.Runtime.CompilerServices.CallerMemberName] string name = null)
            {
                GUIContent temp = new(name, name);
                return temp;
            }
            // public readonly GUIContent SubsurfaceScatteringLabel = new("Subsurface Scattering only");
            //
            // public readonly GUIContent TransmissionLabel = new("Transmission only");

            public Styles()
            {
                CenteredMiniBoldLabel.alignment = TextAnchor.MiddleCenter;
                CenteredMiniBoldLabel.fontSize = 10;
                CenteredMiniBoldLabel.fontStyle = FontStyle.Bold;
            }
        }
    }
}