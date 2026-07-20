using System;
using System.Collections.Generic;

namespace XKAsset
{
	public class ProvideOperation<T> : AsyncOperationBase<T>, IProviderOperation
	{
		private IResourceProvider _provider;
		private AsyncOperationHandle<IList<AsyncOperationHandle>> _DepOp;
		public Type RequestType => typeof(T);
		private Func<bool> _waitForCompletionCallback;

		public void Init(IResourceLocation loc, IResourceProvider provider, AsyncOperationHandle<IList<AsyncOperationHandle>> deps)
		{
			Location = loc;
			Key = Location.HashCode;
			_provider = provider;
			_DepOp = deps;
		}

		protected override void Execute()
		{
			_provider.Provide(this);
		}

		protected override bool OnWaitForCompltion()
		{
			if (IsDone)
				return true;
			if(!_DepOp.IsDone)
				_DepOp.WaitForCompletion();
			if (_waitForCompletionCallback == null)
				return true;
			return _waitForCompletionCallback.Invoke();;
		}

		public object GetFirstDependencies()
		{
			if (!_DepOp.IsValid() || !_DepOp.IsDone)
				return null;
			return _DepOp.Result?[0].Result;
		}

		public void ProvideComplete(object res, bool success, string msg)
		{
			Complete((T)res, success, msg);
		}

		public void SetWaitForCompletionCallback(Func<bool> cb)
		{
			_waitForCompletionCallback = cb;
		}

		protected override void Destroy()
		{
			if (_provider == null)
				return;
			_provider.Release(Result);
			if(_DepOp.IsValid())
				_DepOp.Release();
			Result = default;
		}
	}
}