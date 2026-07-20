// Created By: WangYu  Date: 2023-11-23

using com.xknight.mt.Lib.Runtime.MT.Common;
using com.xknight.mt.Lib.Runtime.MT.Serialize;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.TerrainMesh
{
    [ExecuteInEditMode]
    public class TerrainMeshDataGenerator : AbsGenerator<TerrainMeshDataGenerator>
    {
        /// <summary>
        /// 统一的构建设置
        /// </summary>
        public TerrainMeshBuildSetting setting;
        
        /// <summary>
        /// 自动创建加载器
        /// </summary>
        public bool autoCreateLoader;

        protected override void OnEnable()
        {
            UniqueName();
        }
    }
}