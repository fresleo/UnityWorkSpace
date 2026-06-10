using System;
using UnityEditor;
using UnityEngine;

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

        //==========================Editor properties ====================
        private static Styles _styles;
        private Vector4 ShapeParam = new Vector4();

        private void OnEnable()
        {
            _styles ??= new Styles();
            scatteringColorProp = serializedObject.FindProperty("scatteringColor");
            scatteringMultiplierProp = serializedObject.FindProperty("scatteringMultiplier");
            GetOrCreateDiscPreviewMaterial();
        }

        private void OnDisable()
        {
            if (_discPreviewMaterial != null)
            {
                DestroyImmediate(_discPreviewMaterial);
                _discPreviewMaterial = null;
            }
        }

        public override void OnInspectorGUI()
        {
            _styles ??= new Styles();

            serializedObject.Update();
            var asset = (DiffusionProfileParam)target;


            using (var cc = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(scatteringColorProp, _styles.ProfileScatteringColor);
                EditorGUILayout.PropertyField(scatteringMultiplierProp, _styles.ScatteringMultiplier);
                if (cc.changed)
                {
                    asset.updateKernel();
                    Debug.Log("ShapeParam: " + asset.shape);
                    Debug.Log("_MaxRadius: " + asset.inputMaxRadius);
                    GetOrCreateDiscPreviewMaterial().SetVector("_ShapeParam", asset.shape);
                    GetOrCreateDiscPreviewMaterial().SetFloat("_MaxRadius", asset.inputMaxRadius);
                }
            }

            

            serializedObject.ApplyModifiedProperties();


            DiscPreviewByShader(asset);

            _kernelNeedsUpdate = true;
            EditorUtility.SetDirty(asset);
            if (asset.discPreviewTexture != null)
                EditorUtility.SetDirty(asset.discPreviewTexture);


            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generated Preview", EditorStyles.boldLabel);
            DrawTexturePreview("Disc Kernel", asset.discKernelTex);
            DrawTexturePreview("Disc Preview (Realtime)", asset.discPreviewTexture);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Disc Kernel Texture", EditorStyles.boldLabel);

            if (_kernelNeedsUpdate)
                EditorGUILayout.HelpBox("Burley parameters changed. Click update to rebuild Disc Kernel textures.",
                    MessageType.Warning);

            if (GUILayout.Button("Update Disc Kernel Texture"))
            {
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

            material.SetVector("_ShapeParam", BuildShapeParamFromBurley(asset));
            material.SetFloat("_MaxRadius", Mathf.Max(1e-4f, asset.maxRadius));

            if (asset.discPreviewTexture == null
                || asset.discPreviewTexture.width != DiscPreviewSize
                || asset.discPreviewTexture.height != DiscPreviewSize
                || asset.discPreviewTexture.format != TextureFormat.RGBAFloat)
            {
                var newPreview = new Texture2D(DiscPreviewSize, DiscPreviewSize, TextureFormat.RGBAFloat, false, true)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
                ReplaceSubAssetTexture(asset, ref asset.discPreviewTexture, newPreview, "DiscPreview");
            }

            var rt = RenderTexture.GetTemporary(DiscPreviewSize, DiscPreviewSize, 0, RenderTextureFormat.ARGBFloat,
                RenderTextureReadWrite.Linear);
            var previous = RenderTexture.active;
            try
            {
                Graphics.Blit(Texture2D.blackTexture, rt, material);
                RenderTexture.active = rt;
                asset.discPreviewTexture.ReadPixels(new Rect(0, 0, DiscPreviewSize, DiscPreviewSize), 0, 0);
                asset.discPreviewTexture.Apply(false, false);
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
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

        private static Vector4 BuildShapeParamFromBurley(DiffusionProfileParam asset)
        {
            float aR = Mathf.Max(0f, asset.scatteringColor.r * asset.scatteringMultiplier);
            float aG = Mathf.Max(0f, asset.scatteringColor.g * asset.scatteringMultiplier);
            float aB = Mathf.Max(0f, asset.scatteringColor.b * asset.scatteringMultiplier);

            float sR = ComputeShapeParam(aR);
            float sG = ComputeShapeParam(aG);
            float sB = ComputeShapeParam(aB);

            // Shader squares _ShapeParam before EvalBurleyDiffusionProfile.
            return new Vector4(Mathf.Sqrt(Mathf.Max(sR, 0f)), Mathf.Sqrt(Mathf.Max(sG, 0f)),
                Mathf.Sqrt(Mathf.Max(sB, 0f)), 0f);
        }

        private static float ComputeShapeParam(float albedo)
        {
            float diff = albedo - 0.8f;
            return 1.85f - albedo + 7.0f * diff * diff * diff;
        }

        private static void ReplaceSubAssetTexture(DiffusionProfileParam asset, ref Texture2D current,
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

        private static void DrawTexturePreview(string label, Texture texture)
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

        private sealed class Styles
        {
            public readonly GUIContent ProfilePreview0 = new("Diffusion Profile Preview");

            public readonly GUIContent ProfilePreview1 =
                new("Shows the fraction of light scattered from the source (center).");

            public readonly GUIContent ProfilePreview2 =
                new("The distance to the boundary of the image corresponds to the Max Radius.");

            public readonly GUIContent ProfilePreview3 =
                new("Note that the intensity of pixels around the center may be clipped.");

            public readonly GUIContent TransmittancePreview0 = new("Transmittance Preview");

            public readonly GUIContent TransmittancePreview1 =
                new("Shows the fraction of light passing through the object for thickness values from the remap.");

            public readonly GUIContent TransmittancePreview2 =
                new("Can be viewed as a cross section of a slab of material illuminated by white light from the left.");

            public readonly GUIContent ProfileScatteringColor = new("散射颜色",
                "确定散射的shape，用于算出哪种颜色的波被散射出去");

            public readonly GUIContent ScatteringMultiplier = new("颜色乘量",
                "乘以颜色，表示物体的散射吸收率");

            public readonly GUIContent ProfileTransmissionTint = new("Transmission tint",
                "Color which tints transmitted light. Alpha is ignored.");

            public readonly GUIContent ProfileMaxRadius = new("Max Radius",
                "The maximum radius of the effect you define in Scattering Color and Multiplier.\nWhen the world scale is 1, this value is in millimeters.");


            public readonly GUIContent ProfileMinMaxThickness = new("Thickness Remap Values (Min-Max)",
                "Shows the values of the thickness remap below (in millimeters).");

            public readonly GUIContent ProfileThicknessRemap = new("Thickness Remap (Min-Max)",
                "Remaps the thickness parameter from [0, 1] to the desired range (in millimeters).");

            public readonly GUIContent ProfileWorldScale = new("World Scale", "Size of the world unit in meters.");

            public readonly GUIContent ProfileIor = new("Index of Refraction",
                "Select the index of refraction for this Diffusion Profile. For reference, skin is 1.4 and most materials are between 1.3 and 1.5.");

            public readonly GUIStyle CenteredMiniBoldLabel = new(GUI.skin.label);

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