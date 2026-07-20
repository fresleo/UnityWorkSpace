// Created By: WangYu  Date: 2023-10-15

namespace com.xknight.mt.Lib.Runtime.MT.Log
{
    /// <summary>
    /// 超大地形的日志记录器
    /// </summary>
    public static class MTLogger
    {
        public static void Log(object message)
        {
            MTLoggerProvider.Log(message);
        }

        public static void LogWarning(object message)
        {
            MTLoggerProvider.LogWarning(message);
        }
        
        public static void LogError(object message)
        {
            MTLoggerProvider.LogError(message);
        }
    }
}