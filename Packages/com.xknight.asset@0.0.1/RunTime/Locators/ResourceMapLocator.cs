using System;
using System.Collections.Generic;
using UnityEngine;

namespace XKAsset
{
	public class ResourceMapLocator : IResourceLocator
	{
		private Dictionary<string, IResourceLocation> _dicLocations;
		public string locatorId { get; }

		public ResourceMapLocator()
		{
			_dicLocations = new Dictionary<string, IResourceLocation>();
		}
		
		public bool Locate(string key, Type t, out IResourceLocation locations)
		{
			if (string.IsNullOrEmpty(key))
			{
				locations = null;
				return false;
			}

			//key = key.ToLower();
			return _dicLocations.TryGetValue(key, out locations);
		}

		/// <summary>
		/// 添加资源信息
		/// </summary>
		/// <param name="key"></param>
		/// <param name="loc"></param>
		public void Add(string key, IResourceLocation loc)
		{
			key = string.Intern(key);
			if (_dicLocations.ContainsKey(key))
			{
				Debug.LogError("已经存在此资源信息." + key);
			}
			else
			{
				_dicLocations.Add(key, loc);
			}
		}

		/// <summary>
		/// 尝试获取对应的loc
		/// </summary>
		/// <param name="key"></param>
		/// <param name="loc"></param>
		/// <returns></returns>
		public bool TryGetValue(string key, out IResourceLocation loc)
		{
			return _dicLocations.TryGetValue(key, out loc);
		}
	}
}