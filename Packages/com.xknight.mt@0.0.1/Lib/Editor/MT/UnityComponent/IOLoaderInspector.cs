// Created By: WangYu  Date: 2023-12-21

using com.xknight.mt.Lib.Editor.MT.Utils;
using com.xknight.mt.Lib.Runtime.MT.Serialize;
using com.xknight.mt.Lib.Runtime.MT.UnityComponent;
using com.xknight.mt.Lib.Runtime.MT.Utils;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.UnityComponent
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(IOLoader))]
    public class IOLoaderInspector : AbsInspector<IOLoader>
    {
        private SerializedProperty m_cullCamera_prop;
        
        private SerializedProperty m_groupConfigPath_prop;
        private SerializedProperty m_lodMeshes_prop, m_lodMaterials_prop;
        private SerializedProperty m_displayDistance_prop;
        private SerializedProperty m_lodAndScreenCovers_prop;
        private SerializedProperty m_castShadows_prop, m_receiveShadows_prop;

        private SerializedProperty m_debugTreeBnd_prop, m_debugIoBnd_prop, m_debugIoRate_prop;

        protected override void ExecuteOnEnable(IOLoader script)
        {
            base.ExecuteOnEnable(script);
            
            var pf = new PropertyFetcher<IOLoader>(serializedObject);

            m_cullCamera_prop = pf.Find(x => x.cullCamera);

            m_groupConfigPath_prop = pf.Find(x => x.groupConfigPath);

            m_lodMeshes_prop = pf.Find(x => x.lodMeshes);
            m_lodMaterials_prop = pf.Find(x => x.lodMaterials);

            m_displayDistance_prop = pf.Find(x => x.displayDistance);

            m_lodAndScreenCovers_prop = pf.Find(x => x.lodAndScreenCovers);

            m_castShadows_prop = pf.Find(x => x.castShadows);
            m_receiveShadows_prop = pf.Find(x => x.receiveShadows);

            m_debugTreeBnd_prop = pf.Find(x => x.debugTreeBnd);
            m_debugIoBnd_prop = pf.Find(x => x.debugIoBnd);
            m_debugIoRate_prop = pf.Find(x => x.debugIoRate);
        }

        protected override void DrawAutoApplyGUI(IOLoader script)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("调试信息");
                EditorGUILayout.Space(5);
                EditorGUI.indentLevel++;

                if (script.GroupConfig != null)
                {
                    switch (script.GroupConfig.dataType)
                    {
                        case IOGroupConfig.EDataType.Flat:
                        {
                            EditorGUILayout.PropertyField(m_debugIoBnd_prop, new GUIContent("实例化对象的包围盒"));
                            EditorGUILayout.Space(5);
                        }
                            break;
                        
                        case IOGroupConfig.EDataType.Tree:
                        {
                            EditorGUILayout.PropertyField(m_debugIoBnd_prop, new GUIContent("实例化对象的包围盒"));
                            EditorGUILayout.Space(5);
                            
                            EditorGUILayout.PropertyField(m_debugTreeBnd_prop, new GUIContent("树的包围盒"));
                            EditorGUILayout.Space(5);
                        }
                            break;
                    }
                }
                
                EditorGUILayout.PropertyField(m_debugIoRate_prop, new GUIContent("实例化对象的屏幕覆盖率"));
                
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

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("LOD资源的载体");
                EditorGUILayout.Space(5);
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(m_lodMeshes_prop, new GUIContent("lod 网格"));
                EditorGUILayout.PropertyField(m_lodMaterials_prop, new GUIContent("lod 材质"));
                
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
            }
            EditorGUILayout.Space(5);
            
            EditorGUILayout.PropertyField(m_displayDistance_prop, new GUIContent("显示距离"));
            EditorGUILayout.Space(5);
            
            EditorGUILayout.PropertyField(m_lodAndScreenCovers_prop, new GUIContent("LOD与屏幕覆盖率的关系"));
            EditorGUILayout.Space(5);
            
            EditorGUILayout.PropertyField(m_castShadows_prop, new GUIContent("投射阴影"));
            EditorGUILayout.Space(5);
            
            EditorGUILayout.PropertyField(m_receiveShadows_prop, new GUIContent("接收阴影"));
            EditorGUILayout.Space(5);
        }

        private GUIStyle s_rate_gs;

        protected override void ExecuteOnSceneGUI(IOLoader script)
        {
            base.ExecuteOnSceneGUI(script);

            if (!script.enabled)
            {
                return;
            }

            if (s_rate_gs == null)
            {
                s_rate_gs = new GUIStyle();
                s_rate_gs.normal.textColor = Color.magenta;
            }

            //因为改成了运行时才加载配置，所以这里暂时没有什么好办法，先判空处理了
            if (script.GroupConfig != null)
            {
                switch (script.GroupConfig.dataType)
                {
                    //平铺模式
                    case IOGroupConfig.EDataType.Flat:
                    {
                        if (script.debugIoRate)
                        {
                            if (script.VisibleDataArray != null)
                            {
                                for (int i = 0; i < script.VisibleDataArray.Length; i++)
                                {
                                    var item = script.VisibleDataArray[i];

                                    //覆盖率
                                    float rate = MTRuntimeUtils.ScreenCoverRate(script.cullCamera, item.bnd);
                                    Handles.Label(item.bnd.center, $"{rate}", s_rate_gs);
                                }
                            }
                        }
                    }
                        break;
                    
                    //树模式
                    case IOGroupConfig.EDataType.Tree:
                    {
                        if (script.debugIoRate)
                        {
                            if (script.VisibleNodeArray != null)
                            {
                                for (int ii = 0; ii < script.VisibleNodeArray.Length; ii++)
                                {
                                    var item = script.VisibleNodeArray[ii];

                                    for (int jj = 0; jj < item.holdBounds.Count; jj++)
                                    {
                                        var bnd = item.holdBounds[jj];

                                        //覆盖率
                                        float rate = MTRuntimeUtils.ScreenCoverRate(script.cullCamera, bnd);
                                        Handles.Label(bnd.center, $"{rate}", s_rate_gs);
                                    }
                                }
                            }
                        }
                    }
                        break;
                }
            }
        }

    }
}