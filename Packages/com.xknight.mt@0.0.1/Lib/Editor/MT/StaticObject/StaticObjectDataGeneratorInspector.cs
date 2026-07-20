// Created By: WangYu  Date: 2023-11-18

using System;
using System.Collections.Generic;
using System.IO;
using com.xknight.mt.Lib.Editor.MT.Utils;
using com.xknight.mt.Lib.Runtime.MT.Serialize;
using com.xknight.mt.Lib.Runtime.MT.StaticObject;
using com.xknight.mt.Lib.Runtime.MT.UnityComponent;
using com.xknight.mt.Lib.Runtime.MT.Utils;
using UnityEditor;
using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.StaticObject
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(StaticObjectDataGenerator))]
    public class StaticObjectDataGeneratorInspector : AbsInspector<StaticObjectDataGenerator>
    {
        private SerializedProperty m_debugChildren_prop, m_debugChildrenName_prop;
        private SerializedProperty m_debugTree_prop, m_debugTreeName_prop;
        private SerializedProperty m_debugVolume_prop;
        
        private SerializedProperty m_bnd_prop, m_expand_prop;
        private SerializedProperty m_treeDepth_prop;
        private SerializedProperty m_childrenGos_prop, m_prefabGos_prop;
        private SerializedProperty m_autoCreateLoader_prop;

        protected override void ExecuteOnEnable(StaticObjectDataGenerator script)
        {
            base.ExecuteOnEnable(script);
            
            m_debugChildren_prop = serializedObject.FindProperty(nameof(script.debugChildren));
            m_debugChildrenName_prop = serializedObject.FindProperty(nameof(script.debugChildrenName));
            
            m_debugTree_prop = serializedObject.FindProperty(nameof(script.debugTree));
            m_debugTreeName_prop = serializedObject.FindProperty(nameof(script.debugTreeLabel));
            
            m_debugVolume_prop = serializedObject.FindProperty(nameof(script.debugVolume));
            
            m_bnd_prop = serializedObject.FindProperty(nameof(script.bnd));
            m_expand_prop = serializedObject.FindProperty(nameof(script.expand));
            
            m_treeDepth_prop = serializedObject.FindProperty(nameof(script.treeDepth));
            
            m_childrenGos_prop = serializedObject.FindProperty(nameof(script.childrenGos));
            m_prefabGos_prop = serializedObject.FindProperty(nameof(script.prototypes));
            
            m_autoCreateLoader_prop = serializedObject.FindProperty(nameof(script.autoCreateLoader));
        }
        
        private GUILayoutOption m_glw_100 = GUILayout.Width(100);
        
        protected override void DrawAutoApplyGUI(StaticObjectDataGenerator script)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("调试信息");
                EditorGUILayout.Space(5);
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(m_debugChildren_prop, new GUIContent("静态子对象"));
                EditorGUILayout.PropertyField(m_debugChildrenName_prop, new GUIContent("静态子对象 - 名字"));
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(m_debugTree_prop, new GUIContent("树"));
                EditorGUILayout.PropertyField(m_debugTreeName_prop, new GUIContent("树 - 名字"));
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(m_debugVolume_prop, new GUIContent("体积"));
                
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
            }
            EditorGUILayout.Space(5);
            
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.Space(5);
                
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(m_bnd_prop, new GUIContent("包围盒"));
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.PropertyField(m_expand_prop, new GUIContent("扩大范围"));
                EditorGUILayout.Space(5);
            }
            EditorGUILayout.Space(5);
            
            EditorGUILayout.PropertyField(m_treeDepth_prop, new GUIContent("树深度"));
            EditorGUILayout.Space(5);
            
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(m_childrenGos_prop, new GUIContent("扫描到的子预设"));
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(m_prefabGos_prop, new GUIContent("子预设的原型列表"));
            EditorGUILayout.Space(5);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(m_autoCreateLoader_prop, new GUIContent("自动创建加载器"));
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button(new GUIContent("生成这个"), m_glw_100))
            {
                ExecuteGenerate(script, false);
            }
            EditorGUILayout.Space(5);
            
            BatchExecuteGenerate();
            EditorGUILayout.Space(5);
        }
        
        private StaticObjectDataGenerator[] m_batchArray;
        
        private void BatchExecuteGenerate()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.Space(5);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(new GUIContent("当前生成器数量"), m_glw_100))
                    {
                        m_batchArray = FindObjectsOfType<StaticObjectDataGenerator>();
                    }
                    
                    GUILayout.Space(5);
                    if (GUILayout.Button(new GUIContent("全部生成"), m_glw_100))
                    {
                        m_batchArray = FindObjectsOfType<StaticObjectDataGenerator>();
                        for (int t = m_batchArray.Length, i = 0; i < t; i++)
                        {
                            EditorUtility.DisplayProgressBar("全部生成", $"{i}/{t}", (float)i / t);
                            
                            var item = m_batchArray[i];
                            var editorObj = GUIUtils.GetEditorObjByRuntimeObj<StaticObjectDataGeneratorInspector>(item);
                            editorObj.ExecuteGenerate(item, true);
                        }
                        EditorUtility.ClearProgressBar();
                    }
                }
                
                if (m_batchArray != null)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField($"当前场景中生成器的数量：{m_batchArray.Length}");
                }
                
                EditorGUILayout.Space(5);
            }
        }
        
        
        /// <summary>
        /// 执行生成方法
        /// </summary>
        public void ExecuteGenerate(StaticObjectDataGenerator script, bool isBatchExecute)
        {
            GenerateData(script, out SOGroupConfig groupConfig);
            CreateLoader(script, groupConfig);
            
            EditorUtility.ClearProgressBar();
            if (!isBatchExecute)
            {
                EditorUtility.DisplayDialog("静态对象数据生成", "生成完成", "确定");
            }
        }

        //生成数据
        private void GenerateData(
            StaticObjectDataGenerator script, 
            out SOGroupConfig groupConfig)
        {
            //确保当前场景的输出目录存在
            IOUtils.EnsureActiveSceneOutDir(
                out string meshOutDir, out string dataOutDir, 
                out string binaryOutDir, out string materialOutDir, 
                out string controlOutDir);
            
            string goName = script.gameObject.name.ToLower();
            
            groupConfig = ScriptableObject.CreateInstance<SOGroupConfig>();
            
            //导出树数据
            ExportTreeBytes(groupConfig, script.quadTreeRoot, binaryOutDir, goName);
            
            //保存配置
            string configPath = $"{dataOutDir}/{goName}_so.asset";
            AssetDatabase.CreateAsset(groupConfig, configPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void ExportTreeBytes(SOGroupConfig config, SOQuadTreeBuildNode treeRoot, string outDir, string goName)
        {
            //初始化一个根节点
            var nodes = new List<SOQuadTreeNode>();
            var rootNode = new SOQuadTreeNode();
            rootNode.Initialize(0);
            nodes.Add(rootNode);
            
            //build 导出成 runtime 节点
            QuadTreeNodeUtil.ExportTreeNodes(
                treeRoot, rootNode, nodes,
                (buildNode, node) =>
                {
                    node.holdGids.Clear();
                    node.holdGids.AddRange(buildNode.holdGids);

                    node.holdAssetIdxs.Clear();
                    node.holdAssetIdxs.AddRange(buildNode.holdAssetIdxs);

                    node.holdWorldMatrixs.Clear();
                    node.holdWorldMatrixs.AddRange(buildNode.holdWorldMatrixs);
                    
                    node.holdLightmapDatas.Clear();
                    foreach (var iter in buildNode.holdLightmapDatas)
                    {
                        node.holdLightmapDatas.Add(iter.Key, iter.Value);
                    }
                });

            MemoryStream stream = new MemoryStream();
            QuadTreeNodeUtil.SerializeTrees(stream, nodes);
            
            string treeDataPath = $"{outDir}/{goName}_so.bytes";
            File.WriteAllBytes(treeDataPath, stream.ToArray());
            stream.Close();
            
            AssetDatabase.Refresh();
            config.treeDataPath = IOUtils.FullPathToRelativePath(treeDataPath);
        }
        
        private void CreateLoader(
            StaticObjectDataGenerator script, 
            SOGroupConfig config)
        {
            if (!script.autoCreateLoader)
            {
                return;
            }
            
            //创建加载器对象
            string goName = $"{script.gameObject.name}";
            var loaderGo = new GameObject(goName);
            loaderGo.layer = script.gameObject.layer;
            var loader = loaderGo.AddComponent<SOLoader>();

            //创建原型的父节点
            var prototypeParent = new GameObject("Prototypes");
            prototypeParent.transform.SetParent(loaderGo.transform);
            prototypeParent.SetActive(false);
            
            loader.cullCamera = Camera.main;
            
            string assetPath = AssetDatabase.GetAssetPath(config);
            loader.groupConfigPath = IOUtils.FullPathToRelativePath(assetPath);

            //创建原型资源的载体
            int total = script.prototypes.Count;
            var clonePrototypes = new GameObject[total];
            for (int i = 0; i < total; i++)
            {
                var prototype = script.prototypes[i];
                
                var cloneGo = Instantiate(prototype, prototypeParent.transform);
                cloneGo.name = cloneGo.name.Replace("(Clone)", "");

                clonePrototypes[i] = cloneGo;
            }
            loader.prototypes = clonePrototypes;
            
            //隐藏生成器节点
            if (script.gameObject.activeSelf)
            {
                script.gameObject.SetActive(false);
            }
        }


        private GUIStyle s_gs_child, s_gs_treeLabel;
        
        protected override void ExecuteOnSceneGUI(StaticObjectDataGenerator script)
        {
            base.ExecuteOnSceneGUI(script);

            if (!script.enabled)
            {
                return;
            }
            
            if (script.debugChildren && script.debugChildrenName)
            {
                if (s_gs_child == null)
                {
                    s_gs_child = new GUIStyle();
                    s_gs_child.normal.textColor = Color.magenta;
                }
                
                foreach (var itemGo in script.childrenGos)
                {
                    Bounds childBounds = MTRuntimeUtils.GetWholeBounds(itemGo);
                    
                    Handles.Label(childBounds.center, itemGo.gameObject.name, s_gs_child);
                }
            }

            if (script.debugTree && script.debugTreeLabel)
            {
                if (s_gs_treeLabel == null)
                {
                    s_gs_treeLabel = new GUIStyle();
                    s_gs_treeLabel.normal.textColor = Color.magenta;
                }

                DrawTreeLabel(script.quadTreeRoot);
            }
        }

        private void DrawTreeLabel(SOQuadTreeBuildNode buildNode)
        {
            if (buildNode.childrenNodes.Count == 0)
            {
                Handles.Label(buildNode.bnd.center, buildNode.DebugLabel, s_gs_treeLabel);
            }
            else
            {
                foreach (var child in buildNode.childrenNodes)
                {
                    DrawTreeLabel(child);
                }
            }
        }
        
    }
}