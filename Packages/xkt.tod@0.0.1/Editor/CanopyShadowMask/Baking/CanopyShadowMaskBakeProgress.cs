/*******************************************************************************
 * File: CanopyShadowMaskBakeProgress.cs
 * Author: WangYu
 * Date: 2026-07-06
 * Description: 
 ******************************************************************************/

using System;
using UnityEditor;
using UnityEngine;

namespace XKT.TOD.CanopyShadowMask
{
    /// <summary>
    /// 树冠 Shadowmask 烘焙进度
    /// </summary>
    internal sealed class CanopyShadowMaskBakeProgress : IDisposable
    {
        private const string C_TITLE = "树冠 Shadowmask 二次烘焙";

        /// <summary>
        /// 用户是否已取消烘焙。
        /// </summary>
        public bool IsCancelled { get; private set; }
        
        public void Dispose()
        {
            EditorUtility.ClearProgressBar();
        }
        
        /// <summary>
        /// 汇报进度
        /// false = 表示应中止烘焙
        /// </summary>
        public bool Report(float progress, string info)
        {
            if (IsCancelled)
            {
                return false;
            }

            bool cancelled = EditorUtility.DisplayCancelableProgressBar(
                C_TITLE, info, Mathf.Clamp01(progress));
            if (cancelled)
            {
                IsCancelled = true;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 在范围内按子步骤汇报进度
        /// </summary>
        public bool ReportStep(
            float rangeStart, float rangeEnd,
            int stepIndex, int stepCount,
            string info)
        {
            if (stepCount <= 0)
            {
                return Report(rangeStart, info);
            }

            float t = (stepIndex + 1f) / stepCount;
            float progress = Mathf.Lerp(rangeStart, rangeEnd, t);
            return Report(progress, info);
        }
        
    }
}