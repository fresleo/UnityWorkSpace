using System;
using System.Collections.Generic;

namespace XKAsset
{
	/// <summary>
	/// 资源定位器
	/// </summary>
	public interface IResourceLocator
	{
		/// <summary>
		/// 定位器ID
		/// </summary>
		public string locatorId { get; }

		/// <summary>
		/// 获取资源信息
		/// </summary>
		/// <param name="key">资源key</param>
		/// <param name="type">资源类型</param>
		/// <param name="locations">返回资源信息</param>
		/// <returns></returns>
		public bool Locate(string key, Type t, out IResourceLocation locations);
	}
}