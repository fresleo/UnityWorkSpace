using UnityEngine;
using UnityEngine.SceneManagement;

namespace XKAsset
{
	public struct SceneInstance
	{
		private Scene _Scene;
		public AsyncOperation m_Operation;

		public Scene Scene
		{
			get
			{
				return this._Scene;
			}
			set
			{
				this._Scene = value;
			}
		}

		/// <summary>
		/// 加载场景是否成功
		/// </summary>
		public bool IsSuccess()
		{
			return _Scene.IsValid();
		}

		public AsyncOperation ActivateAsync()
		{
			this.m_Operation.allowSceneActivation = true;
			return this.m_Operation;
		}

		public override int GetHashCode()
		{
			return this.Scene.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return obj is SceneInstance sceneInstance && this.Scene.Equals((object) sceneInstance.Scene);
		}
	}
}