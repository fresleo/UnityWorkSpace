using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal
{
    public class AssetCreatorFactory
    {
        [MenuItem("Assets/Create/SkyPrefab", priority = CoreUtils.assetCreateMenuPriority1)]
        public static void CreateEnvironment_SkyProfile()
        {
            DoCreate<SkyProfileAssetCreator>(null, "SkyProfile", "asset");
        }

        private static void DoCreate<T>(Texture2D icon, string name, string extension = "") where T : EndNameEditAction
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<T>(), $"{name}.{extension}", null, null);
        }
    }
}