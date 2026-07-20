#if UNITY_EDITOR

using UnityEngine;

namespace XKT.ShaderVariantLogger
{
    /// <summary>
    /// 着色器变体记录器
    /// </summary>
    public class ShaderVariantLoggerBehaviour : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Init()
        {
            bool state = ShaderVariantLoggerInterface.GetEnable();
            if (state)
            {
                var gmo = new GameObject("ShaderVariantLogger");
                gmo.AddComponent<ShaderVariantLoggerBehaviour>();
                DontDestroyOnLoad(gmo);
            }
        }

        private void OnDestroy()
        {
            ShaderVariantLoggerInterface.SetEnable(false);
        }
        
        private void Update()
        {
            ShaderVariantLoggerInterface.SetFrame(Time.frameCount);
        }
    }
}

#endif // UNITY_EDITOR
