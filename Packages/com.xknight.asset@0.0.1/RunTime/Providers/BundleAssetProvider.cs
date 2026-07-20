using UnityEngine;

namespace XKAsset
{
	public class BundleAssetProvider : ResourceProviderBase
	{
		class InternalOp
		{
			private IProviderOperation _operation;
			private AssetBundleRequest _bundleOp;
			
			public void Start(IProviderOperation op)
			{
				_operation = op;
				_operation.SetWaitForCompletionCallback(InvokeWaitComplete);
				var bundle = (AssetBundle)op.GetFirstDependencies();
				if (bundle == null)
				{
					op.ProvideComplete(null, false, $"加载资源失败。资源依赖bundle未正确加载{op.Location.RelativePath}");
					return;
				}

				_bundleOp = bundle.LoadAssetAsync(op.Location.RelativePath, op.RequestType);
				_bundleOp.completed += (obj) =>
				{
					var asset = ((AssetBundleRequest) obj).asset;
					CompleteLoad(asset);
				};
			}

			private void CompleteLoad<T>(T asset)
			{
				if(asset != null)
					_operation.ProvideComplete(asset, true,null);
				else
					_operation.ProvideComplete(null, false, $"加载资源失败。bundle中不存在此资源");
			}
			
			private bool InvokeWaitComplete()
			{
				if (_bundleOp == null)
					return true;
				if (_bundleOp.isDone)
					return true;
				return _bundleOp.asset != null;
			}
		}
		public override void Provide(IProviderOperation op)
		{
			new InternalOp().Start(op);
		}
	}
}