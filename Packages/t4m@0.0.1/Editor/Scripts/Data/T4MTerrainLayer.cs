/********************************************************
 * File:    T4MTerrainLayer.cs
 * Description: T4M 地形图层数据模型
 *********************************************************/

using System;
using UnityEngine;

namespace T4MEditor.Data
{
    /// <summary>
    /// 地形图层数据类，封装单个图层的所有纹理和参数
    /// 替代 T4MSC 中散乱的 Layer1~6、BumpX、MaskX 字段
    /// </summary>
    [Serializable]
    public class T4MTerrainLayer
    {
        /// <summary>
        /// 图层索引 (0-5)
        /// </summary>
        public int Index;

        /// <summary>
        /// 基础颜色贴图 (Albedo)
        /// </summary>
        public Texture Albedo;

        /// <summary>
        /// 法线贴图
        /// </summary>
        public Texture Normal;

        /// <summary>
        /// 遮罩贴图
        /// </summary>
        public Texture Mask;

        /// <summary>
        /// UV 平铺
        /// </summary>
        public Vector2 Tile = Vector2.one;

        /// <summary>
        /// UV 偏移
        /// </summary>
        public Vector2 Offset = Vector2.zero;

        /// <summary>
        /// 光泽度
        /// </summary>
        public float Shininess;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public T4MTerrainLayer() { }

        /// <summary>
        /// 带索引的构造函数
        /// </summary>
        public T4MTerrainLayer(int index)
        {
            Index = index;
        }

        /// <summary>
        /// 从材质中读取指定索引的图层数据
        /// </summary>
        /// <param name="mat">目标材质</param>
        /// <param name="index">图层索引 (0-5)</param>
        /// <returns>图层数据</returns>
        public static T4MTerrainLayer FromMaterial(Material mat, int index)
        {
            if (mat == null) return new T4MTerrainLayer(index);

            var layer = new T4MTerrainLayer(index);

            // 根据索引获取对应的属性名
            string albedoName = GetAlbedoPropertyName(index);
            string normalName = GetNormalPropertyName(index);
            string maskName = GetMaskPropertyName(index);
            string tileName = GetTilePropertyName(index);

            if (mat.HasProperty(albedoName))
                layer.Albedo = mat.GetTexture(albedoName);

            if (mat.HasProperty(normalName))
                layer.Normal = mat.GetTexture(normalName);

            if (mat.HasProperty(maskName))
                layer.Mask = mat.GetTexture(maskName);

            if (mat.HasProperty(tileName))
            {
                Vector4 tileOffset = mat.GetVector(tileName);
                layer.Tile = new Vector2(tileOffset.x, tileOffset.y);
                layer.Offset = new Vector2(tileOffset.z, tileOffset.w);
            }

            return layer;
        }

        /// <summary>
        /// 将图层数据应用到材质
        /// </summary>
        /// <param name="mat">目标材质</param>
        public void ApplyToMaterial(Material mat)
        {
            if (mat == null) return;

            string albedoName = GetAlbedoPropertyName(Index);
            string normalName = GetNormalPropertyName(Index);
            string maskName = GetMaskPropertyName(Index);
            string tileName = GetTilePropertyName(Index);

            if (mat.HasProperty(albedoName))
                mat.SetTexture(albedoName, Albedo);

            if (mat.HasProperty(normalName))
                mat.SetTexture(normalName, Normal);

            if (mat.HasProperty(maskName))
                mat.SetTexture(maskName, Mask);

            if (mat.HasProperty(tileName))
            {
                Vector4 tileOffset = new Vector4(Tile.x, Tile.y, Offset.x, Offset.y);
                mat.SetVector(tileName, tileOffset);
            }
        }

        /// <summary>
        /// 检查图层是否有效（至少有 Albedo 贴图）
        /// </summary>
        public bool IsValid => Albedo != null;

        #region Property Name Helpers

        /// <summary>
        /// 根据图层索引获取 Albedo 贴图的 Shader 属性名
        /// </summary>
        /// <param name="index">图层索引 (0-5)</param>
        /// <returns>Shader 属性名</returns>
        private static string GetAlbedoPropertyName(int index)
        {
            return index switch
            {
                0 => "_Splat0",
                1 => "_Splat1",
                2 => "_Splat2",
                3 => "_Splat3",
                4 => "_Splat4",
                5 => "_Splat5",
                _ => "_Splat0"
            };
        }

        /// <summary>
        /// 根据图层索引获取法线贴图的 Shader 属性名
        /// </summary>
        /// <param name="index">图层索引 (0-3)</param>
        /// <returns>Shader 属性名</returns>
        private static string GetNormalPropertyName(int index)
        {
            return index switch
            {
                0 => "_Normal0",
                1 => "_Normal1",
                2 => "_Normal2",
                3 => "_Normal3",
                _ => "_Normal0"
            };
        }

        /// <summary>
        /// 根据图层索引获取遮罩贴图的 Shader 属性名
        /// </summary>
        /// <param name="index">图层索引 (0-3)</param>
        /// <returns>Shader 属性名</returns>
        private static string GetMaskPropertyName(int index)
        {
            return index switch
            {
                0 => "_Mask0",
                1 => "_Mask1",
                2 => "_Mask2",
                3 => "_Mask3",
                _ => "_Mask0"
            };
        }

        /// <summary>
        /// 根据图层索引获取 UV Tile/Offset 的 Shader 属性名
        /// </summary>
        /// <param name="index">图层索引 (0-5)</param>
        /// <returns>Shader 属性名 (格式: _SplatX_ST)</returns>
        private static string GetTilePropertyName(int index)
        {
            return index switch
            {
                0 => "_Splat0_ST",
                1 => "_Splat1_ST",
                2 => "_Splat2_ST",
                3 => "_Splat3_ST",
                4 => "_Splat4_ST",
                5 => "_Splat5_ST",
                _ => "_Splat0_ST"
            };
        }

        #endregion
    }
}
