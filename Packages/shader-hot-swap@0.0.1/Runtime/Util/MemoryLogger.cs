using System.Text;
using UnityEngine;

namespace ShaderHotSwap.Util
{
    /// <summary>
    /// 可记忆的日志
    /// </summary>
    public static class MemoryLogger
    {
        private static StringBuilder s_sb = new();

        public static string Flush()
        {
            var str = s_sb.ToString();
            s_sb.Length = 0;
            return str;
        }

        private static void LogImpl(LogType type, string msg)
        {
            s_sb.AppendFormat("[{0}] {1}\n", type, msg);
        }

        public static void Log(string msg)
        {
            LogImpl(LogType.Log, msg);
        }

        public static void LogWarning(string msg)
        {
            LogImpl(LogType.Warning, msg);
        }
        
        public static void LogError(string msg)
        {
            LogImpl(LogType.Error, msg);
        }
    }
}