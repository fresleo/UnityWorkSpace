using System;

namespace XKAsset
{
	/// <summary>
	/// 桥接
	/// </summary>
	public struct AsyncOperationHandle
	{
		public IAsyncOperation Operation { get; private set; }
		
		public bool IsDone
		{
			get { return Operation == null || Operation.IsDone; }
		}

		public AsyncOperationStatus Status
		{
			get
			{
				return Operation == null ? AsyncOperationStatus.STATUS_FAILED : Operation.Status;
			}
		}

		public bool IsValid()
		{
			return Operation != null;
		}

		public AsyncOperationHandle(IAsyncOperation op)
		{
			Operation = op;
		}
		
		public event Action<AsyncOperationHandle> Completed
		{
			add
			{
				Operation.InterfaceCompleted += value;
			}
			remove
			{
				Operation.InterfaceCompleted -= value;
			}
		}
		
		public event Action<AsyncOperationHandle> Destroyed
		{
			add
			{
				Operation.InterfaceDestroyed += value;
			}
			remove
			{
				Operation.InterfaceDestroyed -= value;
			}
		}

		public object Result
		{
			get
			{
				return Operation == null ? null : Operation.GetResultAsObject();
			}
		}

		public AsyncOperationHandle<T> Convert<T>()
		{
			return new AsyncOperationHandle<T>(Operation);
		}
		
		public void WaitForCompletion()
		{
			if (Operation == null)
				return;
			Operation.WaitForCompletion();
		}

		internal void Release()
		{
			if (Operation == null)
				return;
			Operation.DecrementReferenceCount();
		}
	}

	public struct AsyncOperationHandle<T>
	{
		/// <summary>
		/// 结果
		/// </summary>
		public T Result
		{
			get { return Operation==null ? default: Operation.Result; }
		}

		/// <summary>
		/// 加载行为
		/// </summary>
		public AsyncOperationBase<T> Operation { get; private set; }
		
		public bool IsValid()
		{
			return Operation != null;
		}
		
		/// <summary>
		/// 是否完成
		/// </summary>
		public bool IsDone
		{
			get { return Operation == null || Operation.IsDone; }
		}
		
		public AsyncOperationHandle(IAsyncOperation op)
		{
			Operation = (AsyncOperationBase<T>)op;
		}

		/// <summary>
		/// 非模板类转换
		/// </summary>
		public static implicit operator AsyncOperationHandle(AsyncOperationHandle<T> handle)
		{
			return new AsyncOperationHandle(handle.Operation);
		}
		
		/// <summary>
		/// Operation完成回调
		/// </summary>
		public event Action<T> Completed
		{
			add
			{
				Operation.Completed += value;
			}
			remove
			{
				Operation.Completed -= value;
			}
		}
		
		/// <summary>
		/// Operation完成回调
		/// </summary>
		public event Action<AsyncOperationHandle> TypelessCompleted
		{
			add
			{
				Operation.InterfaceCompleted += value;
			}
			remove
			{
				Operation.InterfaceCompleted -= value;
			}
		}
		
		public event Action<AsyncOperationHandle> Destroyed
		{
			add { Operation.InterfaceCompleted += value; }
			remove { Operation.InterfaceCompleted -= value; }
		}

		public int ReferenceCount
		{
			get
			{
				
				return Operation == null ? 0 :Operation.ReferenceCount;
			}
		}

		public void Acquire()
		{
			if (Operation == null)
				return;
			Operation.IncrementReferenceCount();
			
		}

		/// <summary>
		/// 同步等待加载完成
		/// </summary>
		public void WaitForCompletion()
		{
			if (Operation == null)
				return;
			Operation.WaitForCompletion();
		}

		/// <summary>
		///  释放
		/// </summary>
		internal void Release()
		{
			if (Operation == null)
				return;
			Operation.DecrementReferenceCount();
		}
	}
}