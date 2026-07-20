using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

namespace XKAsset
{
	public class SceneOperation : AsyncOperationBase<SceneInstance>, IUpdatable
	{
		private bool m_ActivateOnLoad;
        private SceneInstance m_Inst;
        private IResourceLocation m_Location;
        private LoadSceneMode m_LoadMode;
        private int m_Priority;
        private AsyncOperationHandle<IList<AsyncOperationHandle>> m_DepOp;
        private AssetManager _assetMgr;

        public SceneOperation(AssetManager assetMgr)
        {
            _assetMgr = assetMgr;
        }
        

        public void Init(IResourceLocation location, LoadSceneMode loadMode, bool activateOnLoad, int priority, AsyncOperationHandle<IList<AsyncOperationHandle>> depOp)
        {
            m_DepOp = depOp;
            if (m_DepOp.IsValid())
                m_DepOp.Acquire();

            m_Location = location;
            m_LoadMode = loadMode;
            m_ActivateOnLoad = activateOnLoad;
            m_Priority = priority;
        }
        
        protected override bool OnWaitForCompltion()
        {
            if (m_DepOp.IsValid() && !m_DepOp.IsDone)
                m_DepOp.WaitForCompletion();

            while (!IsDone)
            {
                if (m_Inst.m_Operation.allowSceneActivation && Mathf.Approximately(m_Inst.m_Operation.progress, .9f))
                {
                    Result = m_Inst;
                    return true;
                }
            }
            
            return IsDone;
        }
        
        protected override void Execute()
        {
            var loadingFromBundle = false;
            if (m_DepOp.IsValid())
            {
                foreach (var d in m_DepOp.Result)
                {
                    var abResource = d.Result as AssetBundle;
                    if (abResource != null)
                        loadingFromBundle = true;
                }
            }
            
            if (!m_DepOp.IsValid() || m_DepOp.IsDone)
            {
                m_Inst = InternalLoadScene(m_Location, loadingFromBundle, m_LoadMode, m_ActivateOnLoad, m_Priority);
                if (!IsDone)
                    _assetMgr.AddUpdate(this);
            }
            else
            {
                _assetMgr.RemoveUpdate(this);
                Complete(m_Inst, false, null);
            }
        }

        internal SceneInstance InternalLoadScene(IResourceLocation location, bool loadingFromBundle, LoadSceneMode loadMode, bool activateOnLoad, int priority)
        {
            var op = InternalLoad(location, loadingFromBundle, loadMode);
            op.allowSceneActivation = activateOnLoad;
            op.priority = priority;
            return new SceneInstance() { m_Operation = op, Scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1) };
        }

        AsyncOperation InternalLoad(IResourceLocation loc, bool loadingFromBundle, LoadSceneMode mode)
        {
#if !UNITY_EDITOR
            return SceneManager.LoadSceneAsync(loc.ResourceName, new LoadSceneParameters() { loadSceneMode = mode });
#else
            if (loadingFromBundle)
                return SceneManager.LoadSceneAsync(loc.ResourceName, new LoadSceneParameters() { loadSceneMode = mode });
            else
            {
                string path = loc.AbsPath;
                if (!path.ToLower().StartsWith("assets/") && !path.ToLower().StartsWith("packages/"))
                    path = "Assets/" + path;
                if (path.LastIndexOf(".unity") == -1)
                    path += ".unity";

                return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters() { loadSceneMode = mode });
            }
#endif
        }

        protected override void Destroy()
        {
            if (m_DepOp.IsValid())
                m_DepOp.Release();
            base.Destroy();
        }

        public void Update(float unscaledDeltaTime)
        {
            if (m_Inst.m_Operation != null)
            {
                if (m_Inst.m_Operation.isDone || (!m_Inst.m_Operation.allowSceneActivation && m_Inst.m_Operation.progress >= .9f))
                {
                    _assetMgr.RemoveUpdate(this);
                    Complete(m_Inst, true, null);
                }
            }
        }
	}
}