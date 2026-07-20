using UnityEngine;
using UnityEditor;

namespace XKnight.XLOD
{
    [CustomEditor(typeof(ParticleHolder))]
    [CanEditMultipleObjects]
    public class ParticleHolderEditor : Editor
    {
        ParticleHolder script;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            script = target as ParticleHolder;
            if (GUILayout.Button("高质量"))
            {
                script.SetLOD((int)EffectQuality.HIGH);
                script.Play();
            }
            if (GUILayout.Button("中质量"))
            {
                script.SetLOD((int)EffectQuality.MEDIUM);
                script.Play();
            }
            if (GUILayout.Button("低质量"))
            {
                script.SetLOD((int)EffectQuality.LOW);
                script.Play();
            }
        }
    }
}
