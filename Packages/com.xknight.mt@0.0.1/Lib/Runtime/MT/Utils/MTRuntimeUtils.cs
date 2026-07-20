// Created By: WangYu  Date: 2022-10-15

using System;
using System.Runtime.CompilerServices;
using com.xknight.mt.Lib.Runtime.MT.Common;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.Utils
{
    public static class MTRuntimeUtils
    {
        /// <summary>
        /// 扩容数组
        /// </summary>
        public static T[] ExpandedArray<T>(T[] array, int count, Func<T, T> setter = null)
            where T : new()
        {
            if (array == null)
            {
                array = Array.Empty<T>();
            }

            if (array.Length == count)
            {
                return array;
            }

            //备份旧数组
            T[] oldArray = array;

            //创建新数组
            array = new T[count];
            for (int i = 0; i < Mathf.Min(oldArray.Length, count); i++)
            {
                array[i] = oldArray[i];
            }

            //有新增
            for (int i = oldArray.Length; i < count; i++)
            {
                array[i] = new T();
                if (setter != null)
                {
                    array[i] = setter(array[i]);
                }
            }

            return array;
        }

        /// <summary>
        /// 包围盒占视野内的像素尺寸
        /// </summary>
        /// <param name="viewerPos">观察者的位置</param>
        /// <param name="fov">fov</param>
        /// <param name="screenHeight">屏幕的高</param>
        /// <param name="targetPos">包围盒的中心点</param>
        /// <param name="diameter">直径</param>
        public static float PixelSize(
            Vector3 viewerPos, float fov, float screenHeight,
            Vector3 targetPos, float diameter)
        {
            float distance = Vector3.Distance(viewerPos, targetPos);
            float pixelSize = (diameter * Mathf.Rad2Deg * screenHeight) / (distance * fov);
            return pixelSize;
        }

        /// <summary>
        /// 屏幕覆盖率
        /// </summary>
        /// <param name="cullCamera">剔除摄像机</param>
        /// <param name="bnd">对象包围盒</param>
        public static float ScreenCoverRate(Camera cullCamera, Bounds bnd)
        {
            if (cullCamera == null) return 0;
            
            float pixelSize = PixelSize(
                cullCamera.transform.position, cullCamera.fieldOfView, Screen.height, 
                bnd.center, bnd.size.magnitude);
            
            float rate = pixelSize / Screen.width;
            
            return rate;
        }

        /// <summary>
        /// 左边 - 右边的一半
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float MinusHalf(float left, float right)
        {
            return left - right * 0.5f;
        }

        /// <summary>
        /// 左边 + 右边的一半
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AddHalf(float left, float right)
        {
            return left + right * 0.5f;
        }
        
        /// <summary>
        /// 获取 GameObject 下所有渲染器的整个包围盒
        /// </summary>
        public static Bounds GetWholeBounds(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return default;
            }
            
            var renderers = gameObject.GetComponentsInChildren<Renderer>();

            Bounds bnd = GetWholeBounds(renderers);
            return bnd;
        }

        /// <summary>
        /// 获取渲染器数组的整个包围盒
        /// </summary>
        public static Bounds GetWholeBounds(Renderer[] renderers)
        {
            if (renderers == null || renderers.Length == 0)
            {
                return default;
            }
            
            // 因为我们需要的是渲染器的包围盒，所以中心点也是以第1个渲染器的位置为准
            var firstMr = renderers[0];
            Vector3 center = firstMr.transform.position;

            // 先确定初始的定位点，否则直接调用 Encapsulate 的话，会以世界原点为基准
            Bounds bnd = new Bounds(center, Vector3.zero);
            // 封装包围盒
            foreach (var renderer in renderers)
            {
                bnd.Encapsulate(renderer.bounds);
            }
            
            return bnd;
        }

        public static void CullQuadTreeNode<TNode>(
            Plane[] cullPlanes, TNode[] treeNodes, 
            MTArray<TNode> candidateArray, MTArray<TNode> visibleArray, 
            Func<TNode, bool> checkVisibility) 
            where TNode : AbsQuadTreeNode
        {
            visibleArray.Reset();
            candidateArray.Reset();
            candidateArray.Add(treeNodes[0]);
            
            int next_start_idx = 0;
            
            //loop 的作用是限制最大遍历次数
            for (int loop = 0; loop < treeNodes.Length; loop++)
            {
                int cidx = next_start_idx;
                next_start_idx = candidateArray.Length;
                
                for (; cidx < next_start_idx; cidx++)
                {
                    var node = candidateArray[cidx];
                    if(node == null) continue;

                    //检查候选节点的可见性
                    bool visibility = false;
                    if (checkVisibility != null)
                    {
                        visibility = checkVisibility(node);
                    }

                    if (visibility)
                    {
                        visibleArray.Add(node);
                    }
                    //尝试把它的子节点加入候选名单
                    else
                    {
                        foreach (int cid in node.children)
                        {
                            var childNode = treeNodes[cid];
                            if (GeometryUtility.TestPlanesAABB(cullPlanes, childNode.bnd))
                            {
                                candidateArray.Add(childNode);
                            }
                        }
                    }
                }
                
                //当候选名单没有增加时，说明已经没有继续往后遍历的必要了
                if (candidateArray.Length == next_start_idx)
                {
                    break;
                }
            }
        }

        public static void DestroyObject(UnityEngine.Object obj)
        {
            if(obj == null) return;
            
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(obj);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }
        
    }
}