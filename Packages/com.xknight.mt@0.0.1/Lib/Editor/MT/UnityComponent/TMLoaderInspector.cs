// Created By: WangYu  Date: 2023-12-21

using com.xknight.mt.Lib.Editor.MT.Utils;
using com.xknight.mt.Lib.Runtime.MT.UnityComponent;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.UnityComponent
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TMLoader))]
    public class TMLoaderInspector : AbsInspector<TMLoader>
    {
        private SerializedProperty m_cullCamera_prop;
        
        private SerializedProperty m_lpPath_prop, m_tmcPath_prop;
        private SerializedProperty m_receiveShadow_prop;
        
        private SerializedProperty m_useDetailDraw_prop, m_detailDrawDistance_prop;

        private SerializedProperty m_debugDetail_prop, m_debugTreeWorldBounds_prop, m_debugPositionContain_prop;
        
        protected override void ExecuteOnEnable(TMLoader script)
        {
            base.ExecuteOnEnable(script);

            var pf = new PropertyFetcher<TMLoader>(serializedObject);

            m_cullCamera_prop = pf.Find(x => x.cullCamera);

            m_lpPath_prop = pf.Find(x => x.lpPath);
            m_tmcPath_prop = pf.Find(x => x.tmcPath);

            m_receiveShadow_prop = pf.Find(x => x.receiveShadow);

            m_useDetailDraw_prop = pf.Find(x => x.useDetailDraw);
            m_detailDrawDistance_prop = pf.Find(x => x.detailDrawDistance);
            
            m_debugDetail_prop = pf.Find(x => x.debugDetail);
            m_debugTreeWorldBounds_prop = pf.Find(x => x.debugTreeWorldBounds);
            m_debugPositionContain_prop = pf.Find(x => x.debugPositionContain);
        }

        protected override void DrawAutoApplyGUI(TMLoader script)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("调试信息");
                EditorGUILayout.Space(5);
                EditorGUI.indentLevel++;
                
                EditorGUI.BeginDisabledGroup(true);
                {
                    EditorGUILayout.PropertyField(m_debugDetail_prop, new GUIContent("显示细节的调试信息"));
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(m_debugTreeWorldBounds_prop, new GUIContent("显示树的世界边界"));
                EditorGUI.indentLevel++;
                if (m_debugTreeWorldBounds_prop.boolValue)
                {
                    EditorGUILayout.PropertyField(m_debugPositionContain_prop, new GUIContent("测试坐标包含的 Transform 对象"));
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
                
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
            }
            EditorGUILayout.Space(5);
            
            //有 gc 问题，暂不开放
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("地表细节绘制功能");
                EditorGUILayout.Space(5);
                EditorGUI.indentLevel++;

                EditorGUI.BeginDisabledGroup(true);
                {
                    EditorGUILayout.PropertyField(m_useDetailDraw_prop, new GUIContent("开关"));
                    EditorGUILayout.Space(5);
                    
                    EditorGUILayout.PropertyField(m_detailDrawDistance_prop, new GUIContent("绘制距离"));
                }
                EditorGUI.EndDisabledGroup();
                
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
            }
            EditorGUILayout.Space(5);
            
            EditorGUILayout.PropertyField(m_cullCamera_prop, new GUIContent("剔除相机")); 
            EditorGUILayout.Space(5);
            
            EditorGUILayout.PropertyField(m_receiveShadow_prop, new GUIContent("接收阴影")); 
            EditorGUILayout.Space(5);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.PropertyField(m_lpPath_prop, new GUIContent("LOD策略"));
                
                if (script.LP != null)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(new GUIContent("当前引用"), script.LP, script.LP.GetType(), false);
                    EditorGUI.EndDisabledGroup();
                }
            }
            EditorGUILayout.Space(5);
            
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.PropertyField(m_tmcPath_prop, new GUIContent("地形网格的配置"));
                
                if (script.TMC != null)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(new GUIContent("当前引用"), script.TMC, script.TMC.GetType(), false);
                    EditorGUI.EndDisabledGroup();
                }
            }
            EditorGUILayout.Space(5);
        }
        
    }
}