using UnityEngine;
using UnityEngine.U2D;
namespace XKAsset
{
	public class AtlasAssetProvider : ResourceProviderBase
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
				_bundleOp = bundle.LoadAllAssetsAsync();
				_bundleOp.completed += (obj) =>
				{
					var assets = ((AssetBundleRequest)obj).allAssets;
					if (assets != null)
					{
						Sprite sp = null;
						for (int i = 0; i < assets.Length; i++)
						{
							if (assets[i].name == op.Location.ResourceName && assets[i] is Sprite sprite)
							{
								sp = sprite;
								break;
							}

							if (assets[i] is SpriteAtlas atlas)
							{
								sp = atlas.GetSprite(op.Location.ResourceName);
								if (sp != null)
								{
									break;
								}
							}
						}
						CompleteLoad(sp);
					}
					else
					{
						Sprite sp = null;
						CompleteLoad(sp);
					}
					
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

		public override void Release<T>(T result)
		{
		}
	}
}