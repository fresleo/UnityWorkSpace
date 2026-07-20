using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace XKnight.Glass.Editor
{
    /// <summary>
    /// FakeGlass 材质面板（汉化 + 折叠分组）
    /// </summary>
    public class FakeGlassShaderGUI : UnityEditor.ShaderGUI
    {
        private const string PropMappingType = "_MappingType";
        private const string PropBaseColor = "_BaseColor";
        private const string PropBaseMap = "_BaseMap";

        private const string PropRoomTex = "_RoomTex";
        private const string PropRoomTintColor = "_RoomTintColor";
        private const string PropRooms = "_Rooms";
        private const string PropRoomDepth = "_RoomDepth";
        private const string PropParallaxDepth = "_ParallaxDepth";

        private const string PropFrostBlendOn = "_FrostBlendOn";
        private const string PropFrostedRoomTex = "_FrostedRoomTex";
        private const string PropFrostMask = "_FrostMask";
        private const string PropFrostStrength = "_FrostStrength";

        private const string PropNormalMapOn = "_NormalMapOn";
        private const string PropNormalMap = "_NormalMap";
        private const string PropNormalScale = "_NormalScale";

        private const string PropMetallic = "_Metallic";
        private const string PropSmoothness = "_Smoothness";

        private const string PropEmissionColor = "_EmissionColor";
        private const string PropEmissionStrength = "_EmissionStrength";

        private const string KwMappingParallax = "_MAPPING_PARALLAX";
        private const string KwFrostBlendOn = "_FROST_BLEND_ON";
        private const string KwNormalMapOn = "_NORMAL_MAP_ON";

        private static bool s_FoldBase = true;
        private static bool s_FoldRoom = true;
        private static bool s_FoldFrost = true;
        private static bool s_FoldNormal = true;
        private static bool s_FoldPbr = true;
        private static bool s_FoldEmission = true;

        private static readonly GUIContent LabelBaseHeader = new("基础");
        private static readonly GUIContent LabelBaseColor = new("基础颜色");
        private static readonly GUIContent LabelBaseMap = new("基础贴图");

        private static readonly GUIContent LabelRoomHeader = new("室内");
        private static readonly GUIContent LabelMappingType = new("映射类型");
        private static readonly string[] MappingTypeOptions = { "室内映射", "视差映射" };
        private static readonly GUIContent LabelRoomTex = new("室内贴图");
        private static readonly GUIContent LabelRoomTintColor = new("室内颜色");
        private static readonly GUIContent LabelRooms = new("室内图集 行列 (XY)");
        private static readonly GUIContent LabelRoomDepth = new("室内深度");
        private static readonly GUIContent LabelParallaxDepth = new("视差深度");

        private static readonly GUIContent LabelFrostHeader = new("毛玻璃");
        private static readonly GUIContent LabelFrostBlendOn = new("开启毛玻璃混合");
        private static readonly GUIContent LabelFrostedRoomTex = new("室内模糊贴图");
        private static readonly GUIContent LabelFrostMask = new("毛玻璃遮罩");
        private static readonly GUIContent LabelFrostStrength = new("毛玻璃强度");

        private static readonly GUIContent LabelNormalHeader = new("法线");
        private static readonly GUIContent LabelNormalMapOn = new("开启法线贴图");
        private static readonly GUIContent LabelNormalMap = new("法线贴图");
        private static readonly GUIContent LabelNormalScale = new("法线强度");

        private static readonly GUIContent LabelPbrHeader = new("PBR");
        private static readonly GUIContent LabelMetallic = new("金属度");
        private static readonly GUIContent LabelSmoothness = new("光滑度");

        private static readonly GUIContent LabelEmissionHeader = new("自发光");
        private static readonly GUIContent LabelEmissionColor = new("自发光颜色");
        private static readonly GUIContent LabelEmissionStrength = new("自发光强度");

        private enum MappingType
        {
            InteriorMapping = 0,
            ParallaxMapping = 1,
        }

        public override void ValidateMaterial(Material material)
        {
            SetupMaterialKeywords(material);
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            if (materialEditor == null || properties == null)
                return;

            var pMappingType = FindProperty(PropMappingType, properties, false);
            var pBaseColor = FindProperty(PropBaseColor, properties, false);
            var pBaseMap = FindProperty(PropBaseMap, properties, false);

            var pRoomTex = FindProperty(PropRoomTex, properties, false);
            var pRoomTintColor = FindProperty(PropRoomTintColor, properties, false);
            var pRooms = FindProperty(PropRooms, properties, false);
            var pRoomDepth = FindProperty(PropRoomDepth, properties, false);
            var pParallaxDepth = FindProperty(PropParallaxDepth, properties, false);

            var pFrostBlendOn = FindProperty(PropFrostBlendOn, properties, false);
            var pFrostedRoomTex = FindProperty(PropFrostedRoomTex, properties, false);
            var pFrostMask = FindProperty(PropFrostMask, properties, false);
            var pFrostStrength = FindProperty(PropFrostStrength, properties, false);

            var pNormalMapOn = FindProperty(PropNormalMapOn, properties, false);
            var pNormalMap = FindProperty(PropNormalMap, properties, false);
            var pNormalScale = FindProperty(PropNormalScale, properties, false);

            var pMetallic = FindProperty(PropMetallic, properties, false);
            var pSmoothness = FindProperty(PropSmoothness, properties, false);

            var pEmissionColor = FindProperty(PropEmissionColor, properties, false);
            var pEmissionStrength = FindProperty(PropEmissionStrength, properties, false);

            bool mixedMapping = pMappingType != null && pMappingType.hasMixedValue;
            int mappingType = 0;
            if (!mixedMapping && pMappingType != null)
                mappingType = Mathf.Clamp(Mathf.RoundToInt(pMappingType.floatValue), 0, 1);

            bool mixedFrost = pFrostBlendOn != null && pFrostBlendOn.hasMixedValue;
            bool frostEnabled = mixedFrost || (pFrostBlendOn != null && pFrostBlendOn.floatValue > 0.5f);

            bool mixedNormal = pNormalMapOn != null && pNormalMapOn.hasMixedValue;
            bool normalEnabled = mixedNormal || (pNormalMapOn != null && pNormalMapOn.floatValue > 0.5f);

            DrawBase(materialEditor, pBaseColor, pBaseMap);

            EditorGUILayout.Space(4);
            DrawRoom(materialEditor, pMappingType, pRoomTex, pRoomTintColor, pRooms, pRoomDepth, pParallaxDepth, mixedMapping, mappingType);

            EditorGUILayout.Space(4);
            DrawFrost(materialEditor, pFrostBlendOn, pFrostedRoomTex, pFrostMask, pFrostStrength, frostEnabled);

            EditorGUILayout.Space(4);
            DrawNormal(materialEditor, pNormalMapOn, pNormalMap, pNormalScale, normalEnabled);

            EditorGUILayout.Space(4);
            DrawPbr(materialEditor, pMetallic, pSmoothness);

            EditorGUILayout.Space(4);
            DrawEmission(materialEditor, pEmissionColor, pEmissionStrength);

            foreach (var o in materialEditor.targets)
            {
                if (o is Material m)
                    SetupMaterialKeywords(m);
            }
        }

        private static void DrawBase(MaterialEditor materialEditor, MaterialProperty baseColor, MaterialProperty baseMap)
        {
            s_FoldBase = EditorGUILayout.BeginFoldoutHeaderGroup(s_FoldBase, LabelBaseHeader);
            if (s_FoldBase)
            {
                if (baseColor != null)
                    materialEditor.ShaderProperty(baseColor, LabelBaseColor);

                if (baseMap != null)
                {
                    materialEditor.TextureProperty(baseMap, LabelBaseMap.text, true);
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private static void DrawRoom(
            MaterialEditor materialEditor,
            MaterialProperty mappingTypeProp,
            MaterialProperty roomTex,
            MaterialProperty roomTintColor,
            MaterialProperty rooms,
            MaterialProperty roomDepth,
            MaterialProperty parallaxDepth,
            bool mixedMapping,
            int mappingType)
        {
            s_FoldRoom = EditorGUILayout.BeginFoldoutHeaderGroup(s_FoldRoom, LabelRoomHeader);
            if (s_FoldRoom)
            {
                if (mappingTypeProp != null)
                {
                    EditorGUI.showMixedValue = mappingTypeProp.hasMixedValue;
                    int current = Mathf.Clamp(Mathf.RoundToInt(mappingTypeProp.floatValue), 0, 1);
                    EditorGUI.BeginChangeCheck();
                    MaterialEditor.BeginProperty(mappingTypeProp);
                    int next = EditorGUILayout.Popup(LabelMappingType, current, MappingTypeOptions);
                    if (EditorGUI.EndChangeCheck())
                        mappingTypeProp.floatValue = next;
                    MaterialEditor.EndProperty();
                    EditorGUI.showMixedValue = false;
                }

                if (roomTex != null)
                {
                    materialEditor.TextureProperty(roomTex, LabelRoomTex.text, true);
                }

                if (roomTintColor != null)
                    materialEditor.ShaderProperty(roomTintColor, LabelRoomTintColor);

                if (mixedMapping)
                {
                    if (rooms != null) materialEditor.ShaderProperty(rooms, LabelRooms);
                    if (roomDepth != null) materialEditor.ShaderProperty(roomDepth, LabelRoomDepth);
                    if (parallaxDepth != null) materialEditor.ShaderProperty(parallaxDepth, LabelParallaxDepth);
                }
                else
                {
                    if (mappingType == (int)MappingType.InteriorMapping)
                    {
                        if (rooms != null) materialEditor.ShaderProperty(rooms, LabelRooms);
                        if (roomDepth != null) materialEditor.ShaderProperty(roomDepth, LabelRoomDepth);
                    }
                    else
                    {
                        if (parallaxDepth != null) materialEditor.ShaderProperty(parallaxDepth, LabelParallaxDepth);
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private static void DrawFrost(
            MaterialEditor materialEditor,
            MaterialProperty frostBlendOn,
            MaterialProperty frostedRoomTex,
            MaterialProperty frostMask,
            MaterialProperty frostStrength,
            bool frostEnabled)
        {
            s_FoldFrost = EditorGUILayout.BeginFoldoutHeaderGroup(s_FoldFrost, LabelFrostHeader);
            if (s_FoldFrost)
            {
                if (frostBlendOn != null)
                    materialEditor.ShaderProperty(frostBlendOn, LabelFrostBlendOn);

                if (frostEnabled)
                {
                    if (frostedRoomTex != null)
                        materialEditor.TextureProperty(frostedRoomTex, LabelFrostedRoomTex.text, false);

                    if (frostMask != null)
                    {
                        materialEditor.TextureProperty(frostMask, LabelFrostMask.text, true);
                    }

                    if (frostStrength != null)
                        materialEditor.ShaderProperty(frostStrength, LabelFrostStrength);
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private static void DrawNormal(
            MaterialEditor materialEditor,
            MaterialProperty normalMapOn,
            MaterialProperty normalMap,
            MaterialProperty normalScale,
            bool normalEnabled)
        {
            s_FoldNormal = EditorGUILayout.BeginFoldoutHeaderGroup(s_FoldNormal, LabelNormalHeader);
            if (s_FoldNormal)
            {
                if (normalMapOn != null)
                    materialEditor.ShaderProperty(normalMapOn, LabelNormalMapOn);

                if (normalEnabled)
                {
                    if (normalMap != null)
                    {
                        materialEditor.TextureProperty(normalMap, LabelNormalMap.text, true);
                    }

                    if (normalScale != null)
                        materialEditor.ShaderProperty(normalScale, LabelNormalScale);
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private static void DrawPbr(MaterialEditor materialEditor, MaterialProperty metallic, MaterialProperty smoothness)
        {
            s_FoldPbr = EditorGUILayout.BeginFoldoutHeaderGroup(s_FoldPbr, LabelPbrHeader);
            if (s_FoldPbr)
            {
                if (metallic != null)
                    materialEditor.ShaderProperty(metallic, LabelMetallic);
                if (smoothness != null)
                    materialEditor.ShaderProperty(smoothness, LabelSmoothness);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private static void DrawEmission(MaterialEditor materialEditor, MaterialProperty emissionColor, MaterialProperty emissionStrength)
        {
            s_FoldEmission = EditorGUILayout.BeginFoldoutHeaderGroup(s_FoldEmission, LabelEmissionHeader);
            if (s_FoldEmission)
            {
                if (emissionColor != null)
                    materialEditor.ShaderProperty(emissionColor, LabelEmissionColor);
                if (emissionStrength != null)
                    materialEditor.ShaderProperty(emissionStrength, LabelEmissionStrength);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private static void SetupMaterialKeywords(Material material)
        {
            if (material == null) return;

            if (material.HasProperty(PropMappingType))
            {
                int type = Mathf.Clamp(Mathf.RoundToInt(material.GetFloat(PropMappingType)), 0, 1);
                CoreUtils.SetKeyword(material, KwMappingParallax, type == (int)MappingType.ParallaxMapping);
            }

            if (material.HasProperty(PropFrostBlendOn))
            {
                bool enabled = material.GetFloat(PropFrostBlendOn) > 0.5f;
                CoreUtils.SetKeyword(material, KwFrostBlendOn, enabled);
            }

            if (material.HasProperty(PropNormalMapOn))
            {
                bool enabled = material.GetFloat(PropNormalMapOn) > 0.5f;
                CoreUtils.SetKeyword(material, KwNormalMapOn, enabled);
            }
        }
    }
}


