namespace XKAsset
{
	public class AssetMgrInitOpBase : IAssetMgrInitOperation
	{
		public void InitLocator(AssetManager mgr)
		{
			OnInitLocator(mgr);
		}

		public void InitProvider(AssetManager mgr)
		{
			mgr.AddProvider(new BytesAssetProvider());
			OnInitProvider(mgr);
		}

		protected virtual void OnInitLocator(AssetManager mgr){}
		protected virtual void OnInitProvider(AssetManager mgr){}
	}
}