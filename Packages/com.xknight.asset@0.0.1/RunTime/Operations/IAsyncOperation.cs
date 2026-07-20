
using System;
using System.Collections.Generic;

namespace XKAsset
{
	public interface IAsyncOperation
	{
		/// <summary>
		/// ID
		/// </summary>
		int Key { get; }
		/// <summary>
		/// 减少计数
		/// </summary>
		void DecrementReferenceCount();
		
		/// <summary>
		/// 增加计数
		/// </summary>
		void IncrementReferenceCount();
		
		/// <summary>
		/// 引用计数
		/// </summary>
		int ReferenceCount { get; }
		
		/// <summary>
		/// 是否完成
		/// </summary>
		bool IsDone { get; }
		
		/// <summary>
		/// 获取结果
		/// </summary>
		/// <returns></returns>
		object GetResultAsObject();

		/// <summary>
		/// 当前状态
		/// </summary>
		AsyncOperationStatus Status { get; }
		
		/// <summary>
		/// 数据
		/// </summary>
		IResourceLocation Location { get; }

		/// <summary>
		/// 获取依赖
		/// </summary>
		IList<IResourceLocation> GetDependencies();

		/// <summary>
		/// 开始执行行为逻辑
		/// </summary>
		void Start(AsyncOperationHandle deps);

		/// <summary>
		/// 完成接口
		/// </summary>
		event Action<AsyncOperationHandle> InterfaceCompleted;
		
		/// <summary>
		/// 销毁接口
		/// </summary>
		event Action<AsyncOperationHandle> InterfaceDestroyed;
		
		/// <summary>
		/// 同步调用接口
		/// </summary>
		void WaitForCompletion();
	}
}