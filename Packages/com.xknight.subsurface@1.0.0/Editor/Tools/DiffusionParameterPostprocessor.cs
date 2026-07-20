//<summary>
//  DiffusionParameterPostprocessor.cs
//  author: calvin
//  date: 2026-06-1
//  description:
//          DiffusionParameter资产维护
//</summary>

using UnityEditor;
using XKnight.TA.SSS;

namespace XKnight.TA.SSS
{
    internal class DiffusionParameterPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            // 如果有任何 .asset 文件被删除，标记 dirty 以刷新 Resources.LoadAll 的结果
            foreach (var str in deletedAssets)
            {
                if (str.EndsWith(".asset"))
                {
                    DiffusionParameter.CleanDirty();
                    return;
                }
            }

            // 对于导入和移动，检查是否是 DiffusionParameter 类型
            foreach (var str in importedAssets)
            {
                if (str.EndsWith(".asset") && AssetDatabase.GetMainAssetTypeAtPath(str) == typeof(DiffusionParameter))
                {
                    DiffusionParameter.CleanDirty();
                    return;
                }
            }

            foreach (var str in movedAssets)
            {
                if (str.EndsWith(".asset") && AssetDatabase.GetMainAssetTypeAtPath(str) == typeof(DiffusionParameter))
                {
                    DiffusionParameter.CleanDirty();
                    return;
                }
            }
        }
    }
}
