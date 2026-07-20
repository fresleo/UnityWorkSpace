// Created By: WangYu  Date: 2024-06-07

using UnityEngine.Events;

namespace com.xknight.mt.Lib.Runtime.MT.OcclusionCulling
{
    public partial class DynamicOcclusionCuller
    {
        private void UpdateActions()
        {
            foreach (var item in m_updateActionList)
            {
                item?.Invoke();
            }
        }
        
        public void AddUpdateAction(UnityAction action)
        {
            if (!m_updateActionList.Contains(action))
            {
                m_updateActionList.Add(action);
            }
        }

        public void RemoveUpdateAction(UnityAction action)
        {
            m_updateActionList.Remove(action);
        }
    }
}