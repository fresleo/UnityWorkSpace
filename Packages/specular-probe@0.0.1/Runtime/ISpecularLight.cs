// Created By: WangYu  Date: 2024-05-07

namespace SpecularProbe
{
    public interface ISpecularLight
    {
        /// <summary>
        /// 隐藏高光
        /// </summary>
        void HideSpecular();
        
#if UNITY_EDITOR
        
        /// <summary>
        /// 准备烘焙
        /// </summary>
        void ReadyToBake();
        
#endif //UNITY_EDITOR
    }
}