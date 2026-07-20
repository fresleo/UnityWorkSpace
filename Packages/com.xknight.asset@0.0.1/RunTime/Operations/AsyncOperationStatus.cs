namespace XKAsset
{
	public enum AsyncOperationStatus
	{
		/// <summary>
		/// 初始状态
		/// </summary>
		STATUS_NONE,
		
		/// <summary>
		/// 加载中
		/// </summary>
		STATUS_RUNNING,
		
		/// <summary>
		/// 成功
		/// </summary>
		STATUS_SUCCESS,
		
		/// <summary>
		/// 失败
		/// </summary>
		STATUS_FAILED,
	}
}