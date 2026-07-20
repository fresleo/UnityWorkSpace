// Created By: WangYu  Date: 2023-11-18

using com.xknight.mt.Lib.Editor.MT.InstancedObject;
using com.xknight.mt.Lib.Editor.MT.OcclusionCulling;
using com.xknight.mt.Lib.Editor.MT.StaticObject;
using com.xknight.mt.Lib.Editor.MT.TerrainMesh;
using com.xknight.mt.Lib.Runtime.MT.InstancedObject;
using com.xknight.mt.Lib.Runtime.MT.OcclusionCulling;
using com.xknight.mt.Lib.Runtime.MT.StaticObject;
using com.xknight.mt.Lib.Runtime.MT.TerrainMesh;
using UnityEditor;
using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT
{
    /// <summary>
    /// MT 的 EditorApplication 绑定
    /// </summary>
    [InitializeOnLoad]
    public class MTEditorApplication
    {
        static MTEditorApplication()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            Lightmapping.bakeCompleted += OnBakeCompleted;
        }
        
        private static void OnHierarchyChanged()
        {
            TriggerVolumes();
        }
        
        private static void OnBakeCompleted()
        {
            TriggerVolumes();
        }

        /// <summary>
        /// 触发体积脚本
        /// </summary>
        public static void TriggerVolumes()
        {
            //确保只在编辑器不运行时执行
            if (Application.isPlaying)
            {
                return;
            }
            
            var tmdgs = UnityEngine.Object.FindObjectsOfType<TerrainMeshDataGenerator>(false);
            TerrainMeshDataBuilder.HandleTerrainMeshDataGenerators(tmdgs);
            
            var sodgs = UnityEngine.Object.FindObjectsOfType<StaticObjectDataGenerator>(false);
            StaticObjectDataBuilder.HandleStaticObjectVolumes(sodgs);
            
            var ioms = UnityEngine.Object.FindObjectsOfType<InstancedObjectMarker>(false);
            InstancedObjectDataBuilder.HandleInstancedObjectMarks(ioms);
            
            var iodgs = UnityEngine.Object.FindObjectsOfType<InstancedObjectDataGenerator>(false);
            InstancedObjectDataBuilder.HandleInstancedObjectVolumes(iodgs);
            
            var sdtgs = UnityEngine.Object.FindObjectsOfType<SpatialDataTagGenerator>(false);
            if (sdtgs.Length > 0)
            {
                SpatialDataTagBuilder.HandleSpatialDataTagGenerator(sdtgs[0]);
            }
        }
        
    }
}