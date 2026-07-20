// Created By: WangYu  Date: 2025-04-10

using System;
using UnityEngine;

namespace XKT.TOD.Tag
{
    [AddComponentMenu("TOD/Tag/ActiveTag")]
    public class ActiveTag : AbsTodTag
    {
        private bool m_recorded;
        private bool m_rawState;

        private void RecordRawData()
        {
            if(m_recorded) return;
            m_recorded = true;
            
            m_rawState = gameObject.activeSelf;
        }
        
        public void SetState(bool state)
        {
            RecordRawData();
            
            gameObject.SetActive(state);
        }

        public void ResetState()
        {
            if(!m_recorded) return;
            
            gameObject.SetActive(m_rawState);
        }
    }
}