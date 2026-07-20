using UnityEngine;

namespace XKnight.XLOD
{
    [ExecuteInEditMode]
    public class MeshHolder : EffectHolder
    {
        Renderer mr = null;
        public Renderer MR
        {
            get
            {
                if (mr == null)
                    mr = GetComponent<Renderer>();
                return mr;
            }
        }

#if UNITY_EDITOR
        protected override void EditorInit()
        {
            mr = MR;
            if (mr != null)
            {
                highConfig.enable = mr.enabled;
                mediumConfig.enable = mr.enabled;
                lowConfig.enable = mr.enabled; 
            }
        }
#endif

        // protected override void SetLODByConfig(EffectConfig config)
        // {
        //     mr = MR;
        //     if (mr != null)
        //     {
        //         mr.enabled = config.enable;
        //     }
        // }
    }
}
