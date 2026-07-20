using System;
using System.Collections.Generic;

namespace XKAsset
{
	public interface IResourceLocation
	{
		/// <summary>
		/// 加载器ID
		/// </summary>
		string ProviderId { get; }
		
		/// <summary>
		/// 加载路径
		/// </summary>
		string RelativePath { get; }
		
		/// <summary>
		/// 资源绝对路径
		/// </summary>
		public string AbsPath { get; }
		
		/// <summary>
		/// 资源绝对url
		/// </summary>
		public string AbsUrl { get; }
		
		/// <summary>
		/// 资源名称
		/// </summary>
		string ResourceName { get; }
		
		/// <summary>
		/// 依赖
		/// </summary>
		IList<IResourceLocation> Dependencies { get; }
		
		/// <summary>
		/// 是否有依赖项
		/// </summary>
		public bool HasDeps { get; }
		
		/// <summary>
		/// 资源类型
		/// </summary>
		Type ResourceType { get; }

		/// <summary>
		/// 资源信息hash
		/// </summary>
		int HashCode { get; }
		
		/// <summary>
		/// 资源依赖信息Hash
		/// </summary>
		int DepsHashCode { get; }
	}
}