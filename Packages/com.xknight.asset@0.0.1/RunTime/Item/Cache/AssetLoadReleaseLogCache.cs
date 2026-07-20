namespace XKAsset
{
	public class AssetLoadReleaseLogCache : AssetCacheBase
	{
		protected override bool IsOpen()
		{
			return AssetCacheSwitch.ACC_OPEN_LOAD_RELEASE_LOG;
		}

		protected override void OnStartLoad(string key)
		{
			if (key.EndsWith(".ab"))
				return;
			AssetLog.Log($"[资源] 开始加载资源: {key}");
		}

		protected override void OnLoaded(AsyncOperationHandle handle)
		{
			if (handle.Operation?.Location == null || handle.Operation.Location.RelativePath.EndsWith(".ab"))
				return;
			AssetLog.Log($"[资源] 加载完成 {handle.Operation.Location.RelativePath} ：{handle.Result != null}");
		}

		protected override void OnRelease(string key)
		{
			AssetLog.Log($"[资源] 资源卸载: {key}");
		}
	}
}