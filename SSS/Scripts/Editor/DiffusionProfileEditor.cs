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

        //====================== properties ============================
        private SerializedProperty scatteringColorProp;
        private SerializedProperty scatteringMultiplierProp;

        private SerializedProperty maxRadiusProp;
        private SerializedProperty worldScaleProp;

        private SerializedProperty kernelSampleCountProp;
        
        public SerializedProperty Fresnel0Prop;
         public SerializedProperty FresnelScaleProp;
         public SerializedProperty TransmissionColorProp;
         public SerializedProperty ThicknessRemapMinProp;
         public SerializedProperty ThicknessRemapMaxProp;

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
            FresnelScaleProp = serializedObject.FindProperty(" FresnelScale");
            TransmissionColorProp = serializedObject.FindProperty("TransmissionColor");
            ThicknessRemapMinProp = serializedObject.FindProperty("ThicknessRemapMin");
            ThicknessRemapMaxProp = serializedObject.FindProperty("ThicknessRemapMax");
            
            
            GetOrCreateDiscPreviewMaterial();
            // Ensure preview is generated immediately on selection
            DiscPreviewByShader((DiffusionProfileParam)target);
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            Debug.Log("DiffusionProfileEditor enabled and disc preview material created.");
        }

        private void OnDisable()
        {
        }

        private bool showDebug = false;

        public override void OnInspectorGUI()
        {
            _styles ??= new Styles();

            serializedObject.Update();
            showDebug = EditorGUILayout.Foldout(showDebug, "Debug Options", true);

            var asset = (DiffusionProfileParam)target;

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

            EditorGUILayout.PropertyField(worldScaleProp, _styles.ProfileWorldScale);
            EditorGUILayout.PropertyField(worldScaleProp, _styles.ProfileWorldScale);//Fresnel0
            
            
            serializedObject.ApplyModifiedProperties();

            asset.updateKernel();

            DiscPreviewByShader(asset);

            if (asset.discPreviewTexture != null)
                EditorUtility.SetDirty(asset.discPreviewTexture);
            if (showDebug)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.FloatField(_styles.hash, asset.hash);
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.FloatField(_styles.DefaultStyle("hash"), asset.hash);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generated Preview", EditorStyles.boldLabel);
            GetOrCreateDiscPreviewMaterial().SetFloat("_MaxRadius", asset.InputMaxRadius);
            GetOrCreateDiscPreviewMaterial().SetVector("_ShapeParam", asset.InputShape / asset.InputShape.w);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(4);
            Rect r = GUILayoutUtility.GetRect(DiscPreviewSize, DiscPreviewSize, GUILayout.ExpandWidth(false));
            EditorGUI.DrawPreviewTexture(r, asset.discPreviewTexture, GetOrCreateDiscPreviewMaterial(),
                ScaleMode.ScaleToFit, 1f);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            DrawTexture2DPreview("Disc Kernel", asset.discKernelTex);

            if (_kernelNeedsUpdate)
                EditorGUILayout.HelpBox("Burley parameters changed. Click update to rebuild Disc Kernel textures.",
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
            public readonly GUIContent ProfilePreview0 = new("Diffusion Profile Preview");

            public readonly GUIContent ProfilePreview1 =
                new("Shows the fraction of light scattered from the source (center).");

            public readonly GUIContent ProfilePreview2 =
                new("The distance to the boundary of the image corresponds to the Max Radius.");

            public readonly GUIContent TransmittancePreview0 = new("Transmittance Preview");

            public readonly GUIContent TransmittancePreview1 =
                new("Shows the fraction of light passing through the object for thickness values from the remap.");

            public readonly GUIContent TransmittancePreview2 =
                new("Can be viewed as a cross section of a slab of material illuminated by white light from the left.");

            public readonly GUIContent ProfileScatteringColor = new("散射颜色",
                "确定散射的shape，用于算出哪种颜色的波被散射出去");

            public readonly GUIContent ScatteringMultiplier = new("散射强度",
                "乘以颜色，表示物体的散射吸收率");

            public readonly GUIContent ProfileTransmissionTint = new("Transmission tint",
                "Color which tints transmitted light. Alpha is ignored.");

            public readonly GUIContent ProfileMaxRadius = new("散射径长",
                "光线从表面进入后，散射的弦长，以毫米为单位。这个值越大，散射越明显，物体看起来越厚重。");


            public readonly GUIContent ProfileMinMaxThickness = new("Thickness Remap Values (Min-Max)",
                "Shows the values of the thickness remap below (in millimeters).");

            public readonly GUIContent ProfileThicknessRemap = new("Thickness Remap (Min-Max)",
                "Remaps the thickness parameter from [0, 1] to the desired range (in millimeters).");

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