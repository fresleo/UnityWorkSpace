#if UNITY_EDITOR
namespace XKAsset
{
	public class EditorAssetMgrInitOp : AssetMgrInitOpBase
	{
		private string _rootPath;

		public EditorAssetMgrInitOp(string path)
		{
			_rootPath = path;
		}
		
		protected override void OnInitLocator(AssetManager mgr)
		{
			mgr.AddLoactor(new AssetDataBaseLocator(_rootPath));
			mgr.HasInited = true;
		}

		protected override void OnInitProvider(AssetManager mgr)
		{
			mgr.AddProvider(new AssetDataBaseProvider());
		}
	}
}
#endif