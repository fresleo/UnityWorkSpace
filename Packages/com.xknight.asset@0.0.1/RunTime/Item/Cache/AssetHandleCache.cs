using System.Collections.Generic;

namespace XKAsset
{
	public class AssetHandleCache : AssetCacheBase
	{
		private Dictionary<object, AsyncOperationHandle> _assetToHandleDic;

		protected override void OnInit()
		{
			_assetToHandleDic = new Dictionary<object, AsyncOperationHandle>();
		}

		protected override void OnLoaded(AsyncOperationHandle handle)
		{
			if (handle.Operation.Location == null || handle.Result == null)
				return;	//只记录有定位器的资源
			_assetToHandleDic.TryAdd(handle.Result, handle);
		}

		protected override void OnDestroy(AsyncOperationHandle handle)
		{
			if (handle.Operation.Location == null || handle.Result == null)
				return;
			_assetToHandleDic.Remove(handle.Result);
		}
		
		public bool GetHandle(object key, out AsyncOperationHandle handle)
		{
			return _assetToHandleDic.TryGetValue(key, out handle);
		}
	}
}