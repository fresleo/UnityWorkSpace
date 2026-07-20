using UnityEngine;

namespace XKnight.XLOD
{
    [ExecuteInEditMode]
    public class ParticleHolder : EffectHolder
    {
        ParticleSystem particle = null;
        public ParticleSystem Particle
        {
            get
            {
                if (particle == null)
                    particle = GetComponent<ParticleSystem>();
                return particle;
            }
        }

#if UNITY_EDITOR
        protected override void EditorInit()
        {
            particle = Particle;
            ParticleSystem.EmissionModule emission = particle.emission;
            ParticleSystem.MainModule main = particle.main;
            highConfig.enable = emission.enabled;
            mediumConfig.enable = emission.enabled;
            lowConfig.enable = emission.enabled;
        }
#endif

        // protected override void SetLODByConfig(EffectConfig config)
        // {
        //     particle = Particle;
        //     ParticleSystem.EmissionModule emission = particle.emission;
        //     ParticleSystem.MainModule main = particle.main;
        //
        //     emission.enabled = config.enable;
        //
        //     if (!config.enable)
        //     {
        //         Stop();
        //     }
        // }

        public void Play()
        {
            particle = Particle;
            gameObject.SetActive(true);
            particle.Play(false);
        }

        void Stop()
        {
            particle = Particle;
            particle.Stop(false);
            particle.Clear(false);
        }
    }
}