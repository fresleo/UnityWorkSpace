using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace XKAsset
{
	public partial class AssetManager : IAssetManager
	{
		private static AssetManager _instance;
		public static AssetManager Instance
		{
			get
			{
				if(_instance == null)
					_instance = new AssetManager();
				return _instance;
			}
		}

#region 加载
		
		public AsyncOperationHandle<T> LoadAssetAsync<T>(string key)
		{
			IResourceLocation loc = GetResourceLocation(key, typeof(T));
			if (loc == null)
			{
				return new AsyncOperationHandle<T>(CompleteOperation<T>.Create($"[资源加载] 未找到资源定位器 {key}"));
			}
			return LoadAssetAsync<T>(loc);
		}

		public AsyncOperationHandle LoadAssetAsync(string key, Type t)
		{
			IResourceLocation loc = GetResourceLocation(key, t);
			if (loc == null)
			{
				return new AsyncOperationHandle(CompleteOperation<object>.Create($"[资源加载] 未找到资源定位器 {key}"));
			}
			return LoadAssetAsync(loc, t);
		}
		
		public AsyncOperationHandle<T> LoadAssetAsync<T>(IResourceLocation loc)
		{
			if (loc != null)
			{
				Type t = typeof(T);
				var provider = GetResourceProvider(loc);
				if (provider != null)
					return ProvideResource<T>(loc, t);
			}

			return new AsyncOperationHandle<T>(CompleteOperation<T>.Create("[资源加载] Provider创建失败"));
		}
		
		public AsyncOperationHandle LoadAssetAsync(IResourceLocation loc, Type t)
		{
			if (loc != null)
			{
				var provider = GetResourceProvider(loc);
				if (provider != null)
					return ProvideResource(loc, t);
			}

			return new AsyncOperationHandle(CompleteOperation<object>.Create("[资源加载] Provider创建失败"));
		}
		
		public AsyncOperationHandle LoadAssetHandle(string key, Type t)
		{
			var op = LoadAssetAsync(key, t);
			op.WaitForCompletion();
			return op;
		}
		
		public T LoadAsset<T>(string key)
		{
			var op = LoadAssetAsync<T>(key);
			op.WaitForCompletion();
			return op.Result;
		}

		/// <summary>
		/// 获取资源URL
		/// </summary>
		/// <param name="cfgPath"></param>
		/// <returns></returns>
		public string GetAssetFileUrl(string cfgPath)
		{//默认资源在获取第一个引用中
			var loc = GetResourceLocation(cfgPath, null);
			if (loc == null)
				return "";
			return loc.AbsUrl;
		}

#endregion

#region 卸载

		public void Release<T>(T obj) where T : Object
		{
			if (obj == null)
				return;
			if (_cachesKit.assetHandleCache.GetHandle(obj, out AsyncOperationHandle handle))
			{
				Release(handle);
			}
		}

		public void Release<T>(AsyncOperationHandle<T> handle)
		{
			if (handle.Operation.Status == AsyncOperationStatus.STATUS_RUNNING)
				return;
			handle.Release();
		}
		
		public void ReleaseWithHandle(AsyncOperationHandle handle)
		{
			Release(handle);
		}
				
		public void Release(string path)
		{
			if (path == null)
				return;
			if (_cachesKit.pathHandleCache.GetHandle(path.ToLower(), out AsyncOperationHandle handle))
			{
				Release(handle);
			}
		}

		private void Release(AsyncOperationHandle handle)
		{
			if (handle.Operation == null || handle.Operation.Location == null)
			{
				handle.Release();
				return;
			}
			_cachesKit.Release(handle.Operation.Location.RelativePath);
			if (handle.Status == AsyncOperationStatus.STATUS_RUNNING)
				return;
			handle.Release();
		}

#endregion

		/// <summary>
		/// 获取正在加载的资源数量
		/// </summary>
		/// <returns></returns>
		public int GetLoadingCount()
		{
			if (_loadedCache == null)
				return 0;
			int nCnt = 0;
			foreach (var item in _loadedCache)
			{
				if (!item.Value.IsDone)
					nCnt++;
			}

			return nCnt;
		}

		private IResourceLocation GetResourceLocation(string key, Type t)
		{
			IResourceLocation loc;
			foreach (var locatorItem in _locators)
			{
				if (locatorItem.Locate(key, t, out loc))
				{
					return loc;
				}
			}
			return null;
		}

		internal AsyncOperationHandle<T> ProvideResource<T>(IResourceLocation loc, Type t)
		{
			var handle = ProvideResource(loc, t);
			return handle.Convert<T>();
		}

		/// <summary>
		/// 获取资源加载器
		/// </summary>
		/// <param name="loc"></param>
		/// <returns></returns>
		private IResourceProvider GetResourceProvider(IResourceLocation loc)
		{
			if (!_dicProviders.TryGetValue(loc.ProviderId, out var provider))
				provider = null;
			return provider;
		}
		
		private T CreateOperation<T>(Type type, int hashCode)
		{
			var op = (T) Allocator.New(type, hashCode);
			_loadedCache.Add(hashCode, (IAsyncOperation)op);
			return op;
		}

		private Dictionary<Type, Type> m_ProviderOperationTypeCache = new Dictionary<Type, Type>();
		private AsyncOperationHandle ProvideResource(IResourceLocation location, Type t)
		{
			if (location == null)
				return default;
			_cachesKit.StartLoad(location.RelativePath);
			var provider = GetResourceProvider(location);
			AsyncOperationHandle<IList<AsyncOperationHandle>> groupOp = default;
			if (location.HasDeps)
			{
				groupOp = ProvideResourceGroupCached(location.Dependencies, t, location.DepsHashCode);
			}

			if (_loadedCache.TryGetValue(location.HashCode, out IAsyncOperation op))
			{
				op.IncrementReferenceCount();
				return new AsyncOperationHandle(op);
			}
			if (!m_ProviderOperationTypeCache.TryGetValue(t, out Type provType))
				m_ProviderOperationTypeCache.Add(t, provType = typeof(ProvideOperation<>).MakeGenericType(new Type[] { t }));
			op = CreateOperation<IAsyncOperation>(provType, location.HashCode);
			
			((IProviderOperation)op).Init(location, provider, groupOp);
			StartOperation(op, groupOp);
			var handle = new AsyncOperationHandle(op);
			handle.Completed += OnAssetLoaded;
			return handle;
		}

		private void StartOperation(IAsyncOperation op, AsyncOperationHandle deps)
		{
			op.Start(deps);
		}

		private AsyncOperationHandle<IList<AsyncOperationHandle>> ProvideResourceGroupCached(IList<IResourceLocation> locs, Type t, int depsHash)
		{
			IAsyncOperation groupOp;
			if (_loadedCache.TryGetValue(depsHash, out groupOp))
			{
				groupOp.IncrementReferenceCount();
				return new AsyncOperationHandle<IList<AsyncOperationHandle>>(groupOp);
			}
			groupOp = CreateOperation<GroupOperation>(typeof(GroupOperation), depsHash);
			var listOps = new List<AsyncOperationHandle>(locs.Count);
			foreach (var locItem in locs)
			{
				listOps.Add(ProvideResource(locItem, locItem.ResourceType));
			}

			((GroupOperation)groupOp).Init(listOps, depsHash);
			StartOperation(groupOp, default);
			var handle = new AsyncOperationHandle<IList<AsyncOperationHandle>>(groupOp);
			handle.TypelessCompleted += OnAssetLoaded;
			return handle;
		}

		private void OnAssetLoaded(AsyncOperationHandle handle)
		{
			handle.Destroyed += OnHandleDestroyed;
			_cachesKit.Loaded(handle);
			if (handle.Status != AsyncOperationStatus.STATUS_SUCCESS)
			{//加载失败，直接释放
				_loadedCache.Remove(handle.Operation.Key);
				handle.Release();
			}
		}

		private void OnHandleDestroyed(AsyncOperationHandle handle)
		{
			_cachesKit.Destroy(handle);
			_loadedCache.Remove(handle.Operation.Key);
		}

		public List<string> GetSpritePathList()
		{
			return _cachesKit.spritePathCache.GetUsedSprtePathList();
		}

        public Dictionary<string, List<string>> GetCurUseBundle()
        {
	        return _cachesKit.pathHandleCache.GetCurUsedBundle();
        }
        
        public Dictionary<string, List<string>> GetCurUseAsset()
        {
	        return _cachesKit.pathHandleCache.GetCurUsedAsset();
        }

        public List<string> GetTimeSlotUnloadAsset()
        {
	        return _cachesKit.timeSlotCache.GetUnloadAsset();
        }
    }
}