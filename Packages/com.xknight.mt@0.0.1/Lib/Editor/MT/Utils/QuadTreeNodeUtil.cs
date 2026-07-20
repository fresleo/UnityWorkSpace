// Created By: WangYu  Date: 2023-12-04

using System;
using System.Collections.Generic;
using System.IO;
using com.xknight.mt.Lib.Runtime.MT.Common;
using com.xknight.mt.Lib.Runtime.MT.Utils;

namespace com.xknight.mt.Lib.Editor.MT.Utils
{
    public static class QuadTreeNodeUtil
    {
        /// <summary>
        /// 把 build 节点导出成 runtime 节点
        /// </summary>
        public static void ExportTreeNodes<TBuildNode, TNode>(
            TBuildNode buildNode, TNode runtimeNode, List<TNode> nodeList, 
            Action<TBuildNode, TNode> dataSetter) 
            where TBuildNode : AbsQuadTreeBuildNode<TBuildNode>, new()
            where TNode : AbsQuadTreeNode, new()
        {
            if (buildNode == null)
            {
                return;
            }

            runtimeNode.bnd = buildNode.bnd;
            
            dataSetter?.Invoke(buildNode, runtimeNode);

            //有子节点
            int childCount = buildNode.childrenNodes.Count;
            if (childCount > 0)
            {
                runtimeNode.children = new int[childCount];

                //创建子节点，并分配 cid (就是序列号)
                for (int i = 0; i < childCount; i++)
                {
                    int cid = nodeList.Count;
                    var child = new TNode();
                    child.Initialize(cid);
                    
                    nodeList.Add(child);
                    
                    runtimeNode.children[i] = child.cellId;
                }

                //递归创建子节点
                for (int i = 0; i < childCount; i++)
                {
                    var childIdx = runtimeNode.children[i];
                    
                    ExportTreeNodes(buildNode.childrenNodes[i], nodeList[childIdx], nodeList, dataSetter);
                }
            }
        }

        /// <summary>
        /// 序列化树的节点列表
        /// </summary>
        public static void SerializeTrees<TNode>(MemoryStream stream, List<TNode> nodeList) 
            where TNode : AbsQuadTreeNode
        {
            int len = nodeList.Count;
            MTStreamUtils.WriteInt(stream, len);
            for (int i = 0; i < len; i++)
            {
                var node = nodeList[i];
                node.Serialize(stream);
            }
        }
        
    }
}