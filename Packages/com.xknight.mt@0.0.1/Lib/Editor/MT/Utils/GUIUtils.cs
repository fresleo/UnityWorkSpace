// Created By: WangYu  Date: 2023-12-07

using com.xknight.mt.Lib.Runtime.MT.Common;
using UnityEditor;
using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.Utils
{
    public static class GUIUtils
    {
        /// <summary>
        /// 绘制线框立方体
        /// </summary>
        /// <param name="position"></param>
        /// <param name="size"></param>
        /// <param name="space"></param>
        /// <param name="color"></param>
        public static void DrawWireCube(Vector3 position, Vector3 size, Transform space, Color color)
        {
            Vector3 halfSize = size * 0.5f;

            Vector3 a, b, c, d, e, f, g, h;
            
            a = position + new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
            b = position + new Vector3(halfSize.x, -halfSize.y, halfSize.z);
            c = position + new Vector3(halfSize.x, halfSize.y, halfSize.z);
            d = position + new Vector3(-halfSize.x, halfSize.y, halfSize.z);

            e = position + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
            f = position + new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
            g = position + new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
            h = position + new Vector3(halfSize.x, halfSize.y, -halfSize.z);

            if (space != null)
            {
                a = space.TransformPoint(a);
                b = space.TransformPoint(b);
                c = space.TransformPoint(c);
                d = space.TransformPoint(d);
                e = space.TransformPoint(e);
                f = space.TransformPoint(f);
                g = space.TransformPoint(g);
                h = space.TransformPoint(h);
            }

            Color lastColor = Handles.color;
            Handles.color = color;

            // draw front
            Handles.DrawLine(a, b);
            Handles.DrawLine(a, d);
            Handles.DrawLine(c, b);
            Handles.DrawLine(c, d);
            // draw back
            Handles.DrawLine(e, f);
            Handles.DrawLine(e, g);
            Handles.DrawLine(h, f);
            Handles.DrawLine(h, g);
            // draw corners
            Handles.DrawLine(e, a);
            Handles.DrawLine(f, b);
            Handles.DrawLine(g, d);
            Handles.DrawLine(h, c);

            Handles.color = lastColor;
        }
        
        /// <summary>
        /// 绘制可控制的盒子体积 GUI
        /// </summary>
        /// <param name="script"></param>
        /// <typeparam name="TScript"></typeparam>
        public static void DrawCubeVolume<TScript>(TScript script) 
            where TScript : MonoBehaviour, ICubeVolume
        {
            DrawWireCube(script.CubeCenter, script.CubeSize, null, Color.white);

            //计算控制柄的位置
            script.CubeHandlePositions[0] = script.CubeCenter + new Vector3(script.CubeSize.x * 0.5f, 0, 0);
            script.CubeHandlePositions[1] = script.CubeCenter + new Vector3(script.CubeSize.x * -0.5f, 0, 0);
            script.CubeHandlePositions[2] = script.CubeCenter + new Vector3(0, script.CubeSize.y * 0.5f, 0);
            script.CubeHandlePositions[3] = script.CubeCenter + new Vector3(0, script.CubeSize.y * -0.5f, 0);
            script.CubeHandlePositions[4] = script.CubeCenter + new Vector3(0, 0, script.CubeSize.z * 0.5f);
            script.CubeHandlePositions[5] = script.CubeCenter + new Vector3(0, 0, script.CubeSize.z * -0.5f);
            
            Vector3[] newHandlePositions = new Vector3[6];
            Handles.CapFunction handle = Handles.DotHandleCap;
            float sizeFactor = 0.05f;
            float snap = 0.1f;

            newHandlePositions[0] = Handles.Slider(script.CubeHandlePositions[0], Vector3.right, HandleUtility.GetHandleSize(script.CubeHandlePositions[0]) * sizeFactor, handle, snap);
            newHandlePositions[1] = Handles.Slider(script.CubeHandlePositions[1], -Vector3.right, HandleUtility.GetHandleSize(script.CubeHandlePositions[1]) * sizeFactor, handle, snap);
            newHandlePositions[2] = Handles.Slider(script.CubeHandlePositions[2], Vector3.up, HandleUtility.GetHandleSize(script.CubeHandlePositions[2]) * sizeFactor, handle, snap);
            newHandlePositions[3] = Handles.Slider(script.CubeHandlePositions[3], -Vector3.up, HandleUtility.GetHandleSize(script.CubeHandlePositions[3]) * sizeFactor, handle, snap);
            newHandlePositions[4] = Handles.Slider(script.CubeHandlePositions[4], Vector3.forward, HandleUtility.GetHandleSize(script.CubeHandlePositions[4]) * sizeFactor, handle, snap);
            newHandlePositions[5] = Handles.Slider(script.CubeHandlePositions[5], -Vector3.forward, HandleUtility.GetHandleSize(script.CubeHandlePositions[5]) * sizeFactor, handle, snap);

            bool changed = newHandlePositions[0] != script.CubeHandlePositions[0] || 
                           newHandlePositions[1] != script.CubeHandlePositions[1] ||
                           newHandlePositions[2] != script.CubeHandlePositions[1] ||
                           newHandlePositions[3] != script.CubeHandlePositions[1] ||
                           newHandlePositions[4] != script.CubeHandlePositions[1] ||
                           newHandlePositions[5] != script.CubeHandlePositions[1];
            if (changed)
            {
                Undo.RecordObject(script, $"{script.name} - Script");
                
                Vector3 newCubeSize = script.CubeSize;
                
                Vector3 changeHandlePosition = newHandlePositions[0] - script.CubeHandlePositions[0];
                if (changeHandlePosition.sqrMagnitude != 0.0f)
                {
                    newCubeSize.x = (script.CubeCenter - newHandlePositions[0]).magnitude * 2;
                }

                changeHandlePosition = newHandlePositions[1] - script.CubeHandlePositions[1];
                if (changeHandlePosition.sqrMagnitude != 0.0f)
                {
                    newCubeSize.x = (script.CubeCenter - newHandlePositions[1]).magnitude * 2;
                }

                changeHandlePosition = newHandlePositions[2] - script.CubeHandlePositions[2];
                if (changeHandlePosition.sqrMagnitude != 0.0f)
                {
                    newCubeSize.y = (script.CubeCenter - newHandlePositions[2]).magnitude * 2;
                }

                changeHandlePosition = newHandlePositions[3] - script.CubeHandlePositions[3];
                if (changeHandlePosition.sqrMagnitude != 0.0f)
                {
                    newCubeSize.y = (script.CubeCenter - newHandlePositions[3]).magnitude * 2;
                }

                changeHandlePosition = newHandlePositions[4] - script.CubeHandlePositions[4];
                if (changeHandlePosition.sqrMagnitude != 0.0f)
                {
                    newCubeSize.z = (script.CubeCenter - newHandlePositions[4]).magnitude * 2;
                }

                changeHandlePosition = newHandlePositions[5] - script.CubeHandlePositions[5];
                if (changeHandlePosition.sqrMagnitude != 0.0f)
                {
                    newCubeSize.z = (script.CubeCenter - newHandlePositions[5]).magnitude * 2;
                }

                if (script.CubeSize != newCubeSize)
                {
                    script.CubeSize = newCubeSize;
                }
            }
        }
        
        /// <summary>
        /// 根据运行时对象获取编辑器对象
        /// </summary>
        /// <param name="runtimeObj">运行时对象</param>
        /// <typeparam name="TEditor">编辑器类</typeparam>
        /// <returns>编辑器对象</returns>
        public static TEditor GetEditorObjByRuntimeObj<TEditor>(UnityEngine.Object runtimeObj) 
            where TEditor : UnityEditor.Editor
        {
            var editorType = typeof(TEditor);
            var editorObj = UnityEditor.Editor.CreateEditor(runtimeObj, editorType) as TEditor;

            return editorObj;
        }
        
    }
}