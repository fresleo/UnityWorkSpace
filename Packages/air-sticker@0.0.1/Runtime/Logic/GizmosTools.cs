// Created By: WangYu  Date: 2025-02-19

using UnityEngine;

namespace AirSticker.Runtime.Logic
{
    public static class GizmosTools
    {
        public static void DrawDecalBox(
            Transform transform, 
            Color gColor, 
            float boxWidth, float boxHeight, float boxDepth)
        {
            var lastColor = Gizmos.color;
            Gizmos.color = gColor;
            
            var lastMatrix = Gizmos.matrix;
            Vector3 originPos = transform.position + transform.forward * boxDepth * 0.5f;
            Gizmos.matrix = Matrix4x4.TRS(originPos, transform.rotation, Vector3.one);
            // 绘制贴花的 box
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(boxWidth, boxHeight, boxDepth));
            Gizmos.matrix = lastMatrix;
            
            // 标记一下原点
            float sphereRadius = Mathf.Min(boxWidth, boxHeight);
            Gizmos.DrawWireSphere(transform.position, sphereRadius * 0.05f);
            
            // 绘制投影方向的箭头
            var arrowLength = boxDepth * 2f;
            var arrowStart = transform.position;
            var arrowEnd = transform.position + transform.forward * arrowLength;
            Gizmos.DrawLine(arrowStart, arrowEnd);
            
            Vector3 arrowTangent;
            if (Mathf.Abs(transform.forward.y) > 0.999f)
            {
                arrowTangent = Vector3.Cross(transform.forward, Vector3.right);
            }
            else
            {
                arrowTangent = Vector3.Cross(transform.forward, Vector3.up);
            }
            var rotAxis = Vector3.Cross(transform.forward, arrowTangent);
            var rotQuat = Quaternion.AngleAxis(45f, rotAxis.normalized);
            var arrowLeft = rotQuat * transform.forward * arrowLength * -0.2f;
            Gizmos.DrawLine(arrowEnd, arrowEnd + arrowLeft);
            
            rotQuat = Quaternion.AngleAxis(-45f, rotAxis.normalized);
            var arrowRight = rotQuat * transform.forward * arrowLength * -0.2f;
            Gizmos.DrawLine(arrowEnd, arrowEnd + arrowRight);
            
            Gizmos.color = lastColor;
        }
        
    }
}