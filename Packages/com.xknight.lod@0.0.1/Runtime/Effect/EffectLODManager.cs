using System.Collections.Generic;

namespace XKnight.XLOD
{
    public class EffectLODManager
    {
        int lodLv = (int)EffectQuality.HIGH;

        List<ILODHost> hosts = new List<ILODHost>();

        public void Register(ILODHost host)
        {
            hosts.Add(host);
        }

        public void Unregister(ILODHost host)
        {
            hosts.Remove(host);
        }

        public int GetLOD()
        {
            return lodLv;
        }

        public void SetLOD(int lv)
        {
            if (lodLv == lv)
            {
                return;
            }

            lodLv = lv;

            for (int i = 0, imax = hosts.Count; i < imax; ++i)
            {
                hosts[i].SetLOD(lodLv);
            }
        }
    }
}