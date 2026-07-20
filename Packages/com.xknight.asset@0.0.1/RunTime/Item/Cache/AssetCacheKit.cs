using System.Collections.Generic;

namespace XKAsset
{
	public class AssetCacheKit
	{
		public AssetHandleCache assetHandleCache { get; private set; }
		public PathHandleCache pathHandleCache { get; private set; }
		public UseTimeCache useTimeCache { get; private set; }
		public SpritePathCache spritePathCache { get; private set; }
		public TimeSlotAssetCache timeSlotCache { get; private set; }
		public AssetLoadReleaseLogCache logCache { get; private set; }

		private List<IAssetCache> _listCache;
		
		public void InitCaches()
		{
			_listCache = new List<IAssetCache>();
			assetHandleCache = AddCaches(new AssetHandleCache());
			pathHandleCache = AddCaches(new PathHandleCache());
			useTimeCache = AddCaches(new UseTimeCache());
			spritePathCache = AddCaches(new SpritePathCache());
			timeSlotCache = AddCaches(new TimeSlotAssetCache());
			logCache = AddCaches(new AssetLoadReleaseLogCache());
		}

		public void StartLoad(string key)
		{
			for (int i = 0, cnt = _listCache.Count; i < cnt; i++)
			{
				if (!_listCache[i].IsOpenCache())
					continue;
				_listCache[i].StartLoad(key);
			}
		}

		public void Loaded(AsyncOperationHandle handle)
		{
			for (int i = 0, cnt = _listCache.Count; i < cnt; i++)
			{
				if (!_listCache[i].IsOpenCache())
					continue;
				_listCache[i].Loaded(handle);
			}
		}

		public void Release(string key)
		{
			for (int i = 0, cnt = _listCache.Count; i < cnt; i++)
			{
				if (!_listCache[i].IsOpenCache())
					continue;
				_listCache[i].Release(key);
			}
		}

		public void Destroy(AsyncOperationHandle handle)
		{
			for (int i = 0, cnt = _listCache.Count; i < cnt; i++)
			{
				if (!_listCache[i].IsOpenCache())
					continue;
				_listCache[i].Destroy(handle);
			}
		}

		private T AddCaches<T>(T obj) where T : IAssetCache
		{
			obj.Init();
			_listCache.Add(obj);
			return obj;
		}
	}
}