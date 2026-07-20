using System.Collections.Generic;

namespace XKAsset
{
	public class GroupOperation : AsyncOperationBase<IList<AsyncOperationHandle>>
	{
		private IList<AsyncOperationHandle> Result;
		private int _completedCnt;
		public void Init(IList<AsyncOperationHandle> ops, int hashId)
		{
			Key = hashId;
			_completedCnt = 0;
			Result = ops;
		}
		protected override void Execute()
		{
			foreach (var opItem in Result)
			{
				if (opItem.IsDone)
					_completedCnt++;
				else
					opItem.Completed += (obj) => OnOperationCompleted();
			}

			OnCheckAllCompleted();
		}

		protected override void Destroy()
		{
			for (int i = 0; i < Result.Count; i++)
				if (Result[i].IsValid())
					Result[i].Release();
			Result.Clear();
		}

		/// <summary>
		/// 单个依赖operation执行完回调
		/// </summary>
		private void OnOperationCompleted()
		{
			_completedCnt++;
			OnCheckAllCompleted();
		}

		private void OnCheckAllCompleted()
		{
			if (_completedCnt == Result.Count)
			{
				Complete(Result, true, null);
			}
		}

		protected override bool OnWaitForCompltion()
		{
			if (IsDone || Result == null || Result.Count == 0)
				return true;
			for (int i = 0, nCnt = Result.Count; i < nCnt; i++)
			{
				if (!Result[i].IsDone)
					Result[i].WaitForCompletion();
			}

			return IsDone;
		}
	}
}