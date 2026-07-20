// Created By: WangYu  Date: 2023-12-01

namespace com.xknight.mt.Lib.Runtime.MT.Common
{
    public interface IQuadTreeBuildNode
    {
        /// <summary>
        /// 清理
        /// </summary>
        void Clear();
        
        /// <summary>
        /// 清理空的树干节点
        /// </summary>
        /// <returns>true=当前节点可以被安全的删除</returns>
        bool ClearEmpty();
    }
}