/*******************************************************************************
 * File: AHDBakeRuntimeBridge.cs
 * Author: WangYu
 * Date: 2026-05-13
 * Description: AHD 烘焙结果与 Unity 场景运行时绑定器之间的桥接。
 * Notice: 仅用于 Unity Editor 烘焙流程。
 *******************************************************************************/

using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace XKnight.AHDBaker.Editor
{
    internal static class AHDBakeRuntimeBridge
    {
        public static XKnightAHDBakedLightTableBinder FindOrCreateSceneBinder()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            XKnightAHDBakedLightTableBinder[] binders = Resources.FindObjectsOfTypeAll<XKnightAHDBakedLightTableBinder>();
            for (int i = 0; i < binders.Length; i++)
            {
                var binder = binders[i];
                if (binder == null || binder.gameObject == null)
                {
                    continue;
                }

                if (binder.gameObject.scene == activeScene)
                {
                    return binder;
                }
            }

            GameObject binderObject = new GameObject("AHD Baked Specular Binder");
            Undo.RegisterCreatedObjectUndo(binderObject, "创建 AHD 烘焙高光绑定器");
            
            if (activeScene.IsValid())
            {
                SceneManager.MoveGameObjectToScene(binderObject, activeScene);
            }

            return binderObject.AddComponent<XKnightAHDBakedLightTableBinder>();
        }

        /// <summary>
        /// 尝试获取默认的输出目录，也就是和场景同名的烘焙结果文件夹。
        /// </summary>
        public static bool TryGetDefaultOutputFolder(out string outputFolder)
        {
            outputFolder = string.Empty;
            
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || string.IsNullOrEmpty(activeScene.path))
            {
                return false;
            }

            string scenePath = activeScene.path.Replace("\\", "/");
            string sceneFolder = Path.GetDirectoryName(scenePath)?.Replace("\\", "/");
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            if (string.IsNullOrEmpty(sceneFolder) || string.IsNullOrEmpty(sceneName))
            {
                return false;
            }

            outputFolder = sceneFolder + "/" + sceneName;
            return true;
        }

        public static void ApplyDirectionMaps(Texture2D[] directionMaps)
        {
            XKnightAHDBakedLightTableBinder binder = FindOrCreateSceneBinder();
            
            Undo.RecordObject(binder, "分配 AHD 烘焙高光方向图");
            binder.SetDirectionMaps(directionMaps);
            EditorUtility.SetDirty(binder);
            
            if (binder.gameObject != null)
            {
                EditorSceneManager.MarkSceneDirty(binder.gameObject.scene);
                Selection.activeObject = binder.gameObject;
            }
        }

        public static Texture2D SaveDirectionMap(Texture2D texture, string assetPath)
        {
            if (texture == null || string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            UnityEngine.Object existingObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (existingObject != null && !(existingObject is Texture2D))
            {
                Debug.LogError("[AHD Baker] 方向图路径已占用: " + assetPath);
                UnityEngine.Object.DestroyImmediate(texture);
                return null;
            }

            byte[] pngData = texture.EncodeToPNG();
            if (pngData == null || pngData.Length == 0)
            {
                Debug.LogError("[AHD Baker] 方向图 PNG 编码失败: " + assetPath);
                UnityEngine.Object.DestroyImmediate(texture);
                return null;
            }

            string fullPath = GetProjectRelativeFullPath(assetPath);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(fullPath, pngData);
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.DirectionalLightmap;
                importer.sRGBTexture = false;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = false;
                importer.isReadable = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.anisoLevel = 0;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        public static Texture2D SaveDebugMap(Texture2D texture, string assetPath)
        {
            if (texture == null || string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            byte[] pngData = texture.EncodeToPNG();
            if (pngData == null || pngData.Length == 0)
            {
                UnityEngine.Object.DestroyImmediate(texture);
                return null;
            }

            string fullPath = GetProjectRelativeFullPath(assetPath);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(fullPath, pngData);
            UnityEngine.Object.DestroyImmediate(texture);
            
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = false;
                importer.isReadable = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        /// <summary>
        /// 将相对于项目根目录的路径转换为绝对路径。
        /// </summary>
        public static string GetProjectRelativeFullPath(string assetPath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string platformPath = assetPath.Replace('/', Path.DirectorySeparatorChar);
            return Path.GetFullPath(Path.Combine(projectRoot, platformPath));
        }
        
    }
}
