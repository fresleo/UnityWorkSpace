// Created By: WangYu  Date: 2024-06-06

using System.Collections.Generic;
using UnityEngine.Events;

namespace com.xknight.mt.Lib.Runtime.MT.OcclusionCulling
{
    public class EventManager
    {
        private static EventManager s_mgr = new();
        private EventManager() {}

        public static EventManager GetInstance => s_mgr;
        
        private Dictionary<string, UnityEvent> m_dictStringKey = new();
        private Dictionary<int, UnityEvent> m_dictIntKey = new();

        public void Clear()
        {
            foreach (var iter in m_dictStringKey)
            {
                iter.Value.RemoveAllListeners();
            }
            m_dictStringKey.Clear();

            foreach (var iter in m_dictIntKey)
            {
                iter.Value.RemoveAllListeners();
            }
            m_dictIntKey.Clear();
        }
        
        public void StartListening(string eventKey, UnityAction listener)
        {
            if(s_mgr == null) return;
            
            UnityEvent thisEvent;
            if (m_dictStringKey.TryGetValue(eventKey, out thisEvent))
            {
                thisEvent.AddListener(listener);
            }
            else
            {
                thisEvent = new UnityEvent();
                thisEvent.AddListener(listener);
                
                m_dictStringKey.Add(eventKey, thisEvent);
            }
        }

        public void StartListening(int eventKey, UnityAction listener)
        {
            if(s_mgr == null) return;
            
            UnityEvent thisEvent;
            if (m_dictIntKey.TryGetValue(eventKey, out thisEvent))
            {
                thisEvent.AddListener(listener);
            }
            else
            {
                thisEvent = new UnityEvent();
                thisEvent.AddListener(listener);
                
                m_dictIntKey.Add(eventKey, thisEvent);
            }
        }

        public void StopListening(string eventKey, UnityAction listener)
        {
            if (s_mgr == null) return;
            
            UnityEvent thisEvent;
            if (m_dictStringKey.TryGetValue(eventKey, out thisEvent))
            {
                thisEvent.RemoveListener(listener);
            }
        }
        
        public void StopListening(int eventKey, UnityAction listener)
        {
            if (s_mgr == null) return;
            
            UnityEvent thisEvent;
            if (m_dictIntKey.TryGetValue(eventKey, out thisEvent))
            {
                thisEvent.RemoveListener(listener);
            }
        }

        public void TriggerEvent(string eventKey)
        {
            if(s_mgr == null) return;
            
            UnityEvent thisEvent;
            if (m_dictStringKey.TryGetValue(eventKey, out thisEvent))
            {
                thisEvent.Invoke();
            }
        }
        
        public void TriggerEvent(int eventKey)
        {
            if(s_mgr == null) return;
            
            UnityEvent thisEvent;
            if (m_dictIntKey.TryGetValue(eventKey, out thisEvent))
            {
                thisEvent.Invoke();
            }
        }
    }
}