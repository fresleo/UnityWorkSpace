using System;
using System.Collections.Generic;

namespace XKAsset
{
	public partial class AssetManager
	{
		public bool HasInited = false;
		
		private Dictionary<string, IResourceProvider> _dicProviders;
		/// <summary>
		/// 定位器
		/// </summary>
		private List<IResourceLocator> _locators;

		private IAssetMgrInitOperation _mgrInitOperation;
		/// <summary>
		/// 加载中/完成 缓存
		/// </summary>
		private Dictionary<int, IAsyncOperation> _loadedCache;
		private LRUCacheAllocationStrategy Allocator;
		private AssetCacheKit _cachesKit;

		public void InitAssetManager(string rootPath = null)
		{
			_loadedScene = new Dictionary<string, AsyncOperationHandle>();
			
			if(AssetLoadGlobalConfig.IsUseBundle())
				_mgrInitOperation = new BundleAssetMgrInitOp();
#if UNITY_EDITOR
			else
				_mgrInitOperation = new EditorAssetMgrInitOp(rootPath);
#endif

			Allocator = new LRUCacheAllocationStrategy(1000, 1000, 100, 10);
			if(_loadedCache == null)
				_loadedCache = new Dictionary<int, IAsyncOperation>();
			_loadedCache.Clear();
			
			if(_dicProviders == null)
				_dicProviders = new Dictionary<string, IResourceProvider>();
			_dicProviders.Clear();
			
			if(_locators == null)
				_locators = new List<IResourceLocator>();
			_locators.Clear();

			if (_cachesKit == null)
				_cachesKit = new AssetCacheKit();
			_cachesKit.InitCaches();

			_mgrInitOperation.InitProvider(this);
			_mgrInitOperation.InitLocator(this);
		}

		/// <summary>
		/// 刷新定位器(热更后调用)
		/// </summary>
		public void RefreshLocation()
		{
			_mgrInitOperation.InitLocator(this);
		}

		internal void AddProvider(IResourceProvider provider)
		{
			_dicProviders.Add(provider.ProvideId, provider);
		}

		internal void AddLoactor(IResourceLocator locator)
		{
			_locators.Clear();
			_locators.Add(locator);
		}
	}
}