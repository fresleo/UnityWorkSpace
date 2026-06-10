using UnityEngine;
using UnityEditor;

namespace Garena.TA.SSS
{
    /// <summary>
    /// Editor 工具：把若干 N×1 kernel 纹理打包成 Texture2DArray 供 SSSS_HDRP_DeferredPass 使用。
    ///
    /// 使用：
    ///   1. 先用 SSS_DiscSampling 生成若干 disc kernel（每个 profile 一个，sample count 一致）
    ///   2. 选中这些 kernel 纹理，右键 → Tools → SSS → 打包选中为 KernelArray，保存生成的 Texture2DArray 资源
    /// 

    public class SSSKernelArrayPackerEditor
    {
        [MenuItem("Tools/SSS/打包选中为 KernelArray", true)]
        static bool ValidatePackSelected()
        {
            // 只有选中2个及以上Texture2D才可用
            var textures = Selection.GetFiltered<Texture2D>(SelectionMode.Assets);
            return textures != null && textures.Length > 1;
        }

        [MenuItem("Tools/SSS/打包选中为 KernelArray")]
        static void PackSelected()
        {
            var textures = Selection.GetFiltered<Texture2D>(SelectionMode.Assets);
            if (textures == null || textures.Length == 0)
            {
                EditorUtility.DisplayDialog("错误", "请至少选中两个 Texture2D！", "OK");
                return;
            }

            // 调用你的打包方法
            var array = SSS_KernelArrayPacker.Pack(textures);

            // 选择保存路径
            string path = EditorUtility.SaveFilePanelInProject(
                "保存 SSS KernelArray",
                "SSS_KernelArray.asset",
                "asset",
                "选择保存路径"
            );
            if (string.IsNullOrEmpty(path)) return;

            AssetDatabase.CreateAsset(array, path);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("完成", $"已保存到 {path}", "OK");
        }
    }
}
