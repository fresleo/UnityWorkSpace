using System.Collections.Generic;

namespace XKAsset
{
	public class PathHandleCache : AssetCacheBase
	{
		private Dictionary<string, AsyncOperationHandle> _pathToHandleDic;

		protected override void OnInit()
		{
			_pathToHandleDic = new Dictionary<string, AsyncOperationHandle>();
		}

		protected override void OnLoaded(AsyncOperationHandle handle)
		{
			if (handle.Operation.Location == null)
				return;	//只记录有定位器的资源
			_pathToHandleDic.TryAdd(handle.Operation.Location.RelativePath, handle);
		}

		protected override void OnDestroy(AsyncOperationHandle handle)
		{
			if (handle.Operation.Location == null)
				return;
			_pathToHandleDic.Remove(handle.Operation.Location.RelativePath);
		}

		public Dictionary<string, List<string>> GetCurUsedBundle()
		{
			Dictionary<string, List<string>> resDic = new Dictionary<string, List<string>>();
			foreach (var item in _pathToHandleDic)
			{
				var deps = item.Value.Operation.GetDependencies();
				if (deps == null)
					continue;
				foreach (var depLoc in deps)
				{
					if (!resDic.TryGetValue(depLoc.RelativePath, out var list))
					{
						list = new List<string>();
						resDic.Add(depLoc.RelativePath, list);
					}
					if(!list.Contains(item.Key))
						list.Add(item.Key);
				}
			}
			return resDic;
		}
		
		public Dictionary<string, List<string>> GetCurUsedAsset()
		{
			Dictionary<string, List<string>> resDic = new Dictionary<string, List<string>>();
			foreach (var item in _pathToHandleDic)
			{
				var deps = item.Value.Operation.GetDependencies();
				if (deps == null)
					continue;
				if (!resDic.TryGetValue(item.Key, out var list))
				{
					list = new List<string>();
					resDic.Add(item.Key, list);
				}
				foreach (var depLoc in deps)
				{
					if(!list.Contains(depLoc.RelativePath))
						list.Add(depLoc.RelativePath);
				}
			}
			return resDic;
		}

		public bool GetHandle(string key, out AsyncOperationHandle handle)
		{
			return _pathToHandleDic.TryGetValue(key, out handle);
		}
	}
}