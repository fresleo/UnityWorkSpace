// Created By: WangYu  Date: 2024-01-11

namespace com.xknight.mt.Lib.Runtime.MT.Log
{
    public interface IMTLogger
    {
        void Log(object message);
        void LogWarning(object message);
        void LogError(object message);
    }
}