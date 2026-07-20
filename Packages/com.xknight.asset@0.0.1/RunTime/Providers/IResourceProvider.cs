using System.Collections.Generic;

namespace XKAsset
{
	public interface IResourceProvider
	{
		string ProvideId { get; }
		
		/// <summary>
		/// 是否可以加载
		/// </summary>
		/// <returns></returns>
		bool CanProvide();
		
		/// <summary>
		/// 加载
		/// </summary>
		void Provide(IProviderOperation op);

		/// <summary>
		/// 释放
		/// </summary>
		void Release<T>(T result);
	}
}