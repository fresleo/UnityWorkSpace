// Created By: WangYu  Date: 2024-11-22

#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEngine;

namespace RaindropEffect
{
    public static class RaindropEffectEditorUtils
    {
        public static void CreateScriptableObject<TScriptableObject>(string assetFileName) 
            where TScriptableObject : ScriptableObject
        {
            var assetObj = ScriptableObject.CreateInstance<TScriptableObject>();

            string selectPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(selectPath))
            {
                selectPath = "Assets";
            }
            else if (Path.GetExtension(selectPath) != "")
            {
                selectPath = selectPath.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
            }

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{selectPath}/{assetFileName}.asset");

            AssetDatabase.CreateAsset(assetObj, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            //EditorUtility.FocusProjectWindow();
            Selection.activeObject = assetObj;
        }
    }
}

#endif // UNITY_EDITOR
