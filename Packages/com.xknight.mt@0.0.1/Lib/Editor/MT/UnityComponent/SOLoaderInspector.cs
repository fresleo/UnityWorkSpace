// Created By: WangYu  Date: 2023-12-21

using com.xknight.mt.Lib.Editor.MT.Utils;
using com.xknight.mt.Lib.Runtime.MT.UnityComponent;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.UnityComponent
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SOLoader))]
    public class SOLoaderInspector : AbsInspector<SOLoader>
    {
        private SerializedProperty m_cullCamera_prop;
        
        private SerializedProperty m_groupConfigPath_prop;
        private SerializedProperty m_displayDistance_prop;
        
        private SerializedProperty m_debugTree_prop, m_debugTreeAssetIdxs_prop;

        protected override void ExecuteOnEnable(SOLoader script)
        {
            base.ExecuteOnEnable(script);
            
            var pf = new PropertyFetcher<SOLoader>(serializedObject);

            m_cullCamera_prop = pf.Find(x => x.cullCamera);

            m_groupConfigPath_prop = pf.Find(x => x.groupConfigPath);
            m_displayDistance_prop = pf.Find(x => x.displayDistance);

            m_debugTree_prop = pf.Find(x => x.debugTree);
            m_debugTreeAssetIdxs_prop = pf.Find(x => x.debugTreeAssetIdxs);
        }

        protected override void DrawAutoApplyGUI(SOLoader script)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("调试信息");
                EditorGUILayout.Space(5);
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(m_debugTree_prop, new GUIContent("调试信息 - 树"));
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(m_debugTreeAssetIdxs_prop, new GUIContent("调试信息 - 树 - 节点包含的资源索引"));
                
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
            }
            EditorGUILayout.Space(5);
            
            EditorGUILayout.PropertyField(m_cullCamera_prop, new GUIContent("剔除相机"));
            EditorGUILayout.Space(5);
            
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.PropertyField(m_groupConfigPath_prop, new GUIContent("组配置"));

                if (script.GroupConfig != null)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(new GUIContent("当前引用"), script.GroupConfig, script.GroupConfig.GetType(), false);
                    EditorGUI.EndDisabledGroup();
                }
            }
            EditorGUILayout.Space(5);
            
            EditorGUILayout.PropertyField(m_displayDistance_prop, new GUIContent("显示距离"));
            EditorGUILayout.Space(5);
        }

        private GUIStyle s_assetIdx_gs;
        
        protected override void ExecuteOnSceneGUI(SOLoader script)
        {
            base.ExecuteOnSceneGUI(script);
            
            if (!script.enabled)
            {
                return;
            }

            if (s_assetIdx_gs == null)
            {
                s_assetIdx_gs = new GUIStyle();
                s_assetIdx_gs.normal.textColor = Color.magenta;
            }
            
            if (script.debugTree && script.debugTreeAssetIdxs)
            {
                if (script.VisibleArray != null)
                {
                    for (int i = 0; i < script.VisibleArray.Length; i++)
                    {
                        var node = script.VisibleArray[i];
                        
                        string label = "";
                        for (int j = 0; j < node.holdAssetIdxs.Count; j++)
                        {
                            if(j > 0) label += ",";
                            label += $"{node.holdAssetIdxs[j]}";
                        }
                        
                        Handles.Label(node.bnd.center, label, s_assetIdx_gs);
                    }
                }
            }
        }
        
    }
}