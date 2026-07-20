// Created By: WangYu  Date: 2024-05-31

using System.Collections.Generic;
using com.xknight.mt.Lib.Runtime.MT.OcclusionCulling;
using com.xknight.mt.Lib.Runtime.MT.UnityComponent;
using com.xknight.mt.Lib.Runtime.MT.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace com.xknight.mt.Lib.Editor.MT.OcclusionCulling
{
    /// <summary>
    /// 空间数据标记构建器
    /// </summary>
    public static class SpatialDataTagBuilder
    {
        /// <summary>
        /// 处理生成器
        /// </summary>
        /// <param name="generator">这个生成器，在场经理应该只有1个</param>
        public static void HandleSpatialDataTagGenerator(SpatialDataTagGenerator generator)
        {
            var scriptList = new List<SDTRendererId>();
            ScanScripts(generator, ref scriptList);
            SetupQuadTree(generator, scriptList);
        }

        private static void ScanScripts(SpatialDataTagGenerator generator, ref List<SDTRendererId> scriptList)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            var rootGos = activeScene.GetRootGameObjects();
            foreach (var rootGo in rootGos)
            {
                var scripts = rootGo.GetComponentsInChildren<SDTRendererId>();
                scriptList.AddRange(scripts);
            }
            
            // 计算完整的包围盒
            generator.bnd = new Bounds();
            foreach (var script in scriptList)
            {
                generator.bnd.Encapsulate(script.WholeBounds);
            }
        }

        private static void SetupQuadTree(SpatialDataTagGenerator generator, List<SDTRendererId> scriptList)
        {
            //创建1棵树
            generator.quadTreeRoot = new SDTQuadTreeBuildNode();
            generator.quadTreeRoot.BuildSimpleQuadTreeNode(generator.bnd, generator.treeDepth);
            
            //往叶子上+数据
            for (int i = 0; i < scriptList.Count; i++)
            {
                var script = scriptList[i];
                FindLeafAddData(generator.quadTreeRoot, script);
            }

            //清理空树干
            generator.quadTreeRoot.ClearEmpty();
        }

        private static void FindLeafAddData(SDTQuadTreeBuildNode buildNode, SDTRendererId script)
        {
            Transform objTf = script.transform;
            Matrix4x4 objM4 = Matrix4x4.TRS(objTf.position, objTf.rotation, Vector3.one);
            
            if (buildNode.bnd.Contains(objTf.position))
            {
                //在叶子节点上记数据
                if (buildNode.childrenNodes.Count == 0)
                {
                    buildNode.holdIds.Add(script.id);
                    buildNode.holdBounds.Add(script.WholeBounds);
                    buildNode.holdWorldMatrixs.Add(objM4);
                }
                //树干的话，继续往后找
                else
                {
                    foreach (var child in buildNode.childrenNodes)
                    {
                        FindLeafAddData(child, script);
                    }
                }
            }
        }
    }
}