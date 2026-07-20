using UnityEngine;

namespace XKnight.XLOD
{
    [ExecuteInEditMode]
    public class TrailHolder : EffectHolder
    {
        TrailRenderer trail = null;
        public TrailRenderer Trail
        {
            get
            {
                if (trail == null)
                    trail = GetComponent<TrailRenderer>();
                return trail;
            }
        }

#if UNITY_EDITOR
        protected override void EditorInit()
        {
            trail = Trail;
            highConfig.enable = trail.enabled;
            mediumConfig.enable = trail.enabled;
            lowConfig.enable = trail.enabled;
        }
#endif

        // protected override void SetLODByConfig(EffectConfig config)
        // {
        //     trail = Trail;
        //     trail.enabled = config.enable;
        // }
    }
}