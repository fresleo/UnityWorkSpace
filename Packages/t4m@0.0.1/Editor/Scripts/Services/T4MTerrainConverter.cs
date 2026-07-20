/********************************************************
 * File:    T4MTerrainConverter.cs
 * Description: T4M 地形转换服务（Unity Terrain 和 Obj 转换）
 *********************************************************/

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace T4MEditor.Services
{
    /// <summary>
    /// 转换进度回调
    /// </summary>
    public delegate void ConversionProgressCallback(float progress, string message);

    /// <summary>
    /// 转换配置
    /// </summary>
    [Serializable]
    public class T4MConversionSettings
    {
        /// <summary>
        /// 输出文件夹
        /// </summary>
        public string OutputFolder = "Assets/ArtRes/T4MOBJ";

        /// <summary>
        /// 地形名称
        /// </summary>
        public string TerrainName = "";

        /// <summary>
        /// T4M 分辨率（顶点数）
        /// </summary>
        public int Resolution = 90;

        /// <summary>
        /// 是否保留原始贴图
        /// </summary>
        public bool KeepTexture = true;

        /// <summary>
        /// 是否删除原始 Unity Terrain
        /// </summary>
        public bool DeleteOriginTerrain = false;

        /// <summary>
        /// 是否隐藏原始 Unity Terrain
        /// </summary>
        public bool HideOriginTerrain = true;

        /// <summary>
        /// 是否需要创建新材质
        /// </summary>
        public bool CreateNewMaterial = true;

        /// <summary>
        /// 新材质路径
        /// </summary>
        public string NewMaterialPath = "Assets/ArtRes/T4MOBJ/Materials/";

        /// <summary>
        /// 目标图层索引
        /// </summary>
        public int TargetLayerIndex = 0;
    }

    /// <summary>
    /// 地形转换服务，处理 Unity Terrain 到 T4M 的转换
    /// </summary>
    public static class T4MTerrainConverter
    {
        /// <summary>
        /// 将 Unity Terrain 转换为 T4M 对象
        /// </summary>
        /// <param name="terrain">Unity 地形</param>
        /// <param name="settings">转换设置</param>
        /// <param name="progressCallback">进度回调</param>
        /// <returns>转换后的 GameObject</returns>
        public static GameObject ConvertUnityTerrain(Terrain terrain, T4MConversionSettings settings, ConversionProgressCallback progressCallback = null)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                Debug.LogError("[T4MTerrainConverter] Terrain 或 TerrainData 为空");
                return null;
            }

            string terrainName = string.IsNullOrEmpty(settings.TerrainName) ? terrain.name : settings.TerrainName;

            // 确保目录存在
            EnsureDirectoriesExist(settings.OutputFolder);

            progressCallback?.Invoke(0.1f, "生成网格数据...");

            // 生成网格
            TerrainData terrainData = terrain.terrainData;
            Mesh mesh = GenerateTerrainMesh(terrainData, settings.Resolution);

            progressCallback?.Invoke(0.4f, "导出 OBJ 文件...");

            // 获取唯一文件名
            string finalName = GetUniqueFileName(settings.OutputFolder + "Terrains/", terrainName);

            // 导出 OBJ
            string objPath = ExportToObj(mesh, finalName);

            progressCallback?.Invoke(0.6f, "创建控制图...");

            // 创建控制图
            string controlMapPath = settings.OutputFolder + "Terrains/Texture/" + finalName + ".png";
            Texture2D controlMap;

            if (settings.KeepTexture)
            {
                controlMap = T4MControlMapService.CreateFromUnityTerrain(terrain, controlMapPath);
            }
            else
            {
                controlMap = T4MControlMapService.CreateControlMap(controlMapPath, 512);
            }

            T4MControlMapService.SetupTextureImporter(controlMapPath);

            progressCallback?.Invoke(0.8f, "创建材质和预制体...");

            // 导入 OBJ 并创建预制体
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(objPath, ImportAssetOptions.ForceUpdate);

            // 设置 OBJ 导入器
            ModelImporter modelImporter = AssetImporter.GetAtPath(objPath) as ModelImporter;
            if (modelImporter != null)
            {
                modelImporter.isReadable = true;
                modelImporter.SaveAndReimport();
            }

            // 创建材质
            string materialPath = settings.OutputFolder + "Terrains/Material/" + finalName + ".mat";
            Material material = T4MMaterialService.CreateT4MMaterial(materialPath);
            T4MMaterialService.SetControlMap(material, controlMap);

            // 从原 Terrain 复制纹理到材质
            if (settings.KeepTexture)
            {
                CopyTerrainLayersToMaterial(terrain, material);
            }

            // 创建 GameObject
            GameObject meshObject = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
            GameObject instance = UnityEngine.Object.Instantiate(meshObject);
            instance.name = finalName;

            // 添加 T4MObjSC 组件
            var t4mObj = instance.AddComponent<T4MObjSC>();
            t4mObj.T4MMaterial = material;
            t4mObj.ConvertType = "UT";

            // 设置材质
            var renderer = instance.GetComponentInChildren<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            // 添加碰撞体
            var meshFilter = instance.GetComponentInChildren<MeshFilter>();
            if (meshFilter != null)
            {
                var collider = instance.GetComponentInChildren<MeshCollider>();
                if (collider == null)
                {
                    collider = meshFilter.gameObject.AddComponent<MeshCollider>();
                }
                collider.sharedMesh = meshFilter.sharedMesh;
            }

            // 设置位置
            instance.transform.position = terrain.transform.position;

            // 处理原 Terrain
            if (settings.DeleteOriginTerrain)
            {
                UnityEngine.Object.DestroyImmediate(terrain.gameObject);
            }
            else if (settings.HideOriginTerrain)
            {
                terrain.gameObject.SetActive(false);
            }

            // 创建预制体
            string prefabPath = settings.OutputFolder + "Terrains/" + finalName + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);

            progressCallback?.Invoke(1f, "转换完成");

            return instance;
        }

        /// <summary>
        /// 将普通模型转换为 T4M 对象
        /// </summary>
        /// <param name="modelTransform">模型 Transform</param>
        /// <param name="settings">转换设置</param>
        /// <param name="progressCallback">进度回调</param>
        /// <returns>转换后的 GameObject</returns>
        public static GameObject ConvertModelToT4M(Transform modelTransform, T4MConversionSettings settings, ConversionProgressCallback progressCallback = null)
        {
            if (modelTransform == null)
            {
                Debug.LogError("[T4MTerrainConverter] 模型 Transform 为空");
                return null;
            }

            var meshFilter = modelTransform.GetComponent<MeshFilter>();
            var meshRenderer = modelTransform.GetComponent<MeshRenderer>();

            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                Debug.LogError("[T4MTerrainConverter] 模型没有 MeshFilter 或 Mesh");
                return null;
            }

            string modelName = string.IsNullOrEmpty(settings.TerrainName) ? modelTransform.name : settings.TerrainName;

            // 确保目录存在
            EnsureDirectoriesExist(settings.OutputFolder, true);

            progressCallback?.Invoke(0.2f, "准备网格...");

            // 获取唯一文件名
            string finalName = GetUniqueFileName(settings.OutputFolder + "Models/", modelName);

            progressCallback?.Invoke(0.4f, "创建控制图...");

            // 创建控制图
            string controlMapPath = settings.OutputFolder + "Models/Texture/" + finalName + ".png";
            Texture2D controlMap = T4MControlMapService.CreateControlMap(controlMapPath, 512);
            T4MControlMapService.SetupTextureImporter(controlMapPath);

            progressCallback?.Invoke(0.6f, "创建材质...");

            // 创建或获取材质
            Material material;
            if (settings.CreateNewMaterial)
            {
                string materialPath = settings.OutputFolder + "Models/Material/" + finalName + ".mat";
                material = T4MMaterialService.CreateT4MMaterial(materialPath);
            }
            else
            {
                material = meshRenderer?.sharedMaterial;
                if (material == null)
                {
                    string materialPath = settings.OutputFolder + "Models/Material/" + finalName + ".mat";
                    material = T4MMaterialService.CreateT4MMaterial(materialPath);
                }
            }

            T4MMaterialService.SetControlMap(material, controlMap);

            progressCallback?.Invoke(0.8f, "配置 T4M 组件...");

            // 添加 T4MObjSC 组件
            var t4mObj = modelTransform.GetComponent<T4MObjSC>();
            if (t4mObj == null)
            {
                t4mObj = modelTransform.gameObject.AddComponent<T4MObjSC>();
            }
            t4mObj.T4MMaterial = material;
            t4mObj.ConvertType = "OBJ";

            // 设置材质
            if (meshRenderer != null)
            {
                meshRenderer.sharedMaterial = material;
            }

            // 添加碰撞体
            var collider = modelTransform.GetComponent<MeshCollider>();
            if (collider == null)
            {
                collider = modelTransform.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = meshFilter.sharedMesh;
            }

            progressCallback?.Invoke(1f, "转换完成");

            return modelTransform.gameObject;
        }

        #region Private Methods

        /// <summary>
        /// 确保输出目录结构存在，不存在则创建
        /// </summary>
        /// <param name="baseFolder">基础文件夹路径</param>
        /// <param name="isModel">是否为模型转换（false 为地形转换）</param>
        private static void EnsureDirectoriesExist(string baseFolder, bool isModel = false)
        {
            string subFolder = isModel ? "Models" : "Terrains";

            string[] dirs = new[]
            {
                baseFolder + subFolder + "/",
                baseFolder + subFolder + "/Material/",
                baseFolder + subFolder + "/Texture/",
                baseFolder + subFolder + "/Meshes/"
            };

            foreach (string dir in dirs)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 获取唯一的文件名，避免覆盖已有文件
        /// </summary>
        /// <param name="folder">目标文件夹</param>
        /// <param name="baseName">基础文件名</param>
        /// <returns>唯一的文件名（可能带数字后缀）</returns>
        private static string GetUniqueFileName(string folder, string baseName)
        {
            if (!File.Exists(folder + baseName + ".prefab"))
            {
                return baseName;
            }

            int num = 1;
            while (File.Exists(folder + baseName + num + ".prefab"))
            {
                num++;
            }

            return baseName + num;
        }

        /// <summary>
        /// 根据 TerrainData 生成网格
        /// </summary>
        /// <param name="terrainData">Unity 地形数据</param>
        /// <param name="resolution">目标分辨率（顶点数）</param>
        /// <returns>生成的网格</returns>
        private static Mesh GenerateTerrainMesh(TerrainData terrainData, int resolution)
        {
            int w = terrainData.heightmapResolution;
            int h = terrainData.heightmapResolution;
            Vector3 meshScale = terrainData.size;
            meshScale = new Vector3(meshScale.x / (resolution - 1), meshScale.y, meshScale.z / (resolution - 1));

            float tScale = (float)w / resolution;
            float[,] heights = terrainData.GetHeights(0, 0, w, h);

            w = resolution;
            h = resolution;

            Vector3[] vertices = new Vector3[w * h];
            Vector2[] uvs = new Vector2[w * h];
            Vector3[] normals = new Vector3[w * h];
            int[] triangles = new int[(w - 1) * (h - 1) * 6];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int index = y * w + x;
                    int hx = Mathf.CeilToInt(x * tScale);
                    int hy = Mathf.CeilToInt(y * tScale);
                    hx = Mathf.Clamp(hx, 0, terrainData.heightmapResolution - 1);
                    hy = Mathf.Clamp(hy, 0, terrainData.heightmapResolution - 1);

                    vertices[index] = Vector3.Scale(meshScale, new Vector3(x, heights[hx, hy], y));
                    uvs[index] = new Vector2(y / (h - 1f), x / (w - 1f));
                    normals[index] = terrainData.GetInterpolatedNormal(x / (w - 1f), y / (h - 1f));
                }
            }

            int triIndex = 0;
            for (int y = 0; y < h - 1; y++)
            {
                for (int x = 0; x < w - 1; x++)
                {
                    triangles[triIndex++] = (y * w) + x;
                    triangles[triIndex++] = ((y + 1) * w) + x;
                    triangles[triIndex++] = (y * w) + x + 1;

                    triangles[triIndex++] = ((y + 1) * w) + x;
                    triangles[triIndex++] = ((y + 1) * w) + x + 1;
                    triangles[triIndex++] = (y * w) + x + 1;
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// 将网格导出为 OBJ 文件
        /// </summary>
        /// <param name="mesh">要导出的网格</param>
        /// <param name="name">文件名（不含扩展名）</param>
        /// <returns>导出的文件路径</returns>
        private static string ExportToObj(Mesh mesh, string name)
        {
            string path = name + ".obj";

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            using (StreamWriter sw = new StreamWriter(path))
            {
                sw.WriteLine("# T4M File");

                foreach (Vector3 v in mesh.vertices)
                {
                    sw.WriteLine($"v {v.x} {v.y} {v.z}");
                }

                foreach (Vector2 uv in mesh.uv)
                {
                    sw.WriteLine($"vt {uv.x} {uv.y}");
                }

                int[] triangles = mesh.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int v1 = triangles[i] + 1;
                    int v2 = triangles[i + 1] + 1;
                    int v3 = triangles[i + 2] + 1;
                    sw.WriteLine($"f {v1}/{v1} {v2}/{v2} {v3}/{v3}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            return path;
        }

        /// <summary>
        /// 将 Unity Terrain 的图层复制到 T4M 材质
        /// </summary>
        /// <param name="terrain">源 Unity 地形</param>
        /// <param name="material">目标 T4M 材质</param>
        private static void CopyTerrainLayersToMaterial(Terrain terrain, Material material)
        {
            if (terrain == null || terrain.terrainData == null || material == null) return;

            TerrainLayer[] layers = terrain.terrainData.terrainLayers;
            if (layers == null) return;

            for (int i = 0; i < Mathf.Min(layers.Length, 6); i++)
            {
                if (layers[i] != null)
                {
                    T4MMaterialService.SetLayerTextures(
                        material,
                        i,
                        layers[i].diffuseTexture,
                        layers[i].normalMapTexture,
                        layers[i].tileSize
                    );
                }
            }
        }

        #endregion
    }
}
