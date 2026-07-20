using UnityEngine;

namespace XKAsset
{
	internal class AssetLog
	{
		public static void LogError(string strLog)
		{
			//if(!AssetLoadGlobalConfig.IsUseBundle())
			Debug.LogError(strLog);
		}

		public static void Log(string strLog)
		{
			Debug.Log(strLog);
		}
	}
}