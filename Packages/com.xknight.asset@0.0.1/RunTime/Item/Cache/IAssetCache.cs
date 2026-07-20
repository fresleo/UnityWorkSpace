namespace XKAsset
{
	public interface IAssetCache
	{
		/// <summary>
		/// 初始化
		/// </summary>
		public void Init();

		/// <summary>
		/// 是否打开缓存
		/// </summary>
		/// <returns></returns>
		public bool IsOpenCache();

		/// <summary>
		/// 开始加载
		/// </summary>
		/// <param name="key"></param>
		public void StartLoad(string key);
		
		/// <summary>
		/// 加载完成
		/// </summary>
		public void Loaded(AsyncOperationHandle handle);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="key"></param>
		public void Release(string key);

		/// <summary>
		/// 销毁
		/// </summary>
		public void Destroy(AsyncOperationHandle handle);
	}
}