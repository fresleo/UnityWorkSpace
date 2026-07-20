using System.Collections.Generic;
using UnityEngine;

namespace XKAsset
{
	public class AssetBundleProvider : ResourceProviderBase
	{
		class InternalOp
		{
			/// <summary>
			/// 加载行为
			/// </summary>
			private IProviderOperation _providerOp;

			private AssetBundleCreateRequest _bundleOp;

			/// <summary>
			/// 开始加载
			/// </summary>
			public void Start(IProviderOperation op)
			{
				_providerOp = op;
				_providerOp.SetWaitForCompletionCallback(InvokeWaitComplete);
				
				_bundleOp = AssetBundle.LoadFromFileAsync(_providerOp.Location.AbsPath);
				_bundleOp.completed += (obj) =>
				{
					var bundle = ((AssetBundleCreateRequest)obj)?.assetBundle;
					CompleteLoad(bundle);
				};
			}
			
			private void CompleteLoad(AssetBundle bundle)
			{
				if (bundle != null)
					_providerOp.ProvideComplete(bundle, true, null);
				else
					_providerOp.ProvideComplete(null, false, $"加载bundle出错.{_providerOp.Location.RelativePath}");
			}

			private bool InvokeWaitComplete()
			{
				if (_bundleOp == null)
					return true;
				var temp = _bundleOp.assetBundle;
				if (_bundleOp.isDone)
				{
					CompleteLoad(_bundleOp.assetBundle);
				}
				return _bundleOp.isDone;
			}
		}

		public override void Provide(IProviderOperation op)
		{
			new InternalOp().Start(op);
		}

		public override void Release<T>(T result)
		{
			if (result is AssetBundle bundle)
			{
				bundle.Unload(true);
			}
		}
	}
}