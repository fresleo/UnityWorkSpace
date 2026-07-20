// Created By: WangYu  Date: 2023-11-23

/*
using System.Collections.Generic;
using System.IO;
using com.xknight.mt.Lib.Editor.MT.Jobs;
using com.xknight.mt.Lib.Editor.MT.Utils;
using com.xknight.mt.Lib.Runtime.MT.Log;
using com.xknight.mt.Lib.Runtime.MT.Serialize;
using com.xknight.mt.Lib.Runtime.MT.TerrainMesh;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.TerrainMesh
{
    [CustomEditor(typeof(TerrainMeshGenerator))]
    public class TerrainMeshGeneratorInspector : AbsInspector<TerrainMeshGenerator>
    {
        private SerializedProperty m_setting_prop;
        private SerializedProperty m_bakeMaterial_prop, m_bakeTextureSize_prop;
        private SerializedProperty m_outDir_prop;

        protected override void ExecuteOnEnable(TerrainMeshGenerator script)
        {
            base.ExecuteOnEnable(script);
            
            var pf = new PropertyFetcher<TerrainMeshGenerator>(serializedObject);

            m_setting_prop = pf.Find(x => x.setting);

            m_bakeMaterial_prop = pf.Find(x => x.bakeMaterial);
            m_bakeTextureSize_prop = pf.Find(x => x.bakeTextureSize);

            m_outDir_prop = pf.Find(x => x.outDir);
        }

        protected override void DrawAutoApplyGUI(TerrainMeshGenerator script)
        {
            EditorGUILayout.LabelField(new GUIContent("地形网格生成器"));
            EditorGUILayout.Space(5);
            
            EditorGUILayout.PropertyField(m_setting_prop, new GUIContent("构建设置"));
            EditorGUILayout.Space(5);
            
            EditorGUILayout.PropertyField(m_bakeMaterial_prop, new GUIContent("烘焙材质"));
            EditorGUILayout.Space(5);

            if (m_bakeMaterial_prop.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_bakeTextureSize_prop, new GUIContent("烘焙纹理尺寸"));
                EditorGUILayout.Space(5);
                EditorGUI.indentLevel--;
                
                m_bakeTextureSize_prop.intValue = Mathf.NextPowerOfTwo(m_bakeTextureSize_prop.intValue);
            }
            
            EditorGUILayout.PropertyField(m_outDir_prop, new GUIContent("输出目录"));
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("生成网格"))
            {
                ExecuteGenerate(script);
            }
            EditorGUILayout.Space(5);
        }
        

        private Terrain m_targetTerrain;
        private TerrainMeshBuildSetting m_buildSetting;
        private bool m_bakeMaterial;
        private int m_bakeTextureSize;
        private string m_outDir;
        
        //jobs
        private CreateMeshJob m_createMeshJob;
        private TriangulateJob m_triangulateJob;
        
        public void ExecuteGenerate(TerrainMeshGenerator script)
        {
            m_targetTerrain = script.GetComponent<Terrain>();
            if (m_targetTerrain == null)
            {
                MTLogger.LogError("缺少目标地形");
                return;
            }

            m_buildSetting = script.setting;
            if (m_buildSetting == null)
            {
                MTLogger.LogError("缺少构建设置");
                return;
            }
            if (m_buildSetting.lodSettings == null || m_buildSetting.lodSettings.Length == 0)
            {
                MTLogger.LogError("缺少可用的 lod 设置");
                return;
            }

            m_bakeMaterial = script.bakeMaterial;
            m_bakeTextureSize = script.bakeTextureSize;
            
            m_outDir = script.outDir;
            
            GenerateMesh();
        }
        
        private void GenerateMesh()
        {
            //该深度下的最大网格数
            int gridMax = (int)Mathf.Pow(2, m_buildSetting.quadTreeDepth);
            
            //获取地形的包围盒
            Vector3 tCenter = m_targetTerrain.transform.TransformPoint(m_targetTerrain.terrainData.bounds.center);
            Vector3 tSize = m_targetTerrain.terrainData.bounds.size;
            Bounds tBounds = new Bounds(tCenter, tSize);
            
            //最大细分数
            int maxSubdivision = 1;
            foreach (var lodSetting in m_buildSetting.lodSettings)
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
            m_createMeshJob = new CreateMeshJob(m_targetTerrain, tBounds, gridMax, gridMax, m_buildSetting.lodSettings);
            for (int i = 0; i < int.MaxValue; i++)
            {
                m_createMeshJob.Update();
                EditorUtility.DisplayProgressBar("创建数据", "创建网格...", m_createMeshJob.Progress);
                if (m_createMeshJob.IsDone)
                {
                    break;
                }
            }
            m_createMeshJob.EndProcess();
            
            //三角测量工作 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
            m_triangulateJob = new TriangulateJob(m_createMeshJob.scanners, minTriangleArea);
            for (int i = 0; i < int.MaxValue; i++)
            {
                m_triangulateJob.Update();
                EditorUtility.DisplayProgressBar("创建数据", "三角测量...", m_triangulateJob.Progress);
                if (m_triangulateJob.IsDone)
                {
                    break;
                }
            }
            
            //根据lod创建资源目录
            int lodTotal = m_buildSetting.lodSettings.Length;
            string[] lodDirs = new string[lodTotal];
            for (int i = 0; i < lodTotal; i++)
            {
                string folderName = $"{m_targetTerrain.name}_LOD{i}";
                string fullDir = $"{m_outDir}/{folderName}";
                if (!Directory.Exists(fullDir))
                {
                    Directory.CreateDirectory(fullDir);
                }
                lodDirs[i] = fullDir;
            }
            AssetDatabase.Refresh();

            //烘焙网格
            var containers = new List<TempMeshData>();
            for (int i = 0, t = m_triangulateJob.meshDatas.Length; i < t; i++)
            {
                EditorUtility.DisplayProgressBar("保存数据", "烘焙网格...", (float)i / t);
                
                TriangulateMeshData data = m_triangulateJob.meshDatas[i];
                for (int lod = 0; lod < data.lods.Length; ++lod)
                {
                    string dir = lodDirs[lod];
                    
                    string subDir = $"{dir}/Meshes";
                    if (!Directory.Exists(subDir))
                    {
                        Directory.CreateDirectory(subDir);
                        AssetDatabase.Refresh();
                    }

                    var mesh = SaveMesh(subDir, data.MeshId, data.lods[lod], m_buildSetting.genUV2);
                    
                    var container = new TempMeshData(lod, data.MeshId, mesh, data.lods[lod].uvMin, data.lods[lod].uvMax);
                    containers.Add(container);
                }
            }

            //构建预设
            var prefabRoots = new GameObject[lodTotal];
            if (m_bakeMaterial)
            {
                BakeMeshAndMaterial(containers, lodDirs, prefabRoots, m_targetTerrain);
            }
            else
            {
                BakeMesh(containers, lodDirs, prefabRoots);
            }

            //保存预设
            for (int i = prefabRoots.Length - 1; i >= 0; i--)
            {
                GameObject prefab = prefabRoots[i];
                string dir = lodDirs[i];
                string assetPath = $"{dir}/{m_targetTerrain.name}.prefab";
                
                PrefabUtility.SaveAsPrefabAsset(prefab, assetPath);
                DestroyImmediate(prefab);
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("网格创建器", "完成生成", "确定");
        }

        //只烘焙网格
        private void BakeMesh(List<TempMeshData> containers, string[] lodDirs, GameObject[] prefabRoots)
        {
            //材质球
            string dir = lodDirs[0];
            var matPaths = new List<string>();
            MTMatUtils.SaveMixMaterials(dir, dir, m_targetTerrain, matPaths);

            var mats = new List<Material>();
            foreach (string matPath in matPaths)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                mats.Add(mat);
            }

            //预设
            for (int i = 0, t = containers.Count; i < containers.Count; i++)
            {
                EditorUtility.DisplayProgressBar("保存数据", "生成预设...", (float)i / t);

                var container = containers[i];
                
                //创建根go
                if (prefabRoots[container.Lod] == null)
                {
                    prefabRoots[container.Lod] = new GameObject(m_targetTerrain.name);
                }

                var meshGo = new GameObject(container.MeshId.ToString());
                meshGo.transform.SetParent(prefabRoots[container.Lod].transform);

                var filter = meshGo.AddComponent<MeshFilter>();
                filter.mesh = container.Mtm;

                var renderer = meshGo.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = mats.ToArray();
            }
        }

        //保存网格
        private Mesh SaveMesh(string directory, int dataId, TriangulateMeshData.LOD data, bool genUV2)
        {
            Mesh mesh = new Mesh();
            mesh.vertices = data.vertices;
            mesh.normals = data.normals;
            mesh.uv = data.uvs;
            if (genUV2)
            {
                mesh.uv2 = data.uvs;
            }
            mesh.triangles = data.faces;

            string assetPath = $"{directory}/{dataId}.mesh";
            AssetDatabase.CreateAsset(mesh, assetPath);
            return mesh;
        }
        
        //烘焙网格和材质
        private void BakeMeshAndMaterial(List<TempMeshData> containers, string[] lodDirs, GameObject[] prefabRoots, Terrain currentTarget)
        {
            var albedoMats = new Material[2];
            var normalMats = new Material[2];
            MTMatUtils.GetBakeMaterials(currentTarget, albedoMats, normalMats);
            
            var texture = new Texture2D(m_bakeTextureSize, m_bakeTextureSize, TextureFormat.RGBA32, false);
            var renderTexture = RenderTexture.GetTemporary(m_bakeTextureSize, m_bakeTextureSize);
            
            for (int i = 0, t = containers.Count; i < t; i++)
            {
                EditorUtility.DisplayProgressBar("保存数据", "生成纹理+材质+预设...", (float)i / t);
                
                var container = containers[i];
                string dir = lodDirs[container.Lod];
                
                //纹理
                string texDir = $"{dir}/Textures";
                if (!Directory.Exists(texDir))
                {
                    Directory.CreateDirectory(texDir);
                    AssetDatabase.Refresh();
                }
                
                string albedoPath = $"{texDir}/albedo_{container.MeshId}.png";
                SaveBakedTexture(albedoPath, renderTexture, texture, albedoMats, container.ScaleOffset);
                string normalPath = $"{texDir}/normal_{container.MeshId}.png";
                SaveBakedTexture(normalPath, renderTexture, texture, normalMats, container.ScaleOffset);
                AssetDatabase.Refresh();
                
                var albedoTex = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
                var normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                
                //材质
                string matDir = $"{dir}/Materials";
                if (!Directory.Exists(matDir))
                {
                    Directory.CreateDirectory(matDir);
                    AssetDatabase.Refresh();
                }
                
                string matPath = $"{matDir}/mat_{container.MeshId}.mat";
                SaveBakedMaterial(matPath, albedoTex, normalTex, container.ScaleOffsetXY);
                AssetDatabase.Refresh();
                
                //预设
                if (prefabRoots[container.Lod] == null)
                {
                    prefabRoots[container.Lod] = new GameObject(currentTarget.name);
                }
                
                var meshGo = new GameObject(container.MeshId.ToString());
                meshGo.transform.SetParent(prefabRoots[container.Lod].transform);
                
                var filter = meshGo.AddComponent<MeshFilter>();
                filter.mesh = container.Mtm;
                
                var renderer = meshGo.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            }

            //回收资源
            RenderTexture.ReleaseTemporary(renderTexture);
            DestroyImmediate(texture);
            foreach (var mat in albedoMats)
            {
                DestroyImmediate(mat);
            }
            foreach (var mat in normalMats)
            {
                DestroyImmediate(mat);
            }
        }
        
        //保存烘焙纹理
        private void SaveBakedTexture(string path, RenderTexture renderTexture, Texture2D texture, Material[] mats, Vector4 scaleOffset)
        {
            //XXX 需要渲染两次才能使 UV 正常工作，原因不明
            for (int loop = 0; loop < 2; loop++)
            {
                Graphics.Blit(null, renderTexture, mats[0]);
                mats[0].SetVector("_BakeScaleOffset", scaleOffset);
                
                if (mats[1] != null)
                {
                    Graphics.Blit(null, renderTexture, mats[1]);
                    mats[1].SetVector("_BakeScaleOffset", scaleOffset);
                }

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = renderTexture;
                {
                    texture.ReadPixels(new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), 0, 0);
                    texture.Apply();
                }
                RenderTexture.active = previous;
            }

            byte[] tga = texture.EncodeToTGA();
            File.WriteAllBytes(path, tga);
        }

        //保存烘焙材质
        private void SaveBakedMaterial(string path, Texture2D albedo, Texture2D normal, Vector2 size)
        {
            Shader sha = Shader.Find(MTMatUtils.VT_Runtime_ShaderPath);
            Material mat = new Material(sha);
            
            Vector2 scale = new Vector2(1f / size.x, 1f / size.y);
            
            mat.SetTexture("_Diffuse", albedo);
            mat.SetTextureScale("_Diffuse", scale);
            mat.SetTexture("_Normal", normal);
            mat.SetTextureScale("_Normal", scale);
            mat.EnableKeyword("_NORMALMAP");
            
            AssetDatabase.CreateAsset(mat, path);
        }
        
    }
}
*/
