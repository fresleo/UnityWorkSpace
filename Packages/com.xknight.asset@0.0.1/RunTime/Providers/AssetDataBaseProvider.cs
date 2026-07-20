#if UNITY_EDITOR

using System.Threading.Tasks;
using UnityEditor;

namespace XKAsset
{
	public class AssetDataBaseProvider : ResourceProviderBase
	{
		private float _delayTime = 0.1f;
		public AssetDataBaseProvider(float delay = 0.05f)
		{
			_delayTime = delay;
		}
		
		public override void Provide(IProviderOperation op)
		{
			new InternalOp().Start(op, _delayTime);
		}
		
		class InternalOp
		{
			private IProviderOperation _op;
			private bool _loadFinished;
			
			public void Start(IProviderOperation op, float delayTime)
			{
				_op = op;
				_loadFinished = false;
				_op.SetWaitForCompletionCallback(OnWaitComplete);
				if(delayTime <= 0)
					LoadImmediate();
				else
					DelayLoad.Add(LoadImmediate, delayTime);	
			}

			private bool OnWaitComplete()
			{
				LoadImmediate();
				return true;
			}

			private void LoadImmediate()
			{
				if (_loadFinished)
					return;
				_loadFinished = true;
				var obj = AssetDatabase.LoadAssetAtPath(_op.Location.AbsPath, _op.Location.ResourceType);
				_op.ProvideComplete(obj, obj!=null, _op.Location.AbsPath);
			}
		}
	}
}
#endif