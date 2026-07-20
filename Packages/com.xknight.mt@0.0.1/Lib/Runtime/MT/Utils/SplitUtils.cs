// Created By: WangYu  Date: 2024-06-01

using System.Collections.Generic;

namespace com.xknight.mt.Lib.Runtime.MT.Utils
{
    /// <summary>
    /// 拆分工具
    /// </summary>
    public static class SplitUtils
    {
        /// <summary>
        /// 拆分数组
        /// </summary>
        public static TType[] SplitArray<TType>(TType[] initial, int maxElements, out TType[] remaining)
        {
            if (initial.Length > maxElements)
            {
                var ret = new TType[maxElements];
                int remainingCount = initial.Length - maxElements;

                remaining = null;
                if (remainingCount > 0)
                {
                    remaining = new TType[initial.Length - maxElements];
                }

                for (int i = 0; i < maxElements; i++)
                {
                    ret[i] = initial[i];
                }

                if (remaining != null)
                {
                    for (int i = maxElements; i < initial.Length; i++)
                    {
                        remaining[i - maxElements] = initial[i];
                    }
                }

                return ret;
            }

            remaining = null;
            return initial;
        }

        /// <summary>
        /// 拆分数组
        /// </summary>
        public static void SplitArray<TType>(int maxElements, TType[] initialList, List<TType[]> groupList)
        {
            int initialCounter = initialList.Length;
            int listIndex = 0;
            int arrayIndex = 0;
            
            // 尚有未进组的
            while (initialCounter > 0)
            {
                // 确保有足够的组
                if (listIndex >= groupList.Count)
                {
                    groupList.Add(new TType[maxElements]);
                }
                
                // 元素进组
                for (int i = 0, j = arrayIndex; i < maxElements; i++, j++)
                {
                    if (j < initialList.Length)
                    {
                        groupList[listIndex][i] = initialList[j];
                    }
                    else
                    {
                        groupList[listIndex][i] = default;
                    }
                }
                
                initialCounter -= maxElements;
                listIndex++;
                arrayIndex += maxElements;
            }

            // 用剩下的组，都给它重置为默认值
            int startIndex = arrayIndex / maxElements;
            for (int i = startIndex; i < groupList.Count; i++)
            {
                for (int j = 0; j < maxElements; j++)
                {
                    groupList[i][j] = default;
                }
            }
        }
        
    }
}