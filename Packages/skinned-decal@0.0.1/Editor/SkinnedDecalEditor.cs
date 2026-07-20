using UnityEngine;
using UnityEditor;

namespace SkinnedDecals
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SkinnedDecal))]
    public class SkinnedDecalEditor : Editor
    {
        private SerializedProperty m_material;
        
        private SerializedProperty m_sizeX;

        private SerializedProperty m_sizeY;

        private SerializedProperty m_normalClip;

        private SerializedProperty m_fadeinTime, m_fadeinCurve, m_duration, m_fadeoutTime, m_fadeoutCurve;
        
        private SerializedProperty m_selectedAtlasItem;
        private SerializedProperty m_atlasItemCount;
        private SerializedProperty m_randomFromAtlas;
        
        private MaterialEditor m_materialEditor;
        private Material m_lastMaterial;

        private void OnEnable()
        {
            var script = this.target as SkinnedDecal;
            if(script == null) return;
            
            m_material = serializedObject.FindProperty(nameof(script.material));
            
            m_sizeX = serializedObject.FindProperty(nameof(script.sizeX));
            m_sizeY = serializedObject.FindProperty(nameof(script.sizeY));

            m_normalClip = serializedObject.FindProperty(nameof(script.normalClip));

            m_fadeinTime = serializedObject.FindProperty(nameof(script.fadeinTime));
            m_fadeinCurve = serializedObject.FindProperty(nameof(script.fadeinCurve));
            m_duration = serializedObject.FindProperty(nameof(script.duration));
            m_fadeoutTime = serializedObject.FindProperty(nameof(script.fadeoutTime));
            m_fadeoutCurve = serializedObject.FindProperty(nameof(script.fadeoutCurve));
            
            m_selectedAtlasItem = serializedObject.FindProperty(nameof(script.selectedAtlasItem));
            m_atlasItemCount = serializedObject.FindProperty(nameof(script.atlasItemCount));
            m_randomFromAtlas = serializedObject.FindProperty(nameof(script.randomFromAtlas));
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            var script = this.target as SkinnedDecal;
            if (script == null) return;

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUIUtility.labelWidth = 160;
            EditorGUIUtility.fieldWidth = 120;
            
            EditorGUILayout.HelpBox("用来定义贴花类型", MessageType.Info);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("尺寸", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope(1))
            {
                EditorGUILayout.PropertyField(m_sizeX, new GUIContent("粗细"));
                EditorGUILayout.PropertyField(m_sizeY, new GUIContent("长短"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("功能", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope(1))
            {
                m_material.objectReferenceValue = EditorGUILayout.ObjectField("材质", m_material.objectReferenceValue, typeof(Material), false);
                
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(m_normalClip, new GUIContent("双面还是单面"));
                EditorGUILayout.HelpBox("-1 = 正面和背面, 0 = 仅正面", MessageType.Info);
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("生命周期", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope(1))
            {
                EditorGUILayout.PropertyField(m_fadeinTime, new GUIContent("淡入时间"));
                EditorGUILayout.PropertyField(m_fadeinCurve, new GUIContent("淡入速度曲线"));
                EditorGUILayout.PropertyField(m_duration, new GUIContent("持续时间，( <=0 ) : 无限"));
                EditorGUI.BeginDisabledGroup(m_duration.floatValue <= 0);
                EditorGUILayout.PropertyField(m_fadeoutTime, new GUIContent("淡出时间"));
                EditorGUILayout.PropertyField(m_fadeoutCurve, new GUIContent("淡出速度曲线"));
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("图集", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope(1))
            {
                EditorGUILayout.HelpBox("这些选项需要 shader 配合工作，否则不起作用。", MessageType.Info);
                EditorGUILayout.PropertyField(m_selectedAtlasItem, new GUIContent("已选项目"));
                EditorGUILayout.PropertyField(m_atlasItemCount, new GUIContent("图集项目计数"));
                EditorGUILayout.PropertyField(m_randomFromAtlas, new GUIContent("从图集中随机"));
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            DrawMaterialInspectorGUI(script.material);
        }

        private void DrawMaterialInspectorGUI(Material material)
        {
            // 记录材质的更新，并创建材质编辑器对象
            if (material != m_lastMaterial)
            {
                if (m_materialEditor != null)
                {
                    DestroyImmediate(m_materialEditor);
                }
                if (material != null)
                {
                    m_materialEditor = (MaterialEditor)CreateEditor(material);
                }

                m_lastMaterial = material;
            }

            // 显示材质的检查器菜单
            if (m_materialEditor != null)
            {
                EditorGUILayout.Separator();
                m_materialEditor.DrawHeader();

                // 如果是 unity 内置的材质，则不允许修改
                bool isDefaultMaterial = !AssetDatabase.GetAssetPath(material).StartsWith("Assets");
                using (new EditorGUI.DisabledGroupScope(isDefaultMaterial))
                {
                    m_materialEditor.OnInspectorGUI();
                }
            }
        }
        
    }
}