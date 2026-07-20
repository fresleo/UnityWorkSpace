namespace XKAsset
{
	public abstract class ResourceProviderBase : IResourceProvider
	{
		public string ProvideId
		{
			get
			{
				return GetType().FullName;
			}
		}

		public virtual void Provide(IProviderOperation op)
		{
			
		}
		
		public virtual bool CanProvide()
		{
			return true;
		}
		
		public virtual void Release<T>(T result)
		{
			
		}
	}
}