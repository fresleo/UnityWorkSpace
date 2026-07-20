using System.Collections.Generic;

namespace XKAsset
{
	/// <summary>
	/// 时间段资源缓存
	/// </summary>
	public class TimeSlotAssetCache : AssetCacheBase
	{
		private List<string> _loadAssetList;
		
		protected override void OnInit()
		{
			_loadAssetList = new List<string>();
		}

		protected override bool IsOpen()
		{
			return AssetCacheSwitch.ACC_OPEN_TIMESLOT_ASSET;
		}

		protected override void OnStartLoad(string key)
		{
			if (key.EndsWith(".ab"))
				return;
			_loadAssetList.Add(key.ToLower());
			AssetLog.Log($"[时间段资源] 加载：{key}");
		}

		protected override void OnRelease(string key)
		{
			_loadAssetList.Remove(key);
			AssetLog.Log($"[时间段资源] 卸载：{key}");
		}

		public List<string> GetUnloadAsset()
		{
			var res = new List<string>();
			res.AddRange(_loadAssetList);
			_loadAssetList.Clear();
			return res;
		}
	}
}