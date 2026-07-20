namespace XKAsset
{
	public class CompleteOperation<T> : AsyncOperationBase<T>
	{
		bool _success;
		string _exception;

		private CompleteOperation(){}
		
		public void Init(bool success, string exception)
		{
			Result = default;
			_success = success;
			_exception = exception;
		}

		protected override void Execute()
		{
			Complete(Result, _success, _exception);
		}

		public static CompleteOperation<T> Create(string exception)
		{
			var op = new CompleteOperation<T>();
			op.Init(false, exception);
			op.Start(default);
			return op;
		}
	}
}