using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Net;
using ShaderHotSwap.Util;

namespace ShaderHotSwap
{
    public class ServerHttpJsonPost : MonoBehaviour
    {
        private static string s_logHeader = $"[{nameof(ServerHttpJsonPost)}]";
        
        /// <summary>
        /// 处理器方法，返回 jsonResponse
        /// </summary>
        public delegate string Handler(string jsonRequest);

        /// <summary>
        /// 监听端口
        /// </summary>
        public short listenPort = 8090;
        
        private Dictionary<string, Handler> m_handlers = new();
        private Queue<Action> m_mainThreadJobs = new();
        private HttpListener m_httpListener;
        
        /// <summary>
        /// 添加处理器
        /// </summary>
        public void AddHandler(string urlPath, Handler handler)
        {
            m_handlers[urlPath] = handler;
        }

        /// <summary>
        /// 移除处理器
        /// </summary>
        public void RemoveHandler(string urlPath)
        {
            m_handlers.Remove(urlPath);
        }

        private void OnDestroy()
        {
            if (m_httpListener != null)
            {
                m_httpListener.Close();
                m_httpListener = null;

                MemoryLogger.Log($"{s_logHeader} 停止监听 {listenPort} ...");
            }
        }
        
        private void Start()
        {
            m_httpListener = new HttpListener();
            m_httpListener.Prefixes.Add($"http://*:{listenPort}/");

            m_httpListener.Start();
            m_httpListener.BeginGetContext(new AsyncCallback(ListenerCallback), m_httpListener);

            MemoryLogger.Log($"{s_logHeader} 监听 {listenPort} ...");
        }
        
        private void Update()
        {
            TryRunAMainThreadJob(); // 1帧1个job
        }

        private void TryRunAMainThreadJob()
        {
            Action job = null;
            if (m_mainThreadJobs.Count > 0)
            {
                lock (m_mainThreadJobs)
                {
                    if (m_mainThreadJobs.Count > 0)
                    {
                        job = m_mainThreadJobs.Dequeue();
                    }
                }
            }
            job?.Invoke();
        }

        private void SendMainThreadJob(Action job)
        {
            lock (m_mainThreadJobs)
            {
                m_mainThreadJobs.Enqueue(job);
            }
        }
        
        private void ListenerCallback(IAsyncResult result)
        {
            HttpListener listener = (HttpListener)result.AsyncState;
            HttpListenerContext context = listener.EndGetContext(result);

            HttpListenerRequest req = context.Request;
            HttpListenerResponse res = context.Response;

            MemoryLogger.Log($"{s_logHeader} req.Url.AbsolutePath = " + req.Url.AbsolutePath);

            Handler handler;
            if (m_handlers.TryGetValue(req.Url.AbsolutePath, out handler))
            {
                HandleRequest(req, res, handler);
            }
            else
            {
                HandleError(req, res);
            }

            m_httpListener.BeginGetContext(new AsyncCallback(ListenerCallback), m_httpListener);
        }

        private void HandleRequest(HttpListenerRequest req, HttpListenerResponse res, Handler handler)
        {
            string jsonRequest = new System.IO.StreamReader(req.InputStream).ReadToEnd();
            jsonRequest = WWW.UnEscapeURL(jsonRequest);
            
            SendMainThreadJob(() =>
            {
                string jsonResponse = handler(jsonRequest);

                MemoryLogger.Log($"{s_logHeader} url:{req.Url.AbsolutePath}, res:{jsonResponse}");

                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(jsonResponse);
                res.ContentLength64 = buffer.Length;
                res.OutputStream.Write(buffer, 0, buffer.Length);
                res.Close();
            });
        }

        private void HandleError(HttpListenerRequest req, HttpListenerResponse res)
        {
            res.StatusCode = 404;
            res.Close();
        }
    }
}