using UnityEngine;

namespace ShaderHotSwap
{
    /// <summary>
    /// Shader 热切换服务
    /// </summary>
    [RequireComponent(typeof(ServerHttpJsonPost))]
    public class ServerShaderHotSwap : MonoBehaviour
    {
        private const string c_serverNodeName = "ShaderHotSwap";
        
        public static void CreateServer(Transform scriptContainer)
        {
            var node = scriptContainer.Find(c_serverNodeName);
            
            if (node == null)
            {
                node = new GameObject(c_serverNodeName).transform;
                node.SetParent(scriptContainer, false);
            }

            var script = node.GetComponent<ServerShaderHotSwap>();
            if (script == null)
            {
                script = node.gameObject.AddComponent<ServerShaderHotSwap>();
            }
        }

        public static void ReleaseServer(Transform scriptContainer)
        {
            var node = scriptContainer.Find(c_serverNodeName);
            if(node == null) return;
            
            Destroy(node.gameObject);
        }
        
        
        private ServerHttpJsonPost m_httpServer;

        private void OnDestroy()
        {
            m_httpServer.RemoveHandler(HandlerQueryEnv.c_url);
            m_httpServer.RemoveHandler(HandlerSwapShaders.c_url);
        }
        
        private void Start()
        {
            DontDestroyOnLoad(gameObject);

            m_httpServer = GetComponent<ServerHttpJsonPost>();
            m_httpServer.AddHandler(HandlerQueryEnv.c_url, HandlerQueryEnv.HandlerMain);
            m_httpServer.AddHandler(HandlerSwapShaders.c_url, HandlerSwapShaders.HandlerMain);
        }
        
    }
}