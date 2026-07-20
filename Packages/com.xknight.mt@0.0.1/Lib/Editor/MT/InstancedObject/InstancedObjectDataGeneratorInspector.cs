// Created By: WangYu  Date: 2023-11-30

using System;
using System.Collections.Generic;
using System.IO;
using com.xknight.mt.Lib.Editor.MT.Utils;
using com.xknight.mt.Lib.Runtime.MT.InstancedObject;
using com.xknight.mt.Lib.Runtime.MT.Serialize;
using com.xknight.mt.Lib.Runtime.MT.UnityComponent;
using com.xknight.mt.Lib.Runtime.MT.Utils;
using UnityEditor;
using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.InstancedObject
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(InstancedObjectDataGenerator))]
    public class InstancedObjectDataGeneratorInspector : AbsInspector<InstancedObjectDataGenerator>
    {
        private SerializedProperty m_debugTree_prop;
        private SerializedProperty m_debugChildren_prop, m_debugChildrenRate_prop;
        private SerializedProperty m_debugVolume_prop;
        private SerializedProperty m_cullCamera_prop;
        
        private SerializedProperty m_bnd_prop, m_expand_prop;
        private SerializedProperty m_dataType_prop;
        private SerializedProperty m_treeDepth_prop;
        private SerializedProperty m_lodMeshes_prop, m_lodMaterials_prop;
        private SerializedProperty m_childrenMarkers_prop;
        private SerializedProperty m_autoCreateLoader_prop;

        protected override void ExecuteOnEnable(InstancedObjectDataGenerator script)
        {
            base.ExecuteOnEnable(script);
            
            m_debugTree_prop = serializedObject.FindProperty(nameof(script.debugTree));
            m_debugChildren_prop = serializedObject.FindProperty(nameof(script.debugChildren));
            m_debugChildrenRate_prop = serializedObject.FindProperty(nameof(script.debugChildrenRate));
            m_debugVolume_prop = serializedObject.FindProperty(nameof(script.debugVolume));
            m_cullCamera_prop = serializedObject.FindProperty(nameof(script.cullCamera));
            
            m_bnd_prop = serializedObject.FindProperty(nameof(script.bnd));
            m_expand_prop = serializedObject.FindProperty(nameof(script.expand));
            
            m_dataType_prop = serializedObject.FindProperty(nameof(script.dataType));
            m_treeDepth_prop = serializedObject.FindProperty(nameof(script.treeDepth));
            
            m_lodMeshes_prop = serializedObject.FindProperty(nameof(script.lodMeshes));
            m_lodMaterials_prop = serializedObject.FindProperty(nameof(script.lodMaterials));
            
            m_childrenMarkers_prop = serializedObject.FindProperty(nameof(script.childrenMarkers));
            
            m_autoCreateLoader_prop = serializedObject.FindProperty(nameof(script.autoCreateLoader));
        }

        private GUILayoutOption m_glw_100 = GUILayout.Width(100);
        
        protected override void DrawAutoApplyGUI(InstancedObjectDataGenerator script)
        {
            EditorGUILayout.HelpBox("注意： Instancing 对象不支持 mesh 自身已有旋转的情况，会导致旋转被叠加成1个奇怪的角度", MessageType.Warning);
            EditorGUILayout.Space(5);
            
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("调试信息");
                EditorGUILayout.Space(5);
                EditorGUI.indentLevel++;

                var dt = (IOGroupConfig.EDataType)m_dataType_prop.intValue;
                if (dt == IOGroupConfig.EDataType.Tree)
                {
                    EditorGUILayout.PropertyField(m_debugTree_prop, new GUIContent("树"));
                    EditorGUILayout.Space(5);
                }
                
                EditorGUILayout.PropertyField(m_debugChildren_prop, new GUIContent("子对象"));
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(m_debugChildrenRate_prop, new GUIContent("子对象屏幕覆盖率"));
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(m_debugVolume_prop, new GUIContent("体积"));
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(m_cullCamera_prop, new GUIContent("剔除摄像机"));
                EditorGUILayout.Space(5);
                
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
            
            EditorGUILayout.PropertyField(m_dataType_prop, new GUIContent("数据类型"));
            EditorGUILayout.Space(5);

            var dataType = (IOGroupConfig.EDataType)m_dataType_prop.intValue;
            if (dataType == IOGroupConfig.EDataType.Tree)
            {
                EditorGUILayout.PropertyField(m_treeDepth_prop, new GUIContent("树深度"));
                EditorGUILayout.Space(5);
            }
            
            EditorGUILayout.PropertyField(m_lodMeshes_prop, new GUIContent("lod网格"));
            EditorGUILayout.Space(5);
            
            EditorGUILayout.PropertyField(m_lodMaterials_prop, new GUIContent("lod材质"));
            EditorGUILayout.Space(5);
            
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(m_childrenMarkers_prop, new GUIContent("扫描到的子标记器"));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space(5);

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

        private InstancedObjectDataGenerator[] m_batchArray;
        
        private void BatchExecuteGenerate()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.Space(5);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(new GUIContent("当前生成器数量"), m_glw_100))
                    {
                        m_batchArray = FindObjectsOfType<InstancedObjectDataGenerator>();
                    }
                    
                    GUILayout.Space(5);
                    if (GUILayout.Button(new GUIContent("全部生成"), m_glw_100))
                    {
                        m_batchArray = FindObjectsOfType<InstancedObjectDataGenerator>();
                        for (int t = m_batchArray.Length, i = 0; i < t; i++)
                        {
                            EditorUtility.DisplayProgressBar("全部生成", $"{i}/{t}", (float)i / t);
                            
                            var item = m_batchArray[i];
                            var editorObj = GUIUtils.GetEditorObjByRuntimeObj<InstancedObjectDataGeneratorInspector>(item);
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
        public void ExecuteGenerate(InstancedObjectDataGenerator script, bool isBatchExecute)
        {
            GenerateData(script, out IOGroupConfig groupConfig);
            CreateLoader(script, groupConfig);
            
            EditorUtility.ClearProgressBar();
            if (!isBatchExecute)
            {
                EditorUtility.DisplayDialog("实例化对象数据生成", "生成完成", "确定");
            }
        }
        
        //生成数据
        private void GenerateData(
            InstancedObjectDataGenerator script, 
            out IOGroupConfig groupConfig)
        {
            //确保当前场景的输出目录存在
            IOUtils.EnsureActiveSceneOutDir(
                out string meshOutDir, out string dataOutDir, 
                out string binaryOutDir, out string materialOutDir, 
                out string controlOutDir);

            string goName = script.gameObject.name.ToLower();
            
            //生成配置
            groupConfig = ScriptableObject.CreateInstance<IOGroupConfig>();
            
            //导出2进制数据
            ExportByteData(script, groupConfig, binaryOutDir, goName);
            
            //保存配置
            string groupConfigPath = $"{dataOutDir}/{goName}_io.asset";
            AssetDatabase.CreateAsset(groupConfig, groupConfigPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void ExportByteData(InstancedObjectDataGenerator script, IOGroupConfig config, string outDir, string goName)
        {
            config.dataType = script.dataType;
            
            MemoryStream stream = new MemoryStream();
            switch (script.dataType)
            {
                case IOGroupConfig.EDataType.Flat:
                    ExportFlatData(stream, script);
                    break;
                case IOGroupConfig.EDataType.Tree:
                    ExportTreeData(stream, script);
                    break;
            }
            
            //写入文件，并挂到配置上
            string byteDataPath = $"{outDir}/{goName}_io.bytes";
            File.WriteAllBytes(byteDataPath, stream.ToArray());
            stream.Close();
            
            AssetDatabase.Refresh();
            config.byteDataPath = IOUtils.FullPathToRelativePath(byteDataPath);
        }

        private void ExportFlatData(MemoryStream stream, InstancedObjectDataGenerator script)
        {
            int len = script.childrenMarkers.Count;
            MTStreamUtils.WriteInt(stream, len);
            for (int i = 0; i < len; i++)
            {
                var marker = script.childrenMarkers[i];
                
                var data = new IOTileData();
                data.bnd = marker.triggerBnd;

                var tf = marker.targetGo.transform;
                data.matrix = Matrix4x4.TRS(tf.position, tf.rotation, tf.lossyScale);
                
                data.lmc = MTEditorUtils.GetLightmapData(marker.targetGo);
                
                IOTileDataExt.Serialize(stream, data);
            }
        }
        
        private void ExportTreeData(MemoryStream stream, InstancedObjectDataGenerator script)
        {
            //初始化一个根节点
            var nodes = new List<IOQuadTreeNode>();
            var rootNode = new IOQuadTreeNode();
            rootNode.Initialize(0);
            nodes.Add(rootNode);

            //build 导出成 runtime 节点
            QuadTreeNodeUtil.ExportTreeNodes(
                script.quadTreeRoot, rootNode, nodes,
                (buildNode, node) =>
                {
                    node.holdBounds.Clear();
                    node.holdBounds.AddRange(buildNode.holdBounds);
                    
                    node.holdWorldMatrixs.Clear();
                    node.holdWorldMatrixs.AddRange(buildNode.holdWorldMatrixs);
                    
                    node.holdLightmapDatas.Clear();
                    node.holdLightmapDatas.AddRange(buildNode.holdLightmapDatas);
                });
            
            QuadTreeNodeUtil.SerializeTrees(stream, nodes);
        }
        
        
        private void CreateLoader(
            InstancedObjectDataGenerator script, 
            IOGroupConfig config)
        {
            if (!script.autoCreateLoader)
            {
                return;
            }
            
            //创建加载器对象
            string goName = $"{script.gameObject.name}";
            var loaderGo = new GameObject(goName);
            loaderGo.layer = script.gameObject.layer;
            var loader = loaderGo.AddComponent<IOLoader>();
            
            loader.cullCamera = Camera.main;
            
            string assetPath = AssetDatabase.GetAssetPath(config);
            loader.groupConfigPath = IOUtils.FullPathToRelativePath(assetPath);

            //因为从ab包加载的难度太大，所以这里需要为场景资源创建一个载体，确保我能够直接从场景中直接获取资源
            int total = script.lodMeshes.Length;
            var mfs = new MeshFilter[total];
            var mrs = new MeshRenderer[total];
            
            for (int i = 0; i < total; i++)
            {
                Mesh lodMesh = script.lodMeshes[i];

                //材质球只可能 1对n，或 n对n
                Material lodMaterial = script.lodMaterials[0];
                if (i > 0 && i < total)
                {
                    var tempMat = script.lodMaterials[i];
                    if (tempMat != null)
                    {
                        lodMaterial = tempMat;
                    }
                }
                
                //创建放在场景中的子对象 - 为了资源打包
                var child = new GameObject(lodMesh.name);
                child.transform.SetParent(loaderGo.transform);
                
                var mf = child.AddComponent<MeshFilter>();
                mfs[i] = mf;
                mf.sharedMesh = lodMesh;
                
                var mr = child.AddComponent<MeshRenderer>();
                mrs[i] = mr;
                mr.sharedMaterial = lodMaterial;
                
                //资源载体不用显示
                child.gameObject.SetActive(false);
            }
            
            loader.lodMeshes = mfs;
            loader.lodMaterials = mrs;

            //隐藏生成器节点
            if (script.gameObject.activeSelf)
            {
                script.gameObject.SetActive(false);
            }
        }


        private GUIStyle s_gs_rate;
        
        protected override void ExecuteOnSceneGUI(InstancedObjectDataGenerator script)
        {
            base.ExecuteOnSceneGUI(script);

            if (!script.enabled)
            {
                return;
            }
            
            if (s_gs_rate == null)
            {
                s_gs_rate = new GUIStyle();
                s_gs_rate.normal.textColor = Color.magenta;
            }
            
            if (script.debugChildren && script.debugChildrenRate)
            {
                foreach (var marker in script.childrenMarkers)
                {
                    //显示填充率
                    float rate = MTRuntimeUtils.ScreenCoverRate(script.cullCamera, marker.triggerBnd);
                    Handles.Label(marker.triggerBnd.center, $"{rate}", s_gs_rate);
                }
            }
        }
        
    }
}