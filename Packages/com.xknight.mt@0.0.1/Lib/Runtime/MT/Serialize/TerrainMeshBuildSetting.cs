// Created By: WangYu  Date: 2023-11-23

using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.Serialize
{
    [CreateAssetMenu(fileName = "TerrainExportLODSetting", menuName = "MT/地形导出的 LOD 设置")]
    public class TerrainMeshBuildSetting : ScriptableObject
    {
        public int quadTreeDepth = 2;
        
        public LODSetting[] lodSettings;

        public int dataPack = 1;

        public bool genUV2;
    }
}