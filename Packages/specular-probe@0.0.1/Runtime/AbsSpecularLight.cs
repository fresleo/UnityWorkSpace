// Created By: WangYu  Date: 2024-05-07

using UnityEngine;

namespace SpecularProbe
{
    public abstract class AbsSpecularLight : MonoBehaviour, ISpecularLight
    {
        public abstract void HideSpecular();
        
#if UNITY_EDITOR
        
        public abstract void ReadyToBake();
        
#endif //UNITY_EDITOR
    }
}