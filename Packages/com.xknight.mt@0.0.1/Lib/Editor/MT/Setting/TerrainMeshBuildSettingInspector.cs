// Created By: WangYu  Date: 2023-11-23

using System;
using com.xknight.mt.Lib.Editor.MT.Utils;
using com.xknight.mt.Lib.Runtime.MT.Serialize;
using com.xknight.mt.Lib.Runtime.MT.Utils;
using UnityEditor;
using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.Setting
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TerrainMeshBuildSetting))]
    public class TerrainMeshBuildSettingInspector : AbsInspector<TerrainMeshBuildSetting>
    {
        protected override void DrawAutoApplyGUI(TerrainMeshBuildSetting script)
        {
            EditorGUILayout.LabelField("地形网格构建设置");
            EditorGUILayout.Space(5);
            
            GUI_QuadTreeDepth(script);
            EditorGUILayout.Space(5);
            
            GUI_LOD(script);
            EditorGUILayout.Space(5);
            
            script.dataPack = EditorGUILayout.IntField("每个文件存多少块 mesh", script.dataPack);
            EditorGUILayout.Space(5);
            
            script.genUV2 = EditorGUILayout.ToggleLeft("生成 UV2", script.genUV2);
            EditorGUILayout.Space(5);
        }
        

        private void GUI_QuadTreeDepth(TerrainMeshBuildSetting script)
        {
            int curSliceCount = Mathf.FloorToInt(Mathf.Pow(2, script.quadTreeDepth));
            int sliceCount = EditorGUILayout.IntField("切割片数 (NxN)", curSliceCount);
            if (sliceCount != curSliceCount)
            {
                curSliceCount = Mathf.NextPowerOfTwo(sliceCount);
                script.quadTreeDepth = Mathf.FloorToInt(Mathf.Log(curSliceCount, 2));
            }
            
            EditorGUILayout.LabelField($"4叉树深度： {script.quadTreeDepth}");
        }
        
        private void GUI_LOD(TerrainMeshBuildSetting script)
        {
            if (script.lodSettings == null)
            {
                script.lodSettings = Array.Empty<LODSetting>();
            }
            
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUIContent label = new GUIContent("LOD级数");
                int len = EditorGUILayout.IntField(label, script.lodSettings.Length);
                
                script.lodSettings = MTRuntimeUtils.ExpandedArray(script.lodSettings, len, (item) =>
                {
                    item.subdivision = 4;
                    return item;
                });
                
                for (int i = 0; i < len; i++)
                {
                    var lod = script.lodSettings[i];
                    if(lod == null) continue;

                    EditorGUI.indentLevel++;
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        EditorGUILayout.Space();
                        
                        label = new GUIContent("细分级别 (NxN)", "细分的越多，地形越精细，但是也会增加顶点数量");
                        int oldS = Mathf.FloorToInt(Mathf.Pow(2, lod.subdivision));
                        int curS = EditorGUILayout.IntField(label, oldS);
                        EditorGUILayout.Space(5);
                        if (oldS != curS)
                        {
                            oldS = Mathf.NextPowerOfTwo(curS);
                            float log = Mathf.Log(oldS, 2);
                            lod.subdivision = Mathf.FloorToInt(log);
                        }

                        label = new GUIContent("坡度角误差", "要求的误差越小，地形越精细，但是也会增加顶点数量");
                        var curSae = EditorGUILayout.FloatField(label, lod.slopeAngleError);
                        lod.slopeAngleError = Mathf.Max(0.01f, curSae);
                        
                        EditorGUILayout.Space();
                    }
                    EditorGUI.indentLevel--;
                }
            }
        }
        
    }
}