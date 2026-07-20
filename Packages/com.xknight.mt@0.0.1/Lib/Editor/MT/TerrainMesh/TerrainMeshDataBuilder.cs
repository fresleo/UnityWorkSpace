// Created By: WangYu  Date: 2023-12-21

using com.xknight.mt.Lib.Runtime.MT.Serialize;
using com.xknight.mt.Lib.Runtime.MT.TerrainMesh;
using UnityEditor;
using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.TerrainMesh
{
    public static class TerrainMeshDataBuilder
    {
        public static void HandleTerrainMeshDataGenerators(TerrainMeshDataGenerator[] array)
        {
            if (array == null || array.Length == 0)
            {
                return;
            }
            
            var tmbs = FindTerrainMeshBuildSetting();
            if (tmbs == null)
            {
                return;
            }
            
            foreach (var item in array)
            {
                AutoSetTerrainMeshBuildSetting(item, tmbs);
            }
        }
        
        private static TerrainMeshBuildSetting FindTerrainMeshBuildSetting()
        {
            TerrainMeshBuildSetting setting = null;

            var guids = AssetDatabase.FindAssets($"t:{nameof(TerrainMeshBuildSetting)}");
            if (guids.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                setting = AssetDatabase.LoadAssetAtPath<TerrainMeshBuildSetting>(assetPath);
            }

            return setting;
        }

        private static void AutoSetTerrainMeshBuildSetting(MonoBehaviour script, TerrainMeshBuildSetting setting)
        {
            if (script is TerrainMeshDataGenerator tmdg)
            {
                tmdg.setting = setting;
            }
            
            /*
            if (script is TerrainMeshGenerator tmg)
            {
                tmg.setting = setting;
            }
            */
        }
        
    }
}