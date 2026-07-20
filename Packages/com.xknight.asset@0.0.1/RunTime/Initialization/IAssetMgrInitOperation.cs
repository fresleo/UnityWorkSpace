namespace XKAsset
{
	public interface IAssetMgrInitOperation
	{
		/// <summary>
		/// 初始化定位器
		/// </summary>
		void InitLocator(AssetManager mgr);
		
		/// <summary>
		/// 初始化加载器
		/// </summary>
		void InitProvider(AssetManager mgr);
	}
}