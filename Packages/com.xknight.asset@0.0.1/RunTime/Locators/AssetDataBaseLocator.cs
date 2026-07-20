#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace XKAsset
{
	public class AssetDataBaseLocator : IResourceLocator
	{
		private Dictionary<string, IResourceLocation> _locations = new Dictionary<string, IResourceLocation>();
		public string locatorId { get; }

		private string _rootPath;

		public AssetDataBaseLocator(string path)
		{
			_rootPath = path;
		}
		
		public bool Locate(string key, Type t, out IResourceLocation location)
		{
			key = string.Intern(key);
			if (key.Contains('\\'))
			{
				location = null;
				Debug.LogError($"[资源加载] 资源配置路径只能使用 '/' : {key}");
				return false;
			}
			var relativePath = key;
			if (key.StartsWith("Assets/OriginalRes/"))
			{
				relativePath = key.Replace("Assets/", "");
			}
			else
			{
				relativePath = Path.Combine(_rootPath, key);
			}
			
			if (!File.Exists(Application.dataPath + "/" + relativePath))
			{
				Debug.LogWarning("This Resource is not in config directories." + key);
				relativePath = key;
			}

			if (!_locations.TryGetValue(key, out location))
			{
				location = new ResourceLocationBase(typeof(AssetDataBaseProvider).FullName, relativePath, t, null);
				_locations.Add(key, location);
			}
			return true;
		}
	}
}
#endif