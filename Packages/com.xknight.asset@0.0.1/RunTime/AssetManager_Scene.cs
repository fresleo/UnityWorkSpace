using System.Collections.Generic;
using System.Resources;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace XKAsset
{
	public partial class AssetManager
	{
		private Dictionary<string, AsyncOperationHandle> _loadedScene;
		
		public AsyncOperationHandle<SceneInstance> LoadSceneAsync(string key, LoadSceneMode mode, bool activateLoaded)
		{
			if(mode == LoadSceneMode.Single)
				TryReleaseActiveScene();
			IResourceLocation loc = GetResourceLocation(key, typeof(SceneInstance));
			if (loc != null)
			{
				return ProvideScene(loc, mode, activateLoaded, 0);
			}

			return default;
		}

		private void TryReleaseActiveScene()
		{
			var scene = SceneManager.GetActiveScene();
			ReleaseScene(scene.name);
		}
		
		private AsyncOperationHandle<SceneInstance> ProvideScene(IResourceLocation location, LoadSceneMode loadMode, bool activateOnLoad, int priority)
		{
			AsyncOperationHandle<IList<AsyncOperationHandle>> depOp = default;
			if (location.HasDeps)
				depOp = ProvideResourceGroupCached(location.Dependencies, typeof(AssetBundle), location.DepsHashCode);

			SceneOperation op = new SceneOperation(this);
			op.Init(location, loadMode, activateOnLoad, priority, depOp);

			StartOperation(op, depOp);

			if (depOp.IsValid())
				depOp.Release();
			var handle = new AsyncOperationHandle<SceneInstance>(op);
			handle.TypelessCompleted += OnSceneLoaded;
			return handle;
		}

		public AsyncOperationHandle<SceneInstance> ReleaseScene(string sceneName)
		{
			AsyncOperationHandle handle;
			if(_loadedScene.TryGetValue(sceneName, out handle))
			{
				return ReleaseScene(handle.Convert<SceneInstance>());
			}

			return default;
		}
		
		private AsyncOperationHandle<SceneInstance> ReleaseScene(AsyncOperationHandle<SceneInstance> sceneLoadHandle)
		{
			var unloadOp = new UnloadSceneOperation();
			unloadOp.Init(sceneLoadHandle, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
			StartOperation(unloadOp, sceneLoadHandle);
			var handle = new AsyncOperationHandle<SceneInstance>(unloadOp);
			handle.TypelessCompleted += OnUnSceneLoaded;
			return handle;
		}

		private void OnSceneLoaded(AsyncOperationHandle handle)
		{
			var sceneInstance = (SceneInstance)handle.Result;
			if (string.IsNullOrEmpty(sceneInstance.Scene.name))
			{
				Debug.LogError("load scene error. scene name is null");
				return;
			}
			
			if (_loadedScene.ContainsKey(sceneInstance.Scene.name))
				_loadedScene.Remove(sceneInstance.Scene.name);		//因为single的scene不走卸载接口的话同样可以卸载
			_loadedScene.TryAdd(sceneInstance.Scene.name, handle);
		}
		
		private void OnUnSceneLoaded(AsyncOperationHandle handle)
		{
			var sceneInstance = (SceneInstance)handle.Result;
			if (sceneInstance.Scene.IsValid())
			{
				_loadedScene.Remove(sceneInstance.Scene.name);
			}
			else
			{
				List<string> keys = new List<string>();
				foreach (var scene in _loadedScene)
				{
					if (scene.Value.Result is SceneInstance temp)
					{
						if (!temp.Scene.IsValid())
						{
							keys.Add(scene.Key);
						}
					}
				}

				foreach (var item in keys)
				{
					_loadedScene.Remove(item);
				}
			}
		}

		/// <summary>
		/// 通过name获取已经加载的scene
		/// </summary>
		/// <param name="sceneName"></param>
		/// <returns></returns>
		public SceneInstance GetSceneInstanceWithName(string sceneName)
		{
			if (_loadedScene.TryGetValue(sceneName, out AsyncOperationHandle handle))
			{
				return (SceneInstance)handle.Result;
			}
			else
			{
				Debug.LogWarning("can not find loaded scene with name " + sceneName);
				return default;
			}
		}
	}
}