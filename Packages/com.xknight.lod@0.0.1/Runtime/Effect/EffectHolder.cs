using UnityEngine;

namespace XKnight.XLOD
{
    public abstract class EffectHolder : MonoBehaviour
    {
        [System.Serializable]
        public class EffectConfig
        {
            public bool enable;
        }

        public EffectConfig highConfig = new EffectConfig();
        public EffectConfig mediumConfig = new EffectConfig();
        public EffectConfig lowConfig = new EffectConfig();

#if UNITY_EDITOR
        [HideInInspector] public bool alreadyInit = false;

        void Awake()
        {
            if (!alreadyInit)
            {
                EditorInit();

                alreadyInit = true;
            }

        }

        protected abstract void EditorInit();
#endif

        public void SetLOD(int lv)
        {
            EffectConfig currentConfig = highConfig;
            switch (lv)
            {
                case (int)EffectQuality.MEDIUM:
                    currentConfig = mediumConfig;
                    break;
                case (int)EffectQuality.LOW:
                    currentConfig = lowConfig;
                    break;
            }
            gameObject.SetActive(currentConfig.enable);
            SetLODByConfig(currentConfig);
        }

        protected virtual void SetLODByConfig(EffectConfig config) {}
    }
}
