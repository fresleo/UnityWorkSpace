using System.Collections.Generic;
using UnityEngine;

namespace AirSticker.Runtime.Core
{
    /// <summary>
    /// 这个类运行宽相位凸多边形检测
    /// </summary>
    internal static class BroadPhaseConvexPolygonsDetection
    {
        /// <summary>
        /// 执行宽相位
        /// </summary>
        /// <remarks>
        /// 移除围绕贴花 box 圆圈外的多边形。<br/>
        /// 此外，删除网格方向与贴花 box 相反的多边形。<br/>
        /// </remarks>
        public static bool Execute(
            Vector3 centerPosInDecalBox,
            Vector3 decalSpaceNormalWs,
            float decalBoxWidth, float decalBoxHeight, float decalBoxDepth,
            List<ConvexPolygonInfo> convexPolygonInfos,
            bool projectionBackside,
            out List<ConvexPolygonInfo> broadPhaseConvexPolygonInfos)
        {
            float threshold = Mathf.Max(decalBoxWidth / 2, decalBoxHeight / 2, decalBoxDepth);
            threshold *= 1.414f;
            threshold *= threshold;

            int broadPhaseConvexPolygonCount = 0;
            for (int i = 0; i < convexPolygonInfos.Count; i++)
            {
                var convexPolygonInfo = convexPolygonInfos[i];
                if (!projectionBackside && Vector3.Dot(decalSpaceNormalWs, convexPolygonInfo.ConvexPolygon.FaceNormal) < 0)
                {
                    convexPolygonInfo.IsOutsideClipSpace = true; // 在 Clip 空间外
                    continue;
                }

                int vertNo_0 = convexPolygonInfo.ConvexPolygon.GetRealVertexNo(0);
                var v0 = convexPolygonInfo.ConvexPolygon.GetVertexPositionInWorldSpace(vertNo_0);
                v0 -= centerPosInDecalBox;
                if (v0.sqrMagnitude > threshold)
                {
                    int vertNo_1 = convexPolygonInfo.ConvexPolygon.GetRealVertexNo(1);
                    var v1 = convexPolygonInfo.ConvexPolygon.GetVertexPositionInWorldSpace(vertNo_1);
                    v1 -= centerPosInDecalBox;
                    if (v1.sqrMagnitude > threshold)
                    {
                        int vertNo_2 = convexPolygonInfo.ConvexPolygon.GetRealVertexNo(2);
                        var v2 = convexPolygonInfo.ConvexPolygon.GetVertexPositionInWorldSpace(vertNo_2);
                        v2 -= centerPosInDecalBox;
                        if (v2.sqrMagnitude > threshold)
                        {
                            convexPolygonInfo.IsOutsideClipSpace = true; // 在 Clip 空间外
                            continue;
                        }
                    }
                }

                broadPhaseConvexPolygonCount++;
            }

            broadPhaseConvexPolygonInfos = new List<ConvexPolygonInfo>(broadPhaseConvexPolygonCount);
            
            var positionBuffer = new Vector3[ConvexPolygon.DefaultMaxVertex * broadPhaseConvexPolygonCount];
            var normalBuffer = new Vector3[ConvexPolygon.DefaultMaxVertex * broadPhaseConvexPolygonCount];
            var localPositionBuffer = new Vector3[ConvexPolygon.DefaultMaxVertex * broadPhaseConvexPolygonCount];
            var localNormalBuffer = new Vector3[ConvexPolygon.DefaultMaxVertex * broadPhaseConvexPolygonCount];
            var boneWeightBuffer = new BoneWeight[ConvexPolygon.DefaultMaxVertex * broadPhaseConvexPolygonCount];
            var lineBuffer = new Line[ConvexPolygon.DefaultMaxVertex * broadPhaseConvexPolygonCount];
            
            int startOffsetInBuffer = 0;

            for (int i = 0; i < convexPolygonInfos.Count; i++)
            {
                var convexPolygonInfo = convexPolygonInfos[i];
                
                if (!convexPolygonInfo.IsOutsideClipSpace)
                {
                    broadPhaseConvexPolygonInfos.Add(new ConvexPolygonInfo
                    {
                        ConvexPolygon = new ConvexPolygon(
                            convexPolygonInfo.ConvexPolygon, 
                            positionBuffer, normalBuffer, boneWeightBuffer, lineBuffer, localPositionBuffer, localNormalBuffer,
                            startOffsetInBuffer),
                        IsOutsideClipSpace = convexPolygonInfo.IsOutsideClipSpace
                    });

                    startOffsetInBuffer += ConvexPolygon.DefaultMaxVertex;
                }

                convexPolygonInfo.IsOutsideClipSpace = false;
            }

            return broadPhaseConvexPolygonCount > 0;
        }
        
    }
}