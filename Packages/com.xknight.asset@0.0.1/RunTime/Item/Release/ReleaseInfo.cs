namespace XKAsset
{
	/// <summary>
	/// 待释放资源结构
	/// </summary>
	public class ReleaseInfo
	{
		public AsyncOperationHandle handle;
		private float _releaseTime;

		public void Init(AsyncOperationHandle asset, float time)
		{
			handle = asset;
			_releaseTime = time;
		}

		public void Update(float time)
		{
			_releaseTime -= time;
		}

		public void Refresh(float time)
		{
			_releaseTime = time;
		}

		public bool CanRelease()
		{
			return _releaseTime <= 0;
		}
	}
}