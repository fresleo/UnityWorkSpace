// Created By: WangYu  Date: 2024-05-30

using System.Collections.Generic;
using System.IO;
using com.xknight.mt.Lib.Editor.MT.Utils;
using com.xknight.mt.Lib.Runtime.MT.OcclusionCulling;
using com.xknight.mt.Lib.Runtime.MT.Serialize;
using com.xknight.mt.Lib.Runtime.MT.UnityComponent;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace com.xknight.mt.Lib.Editor.MT.OcclusionCulling
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SpatialDataTagGenerator))]
    public class SpatialDataTagGeneratorInspector : AbsInspector<SpatialDataTagGenerator>
    {
        private SerializedProperty m_debugTree_prop, m_debugTreeLabel_prop, m_debugVolume_prop;
        
        private SerializedProperty m_bnd_prop;
        private SerializedProperty m_treeDepth_prop;
        private SerializedProperty m_autoCreateLoader_prop;

        protected override void ExecuteOnEnable(SpatialDataTagGenerator script)
        {
            base.ExecuteOnEnable(script);

            m_debugTree_prop = serializedObject.FindProperty(nameof(script.debugTree));
            m_debugTreeLabel_prop = serializedObject.FindProperty(nameof(script.debugTreeLabel));
            m_debugVolume_prop = serializedObject.FindProperty(nameof(script.debugVolume));
            
            m_bnd_prop = serializedObject.FindProperty(nameof(script.bnd));
            m_treeDepth_prop = serializedObject.FindProperty(nameof(script.treeDepth));
            m_autoCreateLoader_prop = serializedObject.FindProperty(nameof(script.autoCreateLoader));
        }

        private GUILayoutOption m_glw_100 = GUILayout.Width(100);
        
        protected override void DrawAutoApplyGUI(SpatialDataTagGenerator script)
        {
            EditorGUILayout.LabelField("空间数据标记生成器");

            EditorGUILayout.Space(5);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("调试信息");
                EditorGUILayout.Space(5);
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(m_debugTree_prop, new GUIContent("树"));
                EditorGUILayout.PropertyField(m_debugTreeLabel_prop, new GUIContent("树标签"));
                
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
                
                EditorGUILayout.Space(5);
            }
            
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(m_treeDepth_prop, new GUIContent("树深度"));
            
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(m_autoCreateLoader_prop, new GUIContent("自动创建加载器"));
            
            EditorGUILayout.Space(5);
            if (GUILayout.Button(new GUIContent("生成"), m_glw_100))
            {
                ExecuteGenerate(script, false);
            }
            
            EditorGUILayout.Space(5);
        }
        
        
        /// <summary>
        /// 执行生成方法
        /// </summary>
        public void ExecuteGenerate(SpatialDataTagGenerator generator, bool displayDialog)
        {
            // 挂 id 组件，分配唯一 id
            Scene activeScene = SceneManager.GetActiveScene();
            SDTRendererId.ClearSceneIdComponents(activeScene);
            SDTRendererId.GenerateSceneIdComponents(activeScene);
            
            GenerateData(generator, out SpatialDataTagConfig config);
            CreateLoader(generator, config);
            
            EditorUtility.ClearProgressBar();
            if (!displayDialog)
            {
                EditorUtility.DisplayDialog("空间数据标记生成", "生成完成", "确定");
            }
        }

        private const string c_config_postfix = "_sdt";
        
        //生成数据
        private void GenerateData(
            SpatialDataTagGenerator generator,
            out SpatialDataTagConfig config)
        {
            //确保当前场景的输出目录存在
            IOUtils.EnsureActiveSceneOutDir(
                out string meshOutDir, out string dataOutDir, 
                out string binaryOutDir, out string materialOutDir, 
                out string controlOutDir);
            
            string goName = generator.gameObject.name.ToLower();
            config = ScriptableObject.CreateInstance<SpatialDataTagConfig>();
            
            //导出树数据
            ExportTreeBytes(config, generator.quadTreeRoot, binaryOutDir, goName);
            
            //保存配置
            string configPath = $"{dataOutDir}/{goName}{c_config_postfix}.asset";
            AssetDatabase.CreateAsset(config, configPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void ExportTreeBytes(SpatialDataTagConfig config, SDTQuadTreeBuildNode treeRoot, string outDir, string goName)
        {
            //初始化一个根节点
            var nodes = new List<SDTQuadTreeNode>();
            var rootNode = new SDTQuadTreeNode();
            rootNode.Initialize(0);
            nodes.Add(rootNode);
            
            //build 导出成 runtime 节点
            QuadTreeNodeUtil.ExportTreeNodes(
                treeRoot, rootNode, nodes,
                (buildNode, node) =>
                {
                    node.holdIds.Clear();
                    node.holdIds.AddRange(buildNode.holdIds);
                    
                    node.holdBounds.Clear();
                    node.holdBounds.AddRange(buildNode.holdBounds);
                    
                    node.holdWorldMatrixs.Clear();
                    node.holdWorldMatrixs.AddRange(buildNode.holdWorldMatrixs);
                });
            
            MemoryStream stream = new MemoryStream();
            QuadTreeNodeUtil.SerializeTrees(stream, nodes);
            
            string treeDataPath = $"{outDir}/{goName}{c_config_postfix}.bytes";
            File.WriteAllBytes(treeDataPath, stream.ToArray());
            stream.Close();
            
            AssetDatabase.Refresh();
            config.treeDataPath = IOUtils.FullPathToRelativePath(treeDataPath);
        }
        
        
        private void CreateLoader(
            SpatialDataTagGenerator generator,
            SpatialDataTagConfig config)
        {
            if (!generator.autoCreateLoader)
            {
                return;
            }
            
            string goName = "SDTLoader"; // 因为这个全局唯1，所以给个死名字
            var loaderGo = new GameObject(goName);
            loaderGo.layer = generator.gameObject.layer;
            
            // 默认用主相机当剔除相机
            Camera cullCamera = Camera.main;
            
            // 剔除器
            var culler = loaderGo.AddComponent<DynamicOcclusionCuller>();
            {
                var assetGo = Resources.Load<GameObject>("VisibilityTestingAsset");
                var cloneGo = UnityEngine.Object.Instantiate(assetGo, loaderGo.transform, false);
                cloneGo.name = cloneGo.name.Replace("(Clone)", "");
                cloneGo.SetActive(false);
                
                culler.testGo = cloneGo;
                culler.cullCamera = cullCamera;
            }

            // 加载器
            var loader = loaderGo.AddComponent<SDTLoader>();
            {
                string assetPath = AssetDatabase.GetAssetPath(config);
                loader.configPath = IOUtils.FullPathToRelativePath(assetPath);
                loader.cullCamera = cullCamera;
                
                // 互相传递一下设置
                loader.culler = culler;
            }
            
            //隐藏生成器节点
            if (generator.gameObject.activeSelf)
            {
                generator.gameObject.SetActive(false);
            }
        }

        
        // 绘制辅助的场景 GUI >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        private GUIStyle s_gs_treeLabel;
        
        protected override void ExecuteOnSceneGUI(SpatialDataTagGenerator script)
        {
            base.ExecuteOnSceneGUI(script);
            
            if (!script.enabled)
            {
                return;
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
        
        private void DrawTreeLabel(SDTQuadTreeBuildNode buildNode)
        {
            if (buildNode == null)
            {
                return;
            }
            
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