using UnityEngine;

namespace XKAsset
{
	public interface IAssetManager
	{
		/// <summary>
		/// 异步加载资源
		/// </summary>
		/// <param name="key">资源路径</param>
		/// <typeparam name="T">资源类型</typeparam>
		/// <returns></returns>
		AsyncOperationHandle<T> LoadAssetAsync<T>(string key);

		/// <summary>
		/// 同步加载资源
		/// </summary>
		/// <param name="key">资源路径</param>
		/// <typeparam name="T">资源类型</typeparam>
		/// <returns></returns>
		T LoadAsset<T>(string key);
	}
}