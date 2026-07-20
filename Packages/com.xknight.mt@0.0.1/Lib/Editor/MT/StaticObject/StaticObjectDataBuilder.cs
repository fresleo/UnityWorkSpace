// Created By: WangYu  Date: 2023-12-21

using com.xknight.mt.Lib.Editor.MT.Utils;
using com.xknight.mt.Lib.Runtime.MT.StaticObject;
using com.xknight.mt.Lib.Runtime.MT.Utils;
using UnityEditor;
using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.StaticObject
{
    public static class StaticObjectDataBuilder
    {
        public static void HandleStaticObjectVolumes(StaticObjectDataGenerator[] array)
        {
            foreach (var generator in array)
            {
                ScanChildren(generator);
                SetupQuadTree(generator);
            }
        }

        private static void ScanChildren(StaticObjectDataGenerator generator)
        {
            generator.UniqueName();
            
            //设置包围盒
            generator.bnd = MTRuntimeUtils.GetWholeBounds(generator.gameObject);
            generator.bnd.Expand(generator.expand);
            
            generator.childrenGos.Clear();
            generator.prototypes.Clear();
            ScanPrefabs(generator, generator.gameObject);
        }
        
        private static void ScanPrefabs(StaticObjectDataGenerator generator, GameObject rootGo)
        {
            Transform rootTf = rootGo.transform;
            for (int i = 0; i < rootTf.childCount; i++)
            {
                var child = rootTf.GetChild(i);
                var childGo = child.gameObject;
                
                if (MTEditorUtils.IsPrefab(childGo))
                {
                    generator.childrenGos.Add(childGo);

                    var prefabGo = PrefabUtility.GetCorrespondingObjectFromOriginalSource(childGo);
                    if (!generator.prototypes.Contains(prefabGo))
                    {
                        generator.prototypes.Add(prefabGo);
                    }
                }
                else
                {
                    ScanPrefabs(generator, childGo);
                }
            }
        }
        
        private static void SetupQuadTree(StaticObjectDataGenerator generator)
        {
            //创建1棵树
            generator.quadTreeRoot = new SOQuadTreeBuildNode();
            generator.quadTreeRoot.BuildSimpleQuadTreeNode(generator.bnd, generator.treeDepth);
            
            //往叶子上+数据
            for (int i = 0; i < generator.childrenGos.Count; i++)
            {
                var childGo = generator.childrenGos[i];

                var prefabGo = PrefabUtility.GetCorrespondingObjectFromOriginalSource(childGo);
                int assetIdx = generator.prototypes.IndexOf(prefabGo);
                
                FindLeafAddData(generator.quadTreeRoot, assetIdx, childGo);
            }

            //清理空树干
            generator.quadTreeRoot.ClearEmpty();
        }
        
        private static void FindLeafAddData(SOQuadTreeBuildNode buildNode, int assetIdx, GameObject go)
        {
            int gid = go.GetInstanceID();
            
            Transform tf = go.transform;
            Matrix4x4 matr = Matrix4x4.TRS(tf.position, tf.rotation, tf.lossyScale);
            
            if (buildNode.bnd.Contains(matr.GetPosition()))
            {
                //在叶子节点上记数据
                if (buildNode.childrenNodes.Count == 0)
                {
                    buildNode.holdGids.Add(gid);
                    buildNode.holdAssetIdxs.Add(assetIdx);
                    buildNode.holdWorldMatrixs.Add(matr);

                    var lds = MTEditorUtils.GetLightmapDatas(go);
                    buildNode.holdLightmapDatas.Add(gid, lds);
                }
                //树干的话，继续往后找
                else
                {
                    foreach (var child in buildNode.childrenNodes)
                    {
                        FindLeafAddData(child, assetIdx, go);
                    }
                }
            }
        }
        
    }
}