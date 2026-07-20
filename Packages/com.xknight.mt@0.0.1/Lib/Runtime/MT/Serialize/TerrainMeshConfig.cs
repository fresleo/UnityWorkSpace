
using System;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.Serialize
{
    /// <summary>
    /// 序列化数据
    /// </summary>
    public class TerrainMeshConfig : ScriptableObject
    {
        // 材质 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        /// <summary>
        /// 混合模式的材质 - 就是地形原始的材质
        /// </summary>
        public string[] mixMatPaths;
        
        /// <summary>
        /// VT - 烘焙漫反射纹理的材质
        /// </summary>
        public string[] bakeVTDiffuseMatPaths;
        
        /// <summary>
        /// VT - 烘焙法线纹理的材质
        /// </summary>
        public string[] bakeVTNormalMatPaths;
        
        /// <summary>
        /// VT - 渲染烘焙后纹理的材质
        /// </summary>
        public string bakedVTMatPath;
        
        // 网格 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        /// <summary>
        /// mesh 数据包容量
        /// </summary>
        public int meshDataPack;
        
        /// <summary>
        /// mesh 的前缀
        /// </summary>
        public string meshPrefix;
        
        // 树 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        /// <summary>
        /// 4叉树数据
        /// </summary>
        public string treeDataPath;
        
        // 高度图 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        /// <summary>
        /// 高度图数据
        /// </summary>
        public string heightMapPath;
        
        /// <summary>
        /// 高度图的世界空间Y值
        /// </summary>
        public float heightmapWorldY;
        
        /// <summary>
        /// 高度图缩放
        /// </summary>
        public Vector3 heightmapScale;
        
        /// <summary>
        /// 高度图分辨率
        /// </summary>
        public int heightmapResolution;
        
        // 细节 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        /*
        /// <summary>
        /// 细节层的原型数据
        /// </summary>
        public DetailLayerData[] detailPrototypes;
        
        /// <summary>
        /// 细节宽
        /// </summary>
        public int detailWidth;
        
        /// <summary>
        /// 细节高
        /// </summary>
        public int detailHeight;
        
        /// <summary>
        /// 每个补丁的细节分辨率
        /// </summary>
        public int detailResolutionPerPatch;
        
        /// <summary>
        /// 细节层数据
        /// </summary>
        public TextAsset detailLayers;
        */

        // Lightmap >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        /// <summary>
        /// Lightmap 配置数据
        /// </summary>
        public LightmapConfig lightmapData;
    }
    
    /// <summary>
    /// 细节的层数据
    /// </summary>
    [Serializable]
    public class DetailLayerData
    {
        /// <summary>
        /// 原型预设
        /// </summary>
        public GameObject prototype;
        
        /// <summary>
        /// 最小宽度
        /// </summary>
        public float minWidth;
        /// <summary>
        /// 最大宽度
        /// </summary>
        public float maxWidth;
        /// <summary>
        /// 最小高度
        /// </summary>
        public float minHeight;
        /// <summary>
        /// 最大高度
        /// </summary>
        public float maxHeight;
        
        /// <summary>
        /// 噪声传播
        /// </summary>
        public float noiseSpread;
        
        /// <summary>
        /// 健康的颜色
        /// </summary>
        public Color healthyColor;
        /// <summary>
        /// 脏了的颜色
        /// </summary>
        public Color dryColor;
        
        /// <summary>
        /// 最大密度
        /// </summary>
        public int maxDensity;
        
        /// <summary>
        /// 水面浮动
        /// 非bake数据，手动开关。\n它决定了该层的细节是否会浮动在水面上
        /// </summary>
        public bool waterFloating;
    }
}