using System.Collections.Generic;
using UnityEngine.Pool;

namespace XKAsset
{
	internal class ReleaseKit
	{
		private Dictionary<int, ReleaseInfo> _dicWaitRelease;	//等待释放的资源
        private const float RELEASE_TIME = 2f;					//释放间隔时间
        private List<int> _releaseList;
        private ObjectPool<ReleaseInfo> _releasePool;

        public void Release(AsyncOperationHandle handle)
        {
        	AddReleaseList(handle);
        }

        /// <summary>
        /// 立即释放
        /// </summary>
        /// <param name="handle"></param>
        internal void ReleaseImmediate(AsyncOperationHandle handle)
        {
        	handle.Release();
        }

        /// <summary>
        /// 初始化释放用参数
        /// </summary>
        private void InitRelease()
        {
        	_dicWaitRelease = new Dictionary<int, ReleaseInfo>();
        	_releaseList = new List<int>();
        	_releasePool = new ObjectPool<ReleaseInfo>(()=>new ReleaseInfo() );
        }

        /// <summary>
        /// 添加到待释放列表中
        /// </summary>
        /// <param name="handle"></param>
        private void AddReleaseList(AsyncOperationHandle handle)
        {
        	if (_dicWaitRelease.TryGetValue(handle.Operation.Key, out var asset))
        	{
        		asset.Refresh(RELEASE_TIME);
        	}
        	else
        	{
        		var info = _releasePool.Get();
        		info.Init(handle, RELEASE_TIME);
        		_dicWaitRelease.Add(handle.Operation.Key, info);
        	}
        }

        /// <summary>
        /// 异步释放
        /// </summary>
        /// <param name="deltaTime"></param>
        private void ReleaseUpdate(float deltaTime)
        {
        	foreach (var item in _dicWaitRelease)
        	{
        		item.Value.Update(deltaTime);
        		if (item.Value.CanRelease())
        		{
        			item.Value.handle.Release();
        			_releasePool.Release(item.Value);
        			_releaseList.Add(item.Key);
        		}
        	}

        	for (int i = 0; i < _releaseList.Count; i++)
        	{
        		_dicWaitRelease.Remove(_releaseList[i]);
        	}
        }
	}
}