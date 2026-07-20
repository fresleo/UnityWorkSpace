using System;
using System.Collections;
using System.Collections.Generic;
using ShaderHotSwap.Util;
using UnityEngine;
using UnityEngine.Networking;

namespace ShaderHotSwap
{
    public static class ShaderHotSwapClient
    {
        private static string s_logHeader = $"[{nameof(ShaderHotSwapClient)}]";
        
        public static void QueryEnv(string urlPrefix, Action<string, string> done)
        {
            EditorCoroutine.Start(PostImpl(urlPrefix + HandlerQueryEnv.c_url, "{}", done));
        }

        public static void SwapShaders(string urlPrefix, string reqContent, Action<string, string> done)
        {
            EditorCoroutine.Start(PostImpl(urlPrefix + HandlerSwapShaders.c_url, reqContent, done));
        }
        
        private static IEnumerator PostImpl(string url, string reqContent, System.Action<string, string> done)
        {
            var www = UnityWebRequest.PostWwwForm(url, reqContent);
            UnityWebRequestAsyncOperation op = www.SendWebRequest();
            yield return new EditorCoroutine.CustomYieldInstruction(() => op.isDone);

            if (www.isNetworkError)
            {
                done(null, $"{s_logHeader} 网络错误: {www.error}");
                yield break;
            }
            else if (www.isHttpError)
            {
                done(null, $"{s_logHeader} Http 错误: {www.responseCode}, {www.error}");
                yield break;
            }

            while (!www.downloadHandler.isDone)
            {
                yield return null;
            }

            MemoryLogger.Log($"{s_logHeader} Code: {www.responseCode}, Res: {www.downloadHandler.text}");
            done(www.downloadHandler.text, null);
        }
    }
}