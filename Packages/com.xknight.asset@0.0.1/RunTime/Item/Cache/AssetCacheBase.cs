namespace XKAsset
{
	public class AssetCacheBase : IAssetCache
	{
		public void Init()
		{
			OnInit();
		}

		public bool IsOpenCache()
		{
			return IsOpen();
		}

		public void StartLoad(string key)
		{
			OnStartLoad(key);
		}

		public void Loaded(AsyncOperationHandle handle)
		{
			OnLoaded(handle);
		}

		public void Release(string key)
		{
			OnRelease(key);
		}

		public void Destroy(AsyncOperationHandle handle)
		{
			OnDestroy(handle);
		}
		
		protected virtual void OnInit(){}

		protected virtual bool IsOpen() { return true; }
		protected virtual void OnStartLoad(string key){}
		protected virtual void OnLoaded(AsyncOperationHandle handle){}
		protected virtual void OnRelease(string key){}
		protected virtual void OnDestroy(AsyncOperationHandle handle){}
	}
}