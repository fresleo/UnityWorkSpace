using UnityEngine.Networking;

namespace XKAsset
{
	public class BytesAssetProvider : ResourceProviderBase
	{
		class InternalOp
		{
			private IProviderOperation _operation;
			public void Start(IProviderOperation op)
			{
				_operation = op;
				string path = op.Location.AbsUrl;
				var request = UnityWebRequest.Get(path);
				request.SendWebRequest().completed+= (obj)=>
				{
					Complete(request.downloadHandler.data);
				};
			}

			private void Complete(byte[] res)
			{
				if (res == null || res.Length <= 0)
					_operation.ProvideComplete(null, false, "");
				else
					_operation.ProvideComplete(res, true, "");
			}
		}
		public override void Provide(IProviderOperation op)
		{
			new InternalOp().Start(op);
		}
	}
}