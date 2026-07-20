// Created By: WangYu  Date: 2022-10-02

namespace com.xknight.mt.Lib.Editor.MT.Jobs
{
    public interface IMTJob
    {
        /// <summary>
        /// 完成
        /// </summary>
        bool IsDone { get; }
        
        /// <summary>
        /// 进度
        /// </summary>
        float Progress { get; }

        /// <summary>
        /// 更新
        /// </summary>
        void Update();
        
    }
}