/********************************************************
 * File:    T4MMaterialService.cs
 * Description: T4M 材质服务（图层读写、UV4 切换）
 *********************************************************/

using T4MEditor.Data;
using UnityEditor;
using UnityEngine;

namespace T4MEditor.Services
{
    /// <summary>
    /// 材质服务，处理材质图层的读写和 UV4 切换
    /// </summary>
    public static class T4MMaterialService
    {
        /// <summary>
        /// 默认 Shader 名称
        /// </summary>
        public const string DefaultShaderName = "XKnight/Scene/Terrain";

        /// <summary>
        /// UV4 关键字
        /// </summary>
        private const string UV4_KEYWORD = "_USE_UV4";

        /// <summary>
        /// 将图层数据应用到材质
        /// </summary>
        /// <param name="mat">目标材质</param>
        /// <param name="layers">图层数组</param>
        public static void ApplyLayers(Material mat, T4MTerrainLayer[] layers)
        {
            if (mat == null || layers == null) return;

            foreach (var layer in layers)
            {
                if (layer != null)
                {
                    layer.ApplyToMaterial(mat);
                }
            }

            EditorUtility.SetDirty(mat);
        }

        /// <summary>
        /// 从材质读取图层数据
        /// </summary>
        /// <param name="mat">目标材质</param>
        /// <returns>图层数组</returns>
        public static T4MTerrainLayer[] ReadLayersFromMaterial(Material mat)
        {
            if (mat == null) return new T4MTerrainLayer[0];

            T4MTerrainLayer[] layers = new T4MTerrainLayer[6];
            for (int i = 0; i < 6; i++)
            {
                layers[i] = T4MTerrainLayer.FromMaterial(mat, i);
            }

            return layers;
        }

        /// <summary>
        /// 切换 UV4 模式
        /// </summary>
        /// <param name="mat">目标材质</param>
        /// <param name="useUV4">是否使用 UV4</param>
        public static void SyncUV4Toggle(Material mat, bool useUV4)
        {
            if (mat == null) return;

            if (useUV4)
            {
                mat.EnableKeyword(UV4_KEYWORD);
            }
            else
            {
                mat.DisableKeyword(UV4_KEYWORD);
            }

            EditorUtility.SetDirty(mat);
        }

        /// <summary>
        /// 检查材质是否使用 UV4
        /// </summary>
        /// <param name="mat">目标材质</param>
        /// <returns>是否使用 UV4</returns>
        public static bool IsUsingUV4(Material mat)
        {
            if (mat == null) return false;
            return mat.IsKeywordEnabled(UV4_KEYWORD);
        }

        /// <summary>
        /// 设置控制图到材质
        /// </summary>
        /// <param name="mat">目标材质</param>
        /// <param name="controlMap">控制图</param>
        /// <param name="isSecond">是否为第二张控制图</param>
        public static void SetControlMap(Material mat, Texture2D controlMap, bool isSecond = false)
        {
            if (mat == null) return;

            string propertyName = isSecond ? "_Control2" : "_Control";
            if (mat.HasProperty(propertyName))
            {
                mat.SetTexture(propertyName, controlMap);
                EditorUtility.SetDirty(mat);
            }
        }

        /// <summary>
        /// 获取控制图
        /// </summary>
        /// <param name="mat">目标材质</param>
        /// <param name="isSecond">是否为第二张控制图</param>
        /// <returns>控制图纹理</returns>
        public static Texture2D GetControlMap(Material mat, bool isSecond = false)
        {
            if (mat == null) return null;

            string propertyName = isSecond ? "_Control2" : "_Control";
            if (mat.HasProperty(propertyName))
            {
                return mat.GetTexture(propertyName) as Texture2D;
            }

            return null;
        }

        /// <summary>
        /// 创建 T4M 材质
        /// </summary>
        /// <param name="path">保存路径</param>
        /// <param name="shaderName">Shader 名称</param>
        /// <returns>创建的材质</returns>
        public static Material CreateT4MMaterial(string path, string shaderName = null)
        {
            if (string.IsNullOrEmpty(shaderName))
            {
                shaderName = DefaultShaderName;
            }

            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"[T4MMaterialService] 找不到 Shader: {shaderName}");
                return null;
            }

            Material mat = new Material(shader);

            string directory = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();

            return mat;
        }

        /// <summary>
        /// 设置图层纹理
        /// </summary>
        /// <param name="mat">目标材质</param>
        /// <param name="layerIndex">图层索引</param>
        /// <param name="albedo">颜色贴图</param>
        /// <param name="normal">法线贴图</param>
        /// <param name="tile">UV 平铺</param>
        public static void SetLayerTextures(Material mat, int layerIndex, Texture albedo, Texture normal = null, Vector2? tile = null)
        {
            if (mat == null) return;

            T4MTerrainLayer layer = new T4MTerrainLayer(layerIndex)
            {
                Albedo = albedo,
                Normal = normal,
                Tile = tile ?? Vector2.one
            };

            layer.ApplyToMaterial(mat);
            EditorUtility.SetDirty(mat);
        }

        /// <summary>
        /// 获取材质使用的有效图层数量
        /// </summary>
        /// <param name="mat">目标材质</param>
        /// <returns>有效图层数量</returns>
        public static int GetValidLayerCount(Material mat)
        {
            if (mat == null) return 0;

            int count = 0;
            for (int i = 0; i < 6; i++)
            {
                var layer = T4MTerrainLayer.FromMaterial(mat, i);
                if (layer.IsValid)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 复制材质属性
        /// </summary>
        /// <param name="source">源材质</param>
        /// <param name="target">目标材质</param>
        public static void CopyMaterialProperties(Material source, Material target)
        {
            if (source == null || target == null) return;

            // 复制图层
            var layers = ReadLayersFromMaterial(source);
            ApplyLayers(target, layers);

            // 复制控制图
            var control1 = GetControlMap(source, false);
            var control2 = GetControlMap(source, true);

            if (control1 != null)
                SetControlMap(target, control1, false);
            if (control2 != null)
                SetControlMap(target, control2, true);

            // 复制 UV4 设置
            SyncUV4Toggle(target, IsUsingUV4(source));
        }
    }
}
