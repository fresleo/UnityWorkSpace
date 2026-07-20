using System;
using ShaderHotSwap.Protocol;
using ShaderHotSwap.Util;
using UnityEngine;

namespace ShaderHotSwap
{
    /// <summary>
    /// 查询环境处理器
    /// </summary>
    public static class HandlerQueryEnv
    {
        private static string s_logHeader = $"[{nameof(HandlerQueryEnv)}]";

        public const string c_url = "/queryEnv";
        
        public static string HandlerMain(string jsonRequest)
        {
            try
            {
                var res = new QueryEnvRes();
                res.platform = Application.platform.ToString();
                string resJson = JsonUtility.ToJson(res);
                
                MemoryLogger.Log($"{s_logHeader} 平台: {resJson}");
                return resJson;
            }
            catch(Exception ex)
            {
                return ErrorString(ex.ToString());
            }
        }

        static string ErrorString(string reason)
        {
            var res = new QueryEnvRes();
            res.error = reason;
            string resJson = JsonUtility.ToJson(res);

            return resJson;
        }
    }
}