
using System.Collections.Generic;
using UnityEngine;

namespace XKAsset
{
	public class SpritePathCache : AssetCacheBase
	{
		//sprite路径存储
		private List<string> _spritePathList;
		
		protected override void OnInit()
		{
			_spritePathList = new List<string>();
		}

		protected override bool IsOpen()
		{
			return false;
		}

		protected override void OnStartLoad(string key)
		{
			if (IsSaveSpritePath())
			{
				if (key.EndsWith(".png"))
				{
					SaveSpritePath(key);
				}
			}
		}

		public List<string> GetUsedSprtePathList()
		{
			return _spritePathList;
		}
		
		private bool IsSaveSpritePath()
		{
			var saveSpritePath = PlayerPrefs.GetInt("SaveSpritePath");
			return saveSpritePath != 0;
		}
		
		private void SaveSpritePath(string spritePath)
		{
			if (_spritePathList == null)
				_spritePathList = new List<string>();
			if (!_spritePathList.Contains(spritePath))
			{
				_spritePathList.Add(spritePath);
			}
		}
	}
}