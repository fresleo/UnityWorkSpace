using System;
using UnityEngine;
using UnityEngine.U2D;

namespace XKAsset
{
	public delegate bool CheckAssetUpdateState(string path);
	public static class AssetLoadGlobalConfig
	{
		/// <summary>
		/// 编辑器下资源父路径
		/// </summary>
		public static string AssetParentPath = "Assets/";
		
#if UNITY_EDITOR
		public static string UseBundle = "UseBundle";
#endif

		public static CheckAssetUpdateState checkUpdateState;
		
		public static string StreamingAssetsURL
		{
			get
			{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
				return "file://" + Application.dataPath + "/StreamingAssets/";
#elif UNITY_ANDROID
		        return "jar:file://" + Application.dataPath + "!/assets/";
#elif UNITY_IPHONE
		        return "file://" + Application.dataPath + "/Raw/";
#else
                return "file://" + Application.dataPath + "/StreamingAssets/";
#endif
			}
		}
		
		public static string PersistentURL
		{
			get
			{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
				return "file://" + Application.dataPath + "/UpdateAssets/";
#elif UNITY_ANDROID
		        return "jar:file://" + Application.persistentDataPath + "/";
#elif UNITY_IPHONE
		        return "file://" + Application.persistentDataPath + "/Raw/";
#else
                return "file://" + Application.persistentDataPath + "/";
#endif
			}
		}

		public static string StreamingAssetsPath
		{
			get
			{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
				return Application.streamingAssetsPath + "/";
#elif UNITY_ANDROID
		        return "jar:file://" + Application.dataPath + "!/assets/";
#elif UNITY_IPHONE
		        return Application.dataPath + "/Raw/";
#else
                return Application.dataPath + "/StreamingAssets/";
#endif
			}
		}
		
		public static string PersistentPath
		{
			get
			{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
				return Application.dataPath + "/UpdateAssets/";
#elif UNITY_ANDROID
		        return Application.persistentDataPath + "/";
#elif UNITY_IPHONE
		        return Application.persistentDataPath + "/Raw/";
#else
                return Application.persistentDataPath + "/";
#endif
			}
		}

		public static string GetAssetUrl(string bundlePath)
		{
			string parentDir = StreamingAssetsURL;
			var tempBundle = "bundles/" + bundlePath;
			if (checkUpdateState != null && checkUpdateState.Invoke(tempBundle))
			{
				parentDir = PersistentURL;
			}
			
			if (IsUseBundle())
			{
				return parentDir + tempBundle;
			}
			else
			{
				return "file://" + Application.dataPath + "/" + bundlePath;
			}
		}
		
		public static string GetAssetPath(string bundlePath)
		{
			string parentDir = StreamingAssetsPath;
			var tempBundle = "bundles/" + bundlePath;
			if (checkUpdateState != null && checkUpdateState.Invoke(tempBundle))
			{
				parentDir = PersistentPath;
			}
			
			if (IsUseBundle())
			{
				return parentDir + tempBundle;
			}
			else
			{
				return  "Assets/" + bundlePath;
			}
		}

		public static string GetAssetName(string path)
		{
			int folderIndex = path.LastIndexOf('/') + 1;
			int endIndex = path.LastIndexOf('.');
			endIndex = endIndex < 0 ? path.Length : endIndex;
			return path.Substring(folderIndex, endIndex-folderIndex);
		}

		public static bool IsUseBundle()
		{
#if UNITY_EDITOR
			var useBundle = PlayerPrefs.GetInt(UseBundle);
			return useBundle != 0;
#else
			return true;
#endif
		}
		
		/// <summary>
        /// 返回资源类型对应的Type实例
        /// </summary>
        /// <param name="assetType"></param>
        /// <returns></returns>
        public static Type ParseType(AssetType assetType)
        {
            switch (assetType)
            {
                case AssetType.GameObject:
                    return typeof(GameObject);
                case AssetType.AnimationClip:
                    return typeof(AnimationClip);
                case AssetType.Audio:
                    return typeof(AudioClip);
                case AssetType.Texture2d:
                    return typeof(Texture2D);
                case AssetType.Material:
                    return typeof(Material);
                case AssetType.TextAsset:
                    return typeof(TextAsset);
                case AssetType.AnimationController:
                    return typeof(RuntimeAnimatorController);
                case AssetType.Scene:
                    return typeof(UnityEngine.SceneManagement.Scene);
                case AssetType.TextureCube:
                    return typeof(Cubemap);
                case AssetType.Other:
                    return typeof(UnityEngine.Object);
                case AssetType.UGUIAtlas:
                    return typeof(SpriteAtlas);
                case AssetType.Playable:
                    return typeof(UnityEngine.Playables.PlayableAsset);
                case AssetType.Font:
                    return typeof(UnityEngine.Font);
                case AssetType.Video:
                    return typeof(UnityEngine.Video.VideoClip);
				case AssetType.Sprite:
                    return typeof(UnityEngine.Sprite);
				default:
                    Debug.LogWarningFormat("AssetManager: ParseType: Unrecognized asset type: {0}", assetType.ToString());
                    return typeof(UnityEngine.Object);
            }
        }
	}
}