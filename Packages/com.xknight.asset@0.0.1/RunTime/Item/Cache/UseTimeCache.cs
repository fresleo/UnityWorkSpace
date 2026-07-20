using System;
using System.Collections.Generic;

namespace XKAsset
{
	public class UseTimeCache : AssetCacheBase
	{
		private Dictionary<string, DateTime> _usedTimeDic;

		protected override void OnInit()
		{
			_usedTimeDic = new Dictionary<string, DateTime>();
		}

		protected override bool IsOpen()
		{
			return AssetCacheSwitch.ACC_OPEN_TIME_LOG;
		}

		protected override void OnStartLoad(string key)
		{
			_usedTimeDic.TryAdd(key, DateTime.Now);
		}

		protected override void OnLoaded(AsyncOperationHandle handle)
		{
			if (handle.Operation.Location == null)
				return;
			DateTime startTime;
			if (_usedTimeDic.TryGetValue(handle.Operation.Location.RelativePath, out startTime))
			{
				var useTime = (int)(DateTime.Now - startTime).TotalMilliseconds;
				AssetLog.Log($"[资源加载耗时] {handle.Operation.Location.RelativePath} : {useTime}ms");
				_usedTimeDic.Remove(handle.Operation.Location.RelativePath);	//展示完移除
			}
		}
	}
}