using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace RadiantGI.Universal
{
    [CustomEditor(typeof(RadiantRenderFeature))]
    public class RadiantRenderFeatureEditor : Editor
    {
        private SerializedProperty m_renderPassEvent, m_renderingPath, m_ignorePostProcessingOption;

        private void OnEnable()
        {
            var sop = new PropertyFetcher<RadiantRenderFeature>(serializedObject);
            
            m_renderPassEvent = sop.Find(x => x.renderPassEvent);
            m_renderingPath = sop.Find(x => x.renderingPath);
            m_ignorePostProcessingOption = sop.Find(x => x.ignorePostProcessingOption);
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(m_renderPassEvent, new GUIContent("渲染通道的执行时机"));
            EditorGUILayout.PropertyField(m_ignorePostProcessingOption, new GUIContent("忽略后处理的设置", "后处理开不开它都执行"));
            
            EditorGUILayout.PropertyField(m_renderingPath, new GUIContent("使用的渲染路径"));
            
            string text = "请确保渲染路径与上述 URP 资源的渲染路径匹配（建议延迟使用以获得最佳效果）。" + "仅当场景在使用前向渲染路径的不透明材质时，使用了如 URP 的 Complex Lit 着色器时，才使用“Both”选项。";
            EditorGUILayout.HelpBox(text, MessageType.Info);
        }
    }
}