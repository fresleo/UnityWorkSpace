// Created By: WangYu  Date: 2022-01-30

using System;

namespace com.xknight.mt.Lib.Runtime.MT.Utils
{
    /// <summary>
    /// [Flags]枚举工具
    /// 枚举值要以2的2次幂的方式增长
    /// 如： 0, 1, 2, 4, 8, 16
    /// 或： 0, 1 << 0, 1 << 1, 1 << 2, 1 << 3, 1 << 4
    /// </summary>
    public static class FlagsUtil<TFlags>
        where TFlags : Enum
    {
        private static Type s_flagsType;
        
        public static TFlags Add(TFlags raw, TFlags input)
        {
            int rawNum = Convert.ToInt32(raw);
            int inputNum = Convert.ToInt32(input);

            if ((rawNum & inputNum) <= 0)
            {
                rawNum |= inputNum;
            }

            if (s_flagsType == null)
            {
                s_flagsType = typeof(TFlags);
            }
            
            TFlags flags = (TFlags)Enum.ToObject(s_flagsType, rawNum);
            return flags;
        }
        
        public static TFlags Remove(TFlags raw, TFlags input)
        {
            int rawNum = Convert.ToInt32(raw);
            int inputNum = Convert.ToInt32(input);

            if ((rawNum & inputNum) > 0)
            {
                rawNum &= ~inputNum;
            }

            if (s_flagsType == null)
            {
                s_flagsType = typeof(TFlags);
            }

            TFlags flags = (TFlags)Enum.ToObject(s_flagsType, rawNum);
            return flags;
        }
        
        public static bool Has(TFlags raw, TFlags input)
        {
            int rawNum = Convert.ToInt32(raw);
            int inputNum = Convert.ToInt32(input);

            return (rawNum & inputNum) == inputNum;
        }
        
    }
}