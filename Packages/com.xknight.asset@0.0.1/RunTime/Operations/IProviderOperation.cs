using System;
using System.Collections.Generic;

namespace XKAsset
{
	public interface IProviderOperation
	{
		/// <summary>
		/// 资源加载信息
		/// </summary>
		IResourceLocation Location { get; }
		
		/// <summary>
		/// 资源类型
		/// </summary>
		Type RequestType { get; }

		/// <summary>
		/// 初始化
		/// </summary>
		/// <param name="loc">位置信息</param>
		/// <param name="provider">加载器</param>
		/// <param name="deps">依赖项</param>
		void Init(IResourceLocation loc, IResourceProvider provider,
						AsyncOperationHandle<IList<AsyncOperationHandle>> deps);
		
		/// <summary>
		/// 获取第一个依赖(bundle)
		/// </summary>
		public object GetFirstDependencies();

		/// <summary>
		/// 加载完成
		/// </summary>
		/// <param name="res">资源对象</param>
		/// <param name="success">是否成功</param>
		/// <param name="msg"></param>
		void ProvideComplete(object res, bool success, string msg);

		/// <summary>
		/// 等待同步结束
		/// </summary>
		void SetWaitForCompletionCallback(Func<bool> cb);
	}
}