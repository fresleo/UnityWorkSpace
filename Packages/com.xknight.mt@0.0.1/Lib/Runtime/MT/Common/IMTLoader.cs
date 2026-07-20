// Created By: WangYu  Date: 2024-01-31

using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.Common
{
    public interface IMTLoader
    {
        /// <summary>
        /// 设置剔除相机
        /// </summary>
        void SetCullCamera(Camera camera);

        /// <summary>
        /// 停止
        /// </summary>
        void Stop();
        
        /// <summary>
        /// 暂停
        /// </summary>
        void Pause();
        
        /// <summary>
        /// 播放
        /// </summary>
        void Play();
        
        /// <summary>
        /// 在编辑器中显示
        /// </summary>
        void DisplayInEditor(int lodLv);
    }
}