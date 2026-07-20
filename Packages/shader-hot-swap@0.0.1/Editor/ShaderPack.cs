using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ShaderHotSwap
{
    public static class ShaderPack
    {
        private static string s_logHeader = $"[{nameof(ShaderPack)}]";
        
        public static string PackShaders(BuildTarget buildTarget, string outputDir, ShaderData[] shaderDataList)
        {
            Debug.Log($"{s_logHeader} 着色器数量: {shaderDataList.Length}");
            
            var assetBundleName = "shaderList";
            var assetBundlePath = Path.Combine(outputDir, assetBundleName);
            Directory.CreateDirectory(outputDir);

            var buildMap = new AssetBundleBuild[1];
            buildMap[0].assetBundleName = assetBundleName;
            
            var assetPaths = new List<string>();
            var assetNames = new List<string>();
            for (int i = 0; i < shaderDataList.Length; ++i)
            {
                var shaderPath = AssetDatabase.GetAssetPath(shaderDataList[i].shader);
                assetPaths.Add(shaderPath);
                assetNames.Add(AssetDatabase.AssetPathToGUID(shaderPath));
                
                Debug.Log($"{s_logHeader} 资源路径: {assetPaths[i]}");
                
                // 引用材质
                /*
                if (shaderDataList[i].refMaterial != null)
                {
                    var matPath = AssetDatabase.GetAssetPath(shaderDataList[i].refMaterial);
                    assetPaths.Add(matPath);
                    assetNames.Add(AssetDatabase.AssetPathToGUID(matPath));
                }
                */
            }

            buildMap[0].assetNames = assetPaths.ToArray();
            buildMap[0].addressableNames = assetNames.ToArray();

            BuildPipeline.BuildAssetBundles(outputDir, buildMap, BuildAssetBundleOptions.None, buildTarget);

            var bytes = File.ReadAllBytes(assetBundlePath);
            string base64 = Convert.ToBase64String(bytes);

            return base64;
        }
    }
}