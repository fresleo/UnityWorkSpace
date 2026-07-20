using System;
using System.IO;
using GraphProcessor;
using UnityEditor;
using UnityEngine;

namespace GameEditor.NodeFlow
{  
    /// <summary>
    /// 类型设置
    /// </summary>
    public enum EGRAPH_TYPE
    {
        INVALIDATE = -1,
        
        //剧情
        STORY = 0,
            
        //关卡
        CHAPTER,
        
        //试炼场
        TRIALFIELD,
        
        //AI技能连招
        AI_SKILL_COMBO,
        
        //AI技能连招 pvp
        AI_SKILL_COMBO_PVP,
    }
    
    public static class NodeGraphUtils
    {
        private const string NODE_GRAPH_PATH = "Assets/OutputRes/assetfiles/node_graph";
        private const string EXT = "asset";

        public static readonly string[] NodeGraphTypeName = 
        {
            "story_flow",
            "chapter_flow",
            "trial_field"
        };

        public static void CreateNewGraphWindow<TypeGraph,TypeWindow>(EGRAPH_TYPE type) where TypeGraph : BaseGraph where TypeWindow : BaseGraphWindow
        {
            BaseGraph newGraph = CreateNewGraph<TypeGraph>(type);
            if (newGraph == null)
            {
                //EditorUtility.DisplayDialog("提示", $"缺少当前类型 {typeof(TypeGraph)} 的编辑器, 请通知程序添加!", "知道了");
                return;
            }

            OpenGraphWindow<TypeWindow>(newGraph);
        }
        
        public static string OpenGraphWindowFold(EGRAPH_TYPE type)
        {
            string openPath = string.Format("{0}/{1}", NODE_GRAPH_PATH, NodeGraphTypeName[(int) type]);
            if (!Directory.Exists(openPath))
            {
                return string.Empty;
            }
            
            string filePath = EditorUtility.OpenFilePanel("Open Graph",  openPath, "asset");
            if (!string.IsNullOrEmpty(filePath))
            {
                int begineIndex = filePath.IndexOf("Assets/OutputRes", StringComparison.Ordinal);
                return filePath.Substring(begineIndex, filePath.Length - begineIndex);
            }
            
            return string.Empty;
        }

        private static BaseGraph CreateNewGraph<T>(EGRAPH_TYPE type) where T : BaseGraph
        {
            BaseGraph newGraph = ScriptableObject.CreateInstance<T>();
            string openPath = string.Format("{0}/{1}", NODE_GRAPH_PATH, NodeGraphTypeName[(int) type]);
            if (!Directory.Exists(openPath))
            {
                Directory.CreateDirectory(openPath);
            }

            string path = EditorUtility.SaveFilePanelInProject("Save Graph", "newGraph", EXT, "", openPath);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }
            
            AssetDatabase.CreateAsset(newGraph, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return newGraph;
        }

        public static void OpenGraphWindow<T>(BaseGraph graph) where T : BaseGraphWindow
        {
            T window = EditorWindow.GetWindow<T>();
            if (window == null)
            {
                return;
            }
            window.InitializeGraph(graph);
        }
        
        public static bool SaveGraph(BaseGraph saveGraph)
        {
            if (saveGraph == null)
                return false;
            
            EditorUtility.SetDirty(saveGraph);
            AssetDatabase.SaveAssets();
            
            //后续转表相关
            //todo...
            return true;
        }
    }
}