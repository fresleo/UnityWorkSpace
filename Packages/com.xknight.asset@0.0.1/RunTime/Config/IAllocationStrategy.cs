namespace XKAsset
{
	public interface IAllocationStrategy
	{
		object New(System.Type type, int typeHash);

		void Release(int typeHash, object obj);
	}
}