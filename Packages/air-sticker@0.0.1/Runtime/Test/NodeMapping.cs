// Created By: WangYu  Date: 2025-02-20

using System;
using UnityEngine;

namespace AirSticker.Runtime.Test
{
    public class NodeMapping : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_receiverObject;
        
        private Collider m_collider;
        
        private void Start()
        {
            m_collider = GetComponent<Collider>();
        }

        public GameObject GetReceiverObject()
        {
            if (m_collider == null)
            {
                Debug.LogError("节点上没有碰撞器");
                return null;
            }

            return m_receiverObject;
        }
        
    }
}