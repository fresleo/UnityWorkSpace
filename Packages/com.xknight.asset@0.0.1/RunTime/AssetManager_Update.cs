using System.Collections.Generic;

namespace XKAsset
{
	public partial class AssetManager
	{
		private List<IUpdatable> _updateOps = new List<IUpdatable>();
		private List<IUpdatable> _remvoeUpdateOps = new List<IUpdatable>();

		internal void AddUpdate(IUpdatable item)
		{
			if (!_updateOps.Contains(item))
			{
				_updateOps.Add(item);
			}
		}
		
		internal void RemoveUpdate(IUpdatable item)
		{
			if (_updateOps.Contains(item))
			{
				_remvoeUpdateOps.Add(item);
			}
		}

		public void Update(float deltaTime)
		{
			foreach (var item in _updateOps)
			{
				item.Update(deltaTime);
			}

			for (int i = 0, nCnt = _remvoeUpdateOps.Count; i < nCnt; i++)
			{
				_updateOps.Remove(_remvoeUpdateOps[i]);
			}
			_remvoeUpdateOps.Clear();
		}
	}
}