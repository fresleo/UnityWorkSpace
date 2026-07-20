/*******************************************************************************
 * File: TimeOfDayEditorProgress.cs
 * Author: Codex
 * Date: 2026/04/24
 * Description: TOD 编辑器流程进度条工具。
 *******************************************************************************/

using UnityEditor;

namespace XKT.TOD
{
    /// <summary>
    /// TOD 编辑器流程进度条。
    /// </summary>
    internal static class TimeOfDayEditorProgress
    {
        private const string Title = "TOD 一键导出";

        public static void Report(string message, float progress)
        {
            EditorUtility.DisplayProgressBar(Title, message, progress);
        }

        public static void Clear()
        {
            EditorUtility.ClearProgressBar();
        }
    }
}
