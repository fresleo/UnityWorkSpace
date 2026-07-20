// Created By: WangYu  Date: 2024-01-11

using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.Log
{
    public static class MTLoggerProvider
    {
        public static IMTLogger provider = null;
        
        public static void Log(object message)
        {
            if (provider != null)
            {
                provider.Log(message);
            }
            else
            {
                Debug.Log(message);
            }
        }

        public static void LogWarning(object message)
        {
            if (provider != null)
            {
                provider.LogWarning(message);
            }
            else
            {
                Debug.LogWarning(message);
            }
        }

        public static void LogError(object message)
        {
            if (provider != null)
            {
                provider.LogError(message);
            }
            else
            {
                Debug.LogError(message);
            }
        }
    }
}