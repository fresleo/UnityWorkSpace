// Created by: WangYu   Date: 2025-12-15

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace ToonPostProcessing
{
    public class PackageConst
    {
        public const string c_packagePath = "Packages/toon.post.processing";
    }

    public static class PackageUtils
    {
        public static void CreateScriptableObject<TScriptableObject>(string assetFileName) 
            where TScriptableObject : ScriptableObject
        {
            var assetObj = ScriptableObject.CreateInstance<TScriptableObject>();

            string projectWindowFolderPath = ProjectWindowUtilReflection.GetActiveFolderPath();
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{projectWindowFolderPath}/{assetFileName}.asset");

            AssetDatabase.CreateAsset(assetObj, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            //EditorUtility.FocusProjectWindow();
            Selection.activeObject = assetObj;
        }
    }
}

#endif // UNITY_EDITOR
