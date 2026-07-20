// Created By: WangYu  Date: 2023-12-21

using com.xknight.mt.Lib.Editor.MT.Utils;
using com.xknight.mt.Lib.Runtime.MT.InstancedObject;
using com.xknight.mt.Lib.Runtime.MT.Utils;
using UnityEditor;
using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.InstancedObject
{
    public static class InstancedObjectDataBuilder
    {
        public static void HandleInstancedObjectMarks(InstancedObjectMarker[] array)
        {
            bool result = true;
            foreach (var item in array)
            {
                result &= item.RefreshRelation();
            }

            if (!result)
            {
                EditorUtility.DisplayDialog("失败", "无法在父中找到 InstancedObjectVolume 组件", "确定");
            }
        }

        public static void HandleInstancedObjectVolumes(InstancedObjectDataGenerator[] array)
        {
            foreach (var generator in array)
            {
                ScanChildrenMark(generator);
                SetupQuadTree(generator);
            }
        }
        
        private static void ScanChildrenMark(InstancedObjectDataGenerator generator)
        {
            generator.UniqueName();
            
            generator.cullCamera = Camera.main;
            
            //获取子对象上所有的标记器
            generator.childrenMarkers.Clear();
            var markers = generator.transform.GetComponentsInChildren<InstancedObjectMarker>();
            foreach (var item in markers)
            {
                generator.childrenMarkers.Add(item);
            }
            
            //生成器的体积包围盒
            if (generator.childrenMarkers.Count > 0)
            {
                var firstMarker = generator.childrenMarkers[0];
                
                generator.bnd = new Bounds(firstMarker.transform.position, Vector3.zero);
                generator.bnd.Encapsulate(firstMarker.triggerBnd);
                
                for (int i = 1; i < generator.childrenMarkers.Count; i++)
                {
                    var itemMarker = generator.childrenMarkers[i];
                    generator.bnd.Encapsulate(itemMarker.triggerBnd);
                }
                
                generator.bnd.Expand(generator.expand);
            }
        }
        
        private static void SetupQuadTree(InstancedObjectDataGenerator generator)
        {
            //创建1棵树
            generator.quadTreeRoot = new IOQuadTreeBuildNode();
            generator.quadTreeRoot.BuildSimpleQuadTreeNode(generator.bnd, generator.treeDepth);
            
            //往叶子上+数据
            for (int i = 0; i < generator.childrenMarkers.Count; i++)
            {
                var marker = generator.childrenMarkers[i];
                FindLeafAddData(generator.quadTreeRoot, marker);
            }

            //清理空树干
            generator.quadTreeRoot.ClearEmpty();
        }

        // 找到树叶节点，并添加数据
        private static void FindLeafAddData(IOQuadTreeBuildNode buildNode, InstancedObjectMarker marker)
        {
            Transform tf = marker.targetGo.transform;
            Matrix4x4 matr = Matrix4x4.TRS(tf.position, tf.rotation, tf.lossyScale);
            
            if (buildNode.bnd.Contains(matr.GetPosition()))
            {
                //在叶子节点上记数据
                if (buildNode.childrenNodes.Count == 0)
                {
                    buildNode.holdWorldMatrixs.Add(matr);
                    
                    buildNode.holdBounds.Add(marker.triggerBnd);
                    
                    var ld = MTEditorUtils.GetLightmapData(marker.targetGo);
                    buildNode.holdLightmapDatas.Add(ld);
                }
                //树干的话，继续往后找
                else
                {
                    foreach (var child in buildNode.childrenNodes)
                    {
                        FindLeafAddData(child, marker);
                    }
                }
            }
        }
        
    }
}