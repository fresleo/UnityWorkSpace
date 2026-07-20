
using System;
using System.Collections.Generic;
using UnityEngine;

namespace XKAsset
{
	public abstract class AsyncOperationBase<T> : IAsyncOperation
	{
		/// <summary>
		/// 结果
		/// </summary>
		public T Result { get; set; }
		
		public int ReferenceCount { get; private set; }
		public IResourceLocation Location { get; protected set; }

		public AsyncOperationStatus Status { get; private set; }
		public bool IsDone => Status == AsyncOperationStatus.STATUS_FAILED || Status == AsyncOperationStatus.STATUS_SUCCESS;
		public int Key { get; protected set; }

		/// <summary>
		/// 执行完成回调
		/// </summary>
		private DelegateList<T> _completedAction;
		
		/// <summary>
		/// 执行完成回调
		/// </summary>
		private DelegateList<AsyncOperationHandle> _destroyedAction;
		
		public event Action<AsyncOperationHandle> InterfaceCompleted
		{
			add
			{
				Completed += o => value(new AsyncOperationHandle(this));
			}
			remove
			{
				Completed -= o => value(new AsyncOperationHandle(this));
			}
		}

		public event Action<T> Completed
		{
			add
			{
				if (_completedAction == null)
				{
					_completedAction = DelegateList<T>.CreateWithGlobalCache();
				}
				_completedAction.Add(value);
				if (IsDone)
				{
					InvokeCompleted();
				}
			}
			remove
			{
				_completedAction?.Remove(value);
			}
		}
		
		public event Action<AsyncOperationHandle> InterfaceDestroyed
		{
			add
			{
				if (_destroyedAction == null)
				{
					_destroyedAction = DelegateList<AsyncOperationHandle>.CreateWithGlobalCache();
				}
				_destroyedAction.Add(value);
			}
			remove
			{
				_destroyedAction?.Remove(value);
			}
		}

		public object GetResultAsObject()
		{
			return Result;
		}

		public IList<IResourceLocation> GetDependencies()
		{
			return Location.Dependencies;
		}
		public void DecrementReferenceCount()
		{
			if (ReferenceCount <= 0)
			{
				//TODO 抛出错误信息
				return;
			}
			ReferenceCount--;
			if (ReferenceCount == 0)
			{
				if (_destroyedAction != null)
				{
					_destroyedAction.Invoke(new AsyncOperationHandle(this));
					_destroyedAction.Clear();
				}

				Destroy();
				Result = default;
				//Status = AsyncOperationStatus.STATUS_NONE;
				ReferenceCount = 1;
			}
		}

		public void IncrementReferenceCount()
		{
			ReferenceCount++;
		}

		public void Start(AsyncOperationHandle deps)
		{
			Status = AsyncOperationStatus.STATUS_RUNNING;
			IncrementReferenceCount();
			if (deps.IsValid() && !deps.IsDone)
				deps.Completed += (obj) => InvokeExecute();
			else
				InvokeExecute();
		}

		public void Complete(T result, bool success, string msg)
		{
			if (!success)
			{//输出错误信息
				//Debug.LogError($"资源加载失败：{msg}");
			}
			Result = result;
			Status = success ? AsyncOperationStatus.STATUS_SUCCESS : AsyncOperationStatus.STATUS_FAILED;
			InvokeCompleted();
		}

		private void InvokeExecute()
		{
			Execute();
		}

		private void InvokeCompleted()
		{
			if (_completedAction != null)
			{
				_completedAction.Invoke(this.Result);
				_completedAction.Clear();
			}
		}

		/// <summary>
		/// 同步等待接口
		/// </summary>
		public void WaitForCompletion()
		{
			while (!OnWaitForCompltion()) { }
		}

		/// <summary>
		/// 同步等待接口
		/// </summary>
		/// <returns></returns>
		protected virtual bool OnWaitForCompltion()
		{
			return true;
		}

		protected virtual void Destroy() { }

		protected abstract void Execute();
	}
}