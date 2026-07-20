using UnityEngine;

namespace XKnight.XLOD
{
    public enum EffectQuality
    {
        LOW = 0,
        MEDIUM = 1,
        HIGH = 2,
    }

    [ExecuteInEditMode]
    public class EffectHost : MonoBehaviour, ILODHost
    {
        EffectHolder[] holders = null;

        void Awake()
        {
            LODManager.Inst.effectLOD.Register(this);
            SetLOD(LODManager.Inst.effectLOD.GetLOD());
        }

        void OnDestroy()
        {
            LODManager.Inst.effectLOD.Unregister(this);
        }

        public void SetLOD(int lv)
        {
            GetHolders();
            if (holders == null)
            {
                return;
            } 

            for (int i = 0; i < holders.Length; i++)
            {
                holders[i].SetLOD(lv);
            }
        }

        void GetHolders()
        {
            // 编辑器下为方便制作，每次重新获取
#if UNITY_EDITOR
            holders = GetComponentsInChildren<EffectHolder>(true);
#else
            if (holders == null)
            {
                holders = GetComponentsInChildren<EffectHolder>(true);
            }
#endif
        }

#if UNITY_EDITOR
        public bool CheckHolder(int[] enableCounts)
        {
            GetHolders();
            if (holders == null || holders.Length == 0)
            {
                return false;
            }

            int highCount = 0;
            int mediumCount = 0;
            int lowCount = 0;
            for (int i = 0, imax = holders.Length; i < imax; ++i)
            {
                var holder = holders[i];
                if (holder.highConfig.enable)
                {
                    highCount++;
                }
                if (holder.mediumConfig.enable)
                {
                    mediumCount++;
                }
                if (holder.lowConfig.enable)
                {
                    lowCount++;
                }
            }
            enableCounts[0] = highCount;
            enableCounts[1] = mediumCount;
            enableCounts[2] = lowCount;

            return true;
        }

        // 所有子Particle组件增加EffectHolder，仅编辑器使用
        public void AddEffectHolderForAllChildren()
        {
            ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();
            for (int i = 0, imax = particles.Length; i < imax; i++)
            {
                if (particles[i].GetComponent<ParticleHolder>() == null)
                    particles[i].gameObject.AddComponent<ParticleHolder>();
            }

            TrailRenderer[] trails = GetComponentsInChildren<TrailRenderer>();
            for (int i = 0, imax = trails.Length; i < imax; i++)
            {
                if (trails[i].GetComponent<TrailHolder>() == null)
                    trails[i].gameObject.AddComponent<TrailHolder>();
            }

            MeshRenderer[] meshs = GetComponentsInChildren<MeshRenderer>();
            for (int i = 0, imax = meshs.Length; i < imax; i++)
            {
                if (meshs[i].GetComponent<MeshHolder>() == null)
                    meshs[i].gameObject.AddComponent<MeshHolder>();
            }
            
            SkinnedMeshRenderer[] smeshs = GetComponentsInChildren<SkinnedMeshRenderer>();
            for (int i = 0, imax = smeshs.Length; i < imax; i++)
            {
                if (smeshs[i].GetComponent<MeshHolder>() == null)
                    smeshs[i].gameObject.AddComponent<MeshHolder>();
            }
        }

        // 移除所有EffectHolder组件，仅编辑器使用
        public void RemoveEffectHolderInAllChildren()
        {
            GetHolders();
            if (holders == null)
            {
                return;
            }

            for (int i = 0, imax = holders.Length; i < imax; i++)
            {
                DestroyImmediate(holders[i]);
            }
        }

        // 播放特效，控制所有子Particle播放的接口，仅编辑器下使用
        public void PlayEffect()
        {
            ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();
            for (int i = 0, imax = particles.Length; i < imax; i++)
            {
                particles[i].Play();
            }
            Animation[] anims = GetComponentsInChildren<Animation>();
            for (int i = 0, imax = anims.Length; i < imax; i++)
            {
                anims[i].Play();
            }
        }

        // 停止特效，控制所有子Particle播放的接口，仅编辑器下使用
        public void StopEffect()
        {
            ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();
            for (int i = 0, imax = particles.Length; i < imax; i++)
            {
                particles[i].Stop();
            }
            Animation[] anims = GetComponentsInChildren<Animation>();
            for (int i = 0, imax = anims.Length; i < imax; i++)
            {
                anims[i].Stop();
            }
        }
#endif
    }
}
