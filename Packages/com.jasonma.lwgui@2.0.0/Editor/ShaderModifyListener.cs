// Copyright (c) Jason Ma
using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LWGUI
{
	public class ShaderModifyListener : AssetPostprocessor
	{
		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();
			
			foreach (var assetPath in importedAssets)
			{
				if (Path.GetExtension(assetPath).Equals(".shader", StringComparison.OrdinalIgnoreCase))
				{
					var shader = AssetDatabase.LoadAssetAtPath<Shader>(assetPath);
					MetaDataHelper.ReleaseShaderMetadataCache(shader);
				}
			}
			
			sw.Stop();
			Debug.LogWarning($"[AssetPostprocessor] ShaderModifyListener PostprocessAllAssets cost {sw.ElapsedMilliseconds} ms");
		}
	}
}