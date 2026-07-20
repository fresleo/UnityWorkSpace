// Created By: WangYu  Date: 2023-11-23

using System;
using System.Collections.Generic;
using System.IO;
using com.xknight.mt.Lib.Editor.MT.Jobs;
using com.xknight.mt.Lib.Editor.MT.Utils;
using com.xknight.mt.Lib.Runtime.MT.Serialize;
using com.xknight.mt.Lib.Runtime.MT.TerrainMesh;
using com.xknight.mt.Lib.Runtime.MT.Utils;
using com.xknight.mt.Lib.Runtime.MT.Log;
using com.xknight.mt.Lib.Runtime.MT.UnityComponent;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.TerrainMesh
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TerrainMeshDataGenerator))]
    public class TerrainMeshDataGeneratorInspector : AbsInspector<TerrainMeshDataGenerator>
    {
        private SerializedProperty m_setting_prop;
        private SerializedProperty m_autoCreateLoader_prop;

        protected override void ExecuteOnEnable(TerrainMeshDataGenerator script)
        {
            base.ExecuteOnEnable(script);

            var pf = new PropertyFetcher<TerrainMeshDataGenerator>(serializedObject);

            m_setting_prop = pf.Find(x => x.setting);
            m_autoCreateLoader_prop = pf.Find(x => x.autoCreateLoader);
        }

        private GUILayoutOption m_glw_100 = GUILayout.Width(100);
        
        protected override void DrawAutoApplyGUI(TerrainMeshDataGenerator script)
        {
            EditorGUILayout.LabelField(new GUIContent("地形网格数据生成器"));
            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(m_setting_prop, new GUIContent("构建设置"));
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

            if (GUILayout.Button(new GUIContent("清除当前场景的旧资源"), GUILayout.Width(150)))
            {
                IOUtils.DeleteActiveSceneOutDir();
            }
            EditorGUILayout.Space(5);
        }
        
        private TerrainMeshDataGenerator[] m_batchArray;

        private void BatchExecuteGenerate()
        {
            using (new GUILayout.VerticalScope("box"))
            {
                EditorGUILayout.Space(5);

                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(new GUIContent("当前生成器数量"), m_glw_100))
                    {
                        m_batchArray = FindObjectsOfType<TerrainMeshDataGenerator>();
                    }

                    GUILayout.Space(5);
                    if (GUILayout.Button(new GUIContent("全部生成"), m_glw_100))
                    {
                        m_batchArray = FindObjectsOfType<TerrainMeshDataGenerator>();
                        for (int t = m_batchArray.Length, i = 0; i < t; i++)
                        {
                            EditorUtility.DisplayProgressBar("全部生成", $"{i}/{t}", (float)i / t);

                            var item = m_batchArray[i];
                            var editorObj = GUIUtils.GetEditorObjByRuntimeObj<TerrainMeshDataGeneratorInspector>(item);
                            editorObj.ExecuteGenerate(item, true);
                        }
                        EditorUtility.ClearProgressBar();
                    }
                }

                if (m_batchArray != null)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField($"当前场景中生成器的数量：{m_batchArray.Length}", new GUIStyle("LinkLabel"));
                }

                EditorGUILayout.Space(5);
            }
        }
        

        /// <summary>
        /// 执行生成方法
        /// </summary>
        public void ExecuteGenerate(TerrainMeshDataGenerator script, bool isBatchExecute)
        {
            var targetTerrain = script.GetComponent<Terrain>();
            if (targetTerrain == null)
            {
                MTLogger.LogError("缺少目标地形");
                return;
            }
            
            if (script.setting == null)
            {
                MTLogger.LogError("缺少构建设置");
                return;
            }
            
            if (script.setting.lodSettings == null || script.setting.lodSettings.Length == 0)
            {
                MTLogger.LogError("缺少可用的 LOD 设置");
                return;
            }

            TerrainMeshConfig tmConfig = GenerateAssetAndData(script);
            CreateLoader(script, tmConfig);
            
            EditorUtility.ClearProgressBar();
            if (!isBatchExecute)
            {
                EditorUtility.DisplayDialog("地形数据生成", "生成完成", "确定");
            }
        }

        //生成资源和数据
        private TerrainMeshConfig GenerateAssetAndData(TerrainMeshDataGenerator script)
        {
            var targetTerrain = script.GetComponent<Terrain>();
            var buildSetting = script.setting;
            
            //该深度下的最大网格数
            int gridMax = (int)Mathf.Pow(2, buildSetting.quadTreeDepth);

            Vector3 tCenter = targetTerrain.transform.TransformPoint(targetTerrain.terrainData.bounds.center);
            Vector3 tSize = targetTerrain.terrainData.bounds.size;
            Bounds tBounds = new Bounds(tCenter, tSize);

            //最大细分数
            int maxSubdivision = 1;
            foreach (var lodSetting in buildSetting.lodSettings)
            {
                if (lodSetting.subdivision > maxSubdivision)
                {
                    maxSubdivision = lodSetting.subdivision;
                }
            }

            //最大细分段数
            float maxSubdivisionSegments = Mathf.Pow(2, maxSubdivision) * gridMax;

            //最小边长
            float minEdgeLen = Mathf.Max(tSize.x, tSize.z) / maxSubdivisionSegments;
            //三角形的最小面积
            float minTriangleArea = minEdgeLen * minEdgeLen / 8f;

            //创建工作 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
            var createDataJob = new CreateDataJob(targetTerrain, tBounds, buildSetting.quadTreeDepth, buildSetting.lodSettings, minEdgeLen * 0.5f);
            for (int i = 0; i < int.MaxValue; i++)
            {
                createDataJob.Update();
                EditorUtility.DisplayProgressBar("创建数据", "创建数据...", createDataJob.Progress);
                if (createDataJob.IsDone)
                {
                    break;
                }
            }
            createDataJob.EndProcess();

            //三角测量工作 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
            var triangulateDataJob = new TriangulateDataJob(createDataJob.scanners, minTriangleArea);
            for (int i = 0; i < int.MaxValue; i++)
            {
                triangulateDataJob.Update();
                EditorUtility.DisplayProgressBar("创建数据", "三角测量...", triangulateDataJob.Progress);
                if (triangulateDataJob.IsDone)
                {
                    break;
                }
            }

            //确保当前场景的输出目录存在
            IOUtils.EnsureActiveSceneOutDir(
                out string meshOutDir, out string dataOutDir, 
                out string binaryOutDir, out string materialOutDir, 
                out string controlOutDir);
            
            //地形名字
            string terrainName = targetTerrain.name;
            terrainName = terrainName.ToLower();

            //创建数据配置
            var tmConfig = ScriptableObject.CreateInstance<TerrainMeshConfig>();
            tmConfig.meshDataPack = buildSetting.dataPack;
            
            //mesh2进制文件的前缀
            string fullMeshPrefix = $"{meshOutDir}/{terrainName}";
            tmConfig.meshPrefix = IOUtils.FullPathToRelativePath(fullMeshPrefix);
            
            //构建用4叉树的根
            var quadTreeRoot = new TMQuadTreeBuildNode(buildSetting.quadTreeDepth, tBounds.min, tBounds.max, Vector2.zero, Vector2.one);

            //写出mesh的2进制数据
            ExportMeshBytes(buildSetting, targetTerrain, triangulateDataJob, quadTreeRoot, meshOutDir);
            //导出树的2进制数据`
            ExportTreeBytes(tmConfig, quadTreeRoot, binaryOutDir, terrainName);
            //导出材质球
            ExportMaterials(tmConfig, targetTerrain, materialOutDir, controlOutDir);
            //导出高度图
            ExportHeightMapBytes(tmConfig, targetTerrain, binaryOutDir, terrainName);
            //导出细节
            // ExportDetailBytes(tmConfig, targetTerrain, binaryOutDir);

            //保存Lightmap配置数据
            SaveLightmapData(tmConfig, targetTerrain);

            //创建数据配置文件
            string ttPath = $"{dataOutDir}/{terrainName}.asset";
            AssetDatabase.CreateAsset(tmConfig, ttPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            return tmConfig;
        }

        //导出mesh的2进制数据
        private void ExportMeshBytes(
            TerrainMeshBuildSetting buildSetting, Terrain targetTerrain, 
            TriangulateDataJob triangulateDataJob, TMQuadTreeBuildNode quadTreeRoot, 
            string outDir)
        {
            int packed = 0;
            int startMeshId = -1;
            MemoryStream stream = new MemoryStream();

            string terrainName = targetTerrain.name.ToLower();

            for (int i = 0, t = triangulateDataJob.meshDatas.Length; i < t; i++)
            {
                EditorUtility.DisplayProgressBar("导出数据", "Mesh的2进制...", (float)i / t);

                TriangulateMeshData data = triangulateDataJob.meshDatas[i];
                if (!quadTreeRoot.AddMesh(data))
                {
                    MTLogger.LogError($"mesh 数据无法插入到4叉树中 : {data.MeshId}");
                }

                if (startMeshId < 0)
                {
                    startMeshId = data.MeshId;
                }

                //能整除包限制时，就该分新包了
                int subpackage = packed % buildSetting.dataPack;
                if (subpackage == 0)
                {
                    if (stream.Length > 0)
                    {
                        string writePath = $"{outDir}/{terrainName}_{startMeshId}.bytes";
                        File.WriteAllBytes(writePath, stream.ToArray());
                        stream.Close();
                        startMeshId = data.MeshId;
                        //创建新的流，接着往后写
                        stream = new MemoryStream();
                    }

                    packed = 0;
                    //预写入 offset
                    for (int j = 0; j < buildSetting.dataPack; j++)
                    {
                        MTStreamUtils.WriteInt(stream, 0);
                    }
                }

                //offset
                int reserve = (int)stream.Position;
                stream.Position = packed * sizeof(int);
                MTStreamUtils.WriteInt(stream, reserve);
                stream.Position = reserve;
                MTMeshUtils.Serialize(stream, data.lods[0]);

                packed++;
            }

            //最后1个
            if (stream.Length > 0 && startMeshId >= 0)
            {
                string writePath = $"{outDir}/{terrainName}_{startMeshId}.bytes";
                File.WriteAllBytes(writePath, stream.ToArray());
            }

            stream.Close();
            AssetDatabase.Refresh();
        }

        //导出树的2进制数据
        private void ExportTreeBytes(TerrainMeshConfig tmConfig, TMQuadTreeBuildNode quadTreeRoot, string outDir, string terrainName)
        {
            EditorUtility.DisplayProgressBar("导出数据", "树的2进制...", 0);

            //初始化一个根节点
            var nodes = new List<TMQuadTreeNode>();
            var rootNode = new TMQuadTreeNode();
            rootNode.Initialize(0);
            nodes.Add(rootNode);

            //build 导出成 runtime 节点
            ExportTreeNodes(quadTreeRoot, rootNode, nodes);

            MemoryStream stream = new MemoryStream();
            SerializeTrees(stream, nodes);
            string treeDataPath = $"{outDir}/{terrainName}_tree_data.bytes";
            File.WriteAllBytes(treeDataPath, stream.ToArray());
            stream.Close();

            AssetDatabase.Refresh();
            tmConfig.treeDataPath = IOUtils.FullPathToRelativePath(treeDataPath);
        }

        private void ExportTreeNodes(TMQuadTreeBuildNode buildNodes, TMQuadTreeNode runtimeNodes, List<TMQuadTreeNode> nodeList)
        {
            if (buildNodes == null)
            {
                return;
            }

            //同步数据
            runtimeNodes.bnd = buildNodes.bnd;
            runtimeNodes.meshId = buildNodes.meshId;
            runtimeNodes.lodLv = buildNodes.lodLv;

            //处理子节点
            if (buildNodes.childrenNodes != null)
            {
                int childCount = buildNodes.childrenNodes.Length;
                runtimeNodes.children = new int[childCount];

                //创建子节点，并分配 cid (就是序列号)
                for (int i = 0; i < childCount; i++)
                {
                    int cid = nodeList.Count;
                    var child = new TMQuadTreeNode();
                    child.Initialize(cid);
                    nodeList.Add(child);

                    runtimeNodes.children[i] = child.cellId;
                }

                //递归创建子节点
                for (int i = 0; i < childCount; i++)
                {
                    var childIdx = runtimeNodes.children[i];

                    ExportTreeNodes(buildNodes.childrenNodes[i], nodeList[childIdx], nodeList);
                }
            }
        }

        private void SerializeTrees(MemoryStream stream, List<TMQuadTreeNode> nodeList)
        {
            MTStreamUtils.WriteInt(stream, nodeList.Count);
            foreach (var node in nodeList)
            {
                node.Serialize(stream);
            }
        }

        //导出材质球
        private void ExportMaterials(TerrainMeshConfig tmConfig, Terrain targetTerrain, string matOutDir, string texOutDir)
        {
            var mixMats = new List<string>();
            var bakeAlbedoMats = new List<string>();
            var bakeBumpMats = new List<string>();
            MTMatUtils.SaveMixMaterials(matOutDir, texOutDir, targetTerrain, mixMats);
            MTMatUtils.SaveVTBakeMaterials(matOutDir, texOutDir, targetTerrain, bakeAlbedoMats, bakeBumpMats);

            tmConfig.mixMatPaths = IOUtils.FullPathsToRelativePaths(mixMats.ToArray());
            tmConfig.bakeVTDiffuseMatPaths = IOUtils.FullPathsToRelativePaths(bakeAlbedoMats.ToArray());
            tmConfig.bakeVTNormalMatPaths = IOUtils.FullPathsToRelativePaths(bakeBumpMats.ToArray());
            
            CreateVTRuntimeMaterial(tmConfig, matOutDir, targetTerrain);
        }

        //创建vt的运行时材质球
        private void CreateVTRuntimeMaterial(TerrainMeshConfig mtConfig, string outDir, Terrain targetTerrain)
        {
            Shader sha = Shader.Find(MTMatUtils.VT_Runtime_ShaderPath);
            Material mat = new Material(sha);

            mat.EnableKeyword("_NORMALMAP");

            //地形名
            string terrainName = targetTerrain.name;
            terrainName = terrainName.ToLower();
            
            string vtMatPath = $"{outDir}/{terrainName}_vt.mat";
            AssetDatabase.CreateAsset(mat, vtMatPath);

            mtConfig.bakedVTMatPath = IOUtils.FullPathToRelativePath(vtMatPath);
        }

        //导出高度图的2进制数据
        private void ExportHeightMapBytes(TerrainMeshConfig tmConfig, Terrain targetTerrain, string outDir, string terrainName)
        {
            EditorUtility.DisplayProgressBar("导出数据", "高度图...", 0);

            tmConfig.heightmapWorldY = targetTerrain.transform.position.y;
            tmConfig.heightmapResolution = targetTerrain.terrainData.heightmapResolution;
            tmConfig.heightmapScale = targetTerrain.terrainData.heightmapScale;

            int heightmapSize = tmConfig.heightmapResolution;

            float[,] heightData = targetTerrain.terrainData.GetHeights(0, 0, heightmapSize, heightmapSize);
            byte[] heightBytes = new byte[heightmapSize * heightmapSize * 2];

            for (int hz = 0; hz < heightmapSize; hz++)
            {
                for (int hx = 0; hx < heightmapSize; hx++)
                {
                    //高度值
                    float heightVal = heightData[hz, hx] * 255f;
                    //高位存整数，低位存小数
                    byte h = (byte)Mathf.FloorToInt(heightVal);
                    byte l = (byte)Mathf.FloorToInt((heightVal - h) * 255f);
                    //写入
                    int idx = hz * 2 * heightmapSize + hx * 2;
                    heightBytes[idx] = h;
                    heightBytes[idx + 1] = l;
                }
            }

            string bytesPath = $"{outDir}/{terrainName}_height_map.bytes";

            File.WriteAllBytes(bytesPath, heightBytes);
            AssetDatabase.Refresh();

            tmConfig.heightMapPath = IOUtils.FullPathToRelativePath(bytesPath);
        }

        /*
        //导出细节的2进制数据
        private void ExportDetailBytes(TerrainMeshConfig tmConfig, Terrain targetTerrain, string outDir)
        {
            EditorUtility.DisplayProgressBar("导出数据", "地表细节...", 0);

            TerrainData td = targetTerrain.terrainData;
            var source = td.detailPrototypes;

            var layerDatas = new List<DetailLayerData>();
            int[][,] layerDensitys = new int[source.Length][,];

            for (int l = 0; l < source.Length; l++)
            {
                var sourceLayer = source[l];
                if (sourceLayer.prototype == null)
                {
                    continue;
                }

                var layerName = sourceLayer.prototype.name;

                layerDensitys[l] = td.GetDetailLayer(0, 0, td.detailWidth, td.detailHeight, l);

                var layer = new DetailLayerData();
                layerDatas.Add(layer);

                layer.prototype = sourceLayer.prototype;
                layer.minWidth = sourceLayer.minWidth;
                layer.maxWidth = sourceLayer.maxWidth;
                layer.minHeight = sourceLayer.minHeight;
                layer.maxHeight = sourceLayer.maxHeight;
                layer.noiseSpread = sourceLayer.noiseSpread;
                layer.healthyColor = sourceLayer.healthyColor;
                layer.dryColor = sourceLayer.dryColor;
            }

            tmConfig.detailPrototypes = new DetailLayerData[layerDatas.Count];
            tmConfig.detailWidth = td.detailWidth;
            tmConfig.detailHeight = td.detailHeight;
            tmConfig.detailResolutionPerPatch = td.detailResolutionPerPatch;
            if (tmConfig.detailHeight / tmConfig.detailResolutionPerPatch > byte.MaxValue)
            {
                MTLogger.LogError("导出细节失败，DetailResolutionPerPatch 数值太小");
                return;
            }

            MemoryStream detailStream = new MemoryStream();
            int patchX = Mathf.CeilToInt((float)tmConfig.detailWidth / tmConfig.detailResolutionPerPatch);
            int patchY = Mathf.CeilToInt((float)tmConfig.detailHeight / tmConfig.detailResolutionPerPatch);
            byte[] patchBlock = new byte[tmConfig.detailResolutionPerPatch * tmConfig.detailResolutionPerPatch];
            int[] patchDataOffsets = new int[patchX * patchY * layerDatas.Count];

            //先占位，后面会再把有密度的地方覆写进来
            for (int i = 0; i < patchDataOffsets.Length; i++)
            {
                MTStreamUtils.WriteInt(detailStream, -1);
            }

            for (int i = 0; i < layerDatas.Count; i++)
            {
                var layerData = layerDatas[i];
                tmConfig.detailPrototypes[i] = layerData;

                layerData.maxDensity = 0;

                // Texture2D debugTex = new Texture2D(mtData.DetailWidth, mtData.DetailHeight);

                for (int py = 0; py < patchY; py++)
                {
                    for (int px = 0; px < patchX; px++)
                    {
                        int maxDensity = 0;
                        for (int subPy = 0; subPy < tmConfig.detailResolutionPerPatch; subPy++)
                        {
                            for (int subPx = 0; subPx < tmConfig.detailResolutionPerPatch; subPx++)
                            {
                                int hy = py * tmConfig.detailResolutionPerPatch + subPy;
                                int hx = px * tmConfig.detailResolutionPerPatch + subPx;
                                hy = Mathf.Min(hy, tmConfig.detailHeight);
                                hx = Mathf.Min(hx, tmConfig.detailWidth);

                                int density = layerDensitys[i][hy, hx];
                                if (density > maxDensity)
                                {
                                    maxDensity = density;
                                }

                                patchBlock[subPy * tmConfig.detailResolutionPerPatch + subPx] = (byte)density;
                            }
                        }

                        var offsetDataIdx = i * patchX * patchY + py * patchX + px;
                        if (maxDensity > 0)
                        {
                            patchDataOffsets[offsetDataIdx] = (int)detailStream.Position;
                            detailStream.Write(patchBlock, 0, patchBlock.Length);
                        }
                        else
                        {
                            patchDataOffsets[offsetDataIdx] = -1;
                        }

                        if (maxDensity > layerData.maxDensity)
                        {
                            layerData.maxDensity = maxDensity;
                        }
                    }
                }

                // var tgaBytes = debugTex.EncodeToTGA();
                // FileStream stream = File.Open($"{topFullPath}/debug_desity_texture.tga", FileMode.Create);
                // stream.Write(tgaBytes, 0, tgaBytes.Length);
                // stream.Close();
                // AssetDatabase.Refresh();
            }

            detailStream.Position = 0;
            foreach (var item in patchDataOffsets)
            {
                MTStreamUtils.WriteInt(detailStream, item);
            }

            string bytesPath = $"{outDir}/details.bytes";

            File.WriteAllBytes(bytesPath, detailStream.ToArray());
            detailStream.Close();
            AssetDatabase.Refresh();

            tmConfig.detailLayers = AssetDatabase.LoadAssetAtPath(bytesPath, typeof(TextAsset)) as TextAsset;
        }
        */

        //保存Lightmap配置数据
        private void SaveLightmapData(TerrainMeshConfig tmConfig, Terrain targetTerrain)
        {
            tmConfig.lightmapData = new LightmapConfig();

            StaticEditorFlags sef = GameObjectUtility.GetStaticEditorFlags(targetTerrain.gameObject);
            tmConfig.lightmapData.baked = FlagsUtil<StaticEditorFlags>.Has(sef, StaticEditorFlags.ContributeGI);

            if (tmConfig.lightmapData.baked)
            {
                tmConfig.lightmapData.index = targetTerrain.lightmapIndex;
                tmConfig.lightmapData.scaleOffset = targetTerrain.lightmapScaleOffset;
            }
        }


        //创建加载器
        private void CreateLoader(TerrainMeshDataGenerator script, TerrainMeshConfig tmConfig)
        {
            if (!script.autoCreateLoader)
            {
                return;
            }
            
            //创建加载器对象
            string goName = $"{script.gameObject.name}";
            var loaderGo = new GameObject(goName);
            loaderGo.layer = script.gameObject.layer;
            var loader = loaderGo.AddComponent<TMLoader>();
            
            loader.cullCamera = Camera.main;
            string assetPath = AssetDatabase.GetAssetPath(tmConfig);
            loader.tmcPath = IOUtils.FullPathToRelativePath(assetPath);

            //因为这个配置的目录和其它资源毫无关联，所以这里用直接配置文件名的方式来进行配置
            var guids = AssetDatabase.FindAssets($"t:{nameof(LODPolicy)}");
            if (guids.Length > 0)
            {
                assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                loader.lpPath = Path.GetFileName(assetPath);
            }

            if (script.gameObject.activeSelf)
            {
                script.gameObject.SetActive(false);
            }
        }
        
    }
}