using UnityEngine;
using UnityEngine.SceneManagement;

namespace XKAsset
{
	public class UnloadSceneOperation : AsyncOperationBase<SceneInstance>
	{
		SceneInstance m_Instance;
		AsyncOperationHandle<SceneInstance> m_sceneLoadHandle;
		UnloadSceneOptions m_UnloadOptions;
		public void Init(AsyncOperationHandle<SceneInstance> sceneLoadHandle, UnloadSceneOptions options)
		{
			if (sceneLoadHandle.ReferenceCount > 0)
			{
				m_sceneLoadHandle = sceneLoadHandle;
				m_Instance = m_sceneLoadHandle.Result;
			}
			m_UnloadOptions = options;
		}

		protected override void Execute()
		{
			if (m_sceneLoadHandle.IsValid() && m_Instance.Scene.isLoaded)
			{
				var unloadOp = SceneManager.UnloadSceneAsync(m_Instance.Scene, m_UnloadOptions);
				if (unloadOp == null)
					UnloadSceneCompletedNoRelease(null);
				else
					unloadOp.completed += UnloadSceneCompletedNoRelease;
			}
			else
				UnloadSceneCompleted(null);
		}

		private void UnloadSceneCompleted(AsyncOperation obj)
		{
			UnloadSceneCompleted(true, "unload scene is not exist.");
		}
            
		private void UnloadSceneCompletedNoRelease(AsyncOperation obj)
		{
			UnloadSceneCompleted(true, "");
		}

		private void UnloadSceneCompleted(bool unloaded, string msg)
		{
// #if UNITY_EDITOR
// 			if (unloaded)
// 				Resources.UnloadUnusedAssets();
// #endif
			Complete(m_Instance, unloaded, msg);
			if (m_sceneLoadHandle.IsValid())
				m_sceneLoadHandle.Release();
		}
	}
}