using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.IO;
using System.Text;

namespace XKT.ShaderVariantStripping.RemoveShaderPostProcessor
{
    /// <summary>
    /// 重新编译程序集实用程序
    /// </summary>
    public static class RecompileAssemblyUtility
    {
        /// <summary>
        /// 程序集详情
        /// </summary>
        public struct AssemblyInfo
        {
            public string asmName;
            public string asmDefPath;
        }
        
        public static readonly string s_tempCompileTargetFile = "Temp/com.utj.stripvariants.asmtarget.txt";

        public static void Check()
        {
            var target = GetRecompileTarget(true);
            foreach (var asm in target)
            {
                Debug.Log(asm.asmName + "::" + asm.asmDefPath);
            }

            string path = "Temp/com.utj.stripvariant.tmp.txt";
            WriteFile(path, target);
            
            var read = ReadFromFile(path);
            foreach (var asm in read)
            {
                Debug.Log("读取:" + asm.asmName + "::" + asm.asmDefPath);
            }
        }

        public static bool RewriteFile(string path, List<AssemblyInfo> list, string removeAsmName)
        {
            int reoveIdx = -1;
            int cnt = list.Count;
            for (int i = 0; i < cnt; ++i)
            {
                if (list[i].asmName == removeAsmName)
                {
                    reoveIdx = i;
                    break;
                }
            }

            if (reoveIdx >= 0)
            {
                list.RemoveAt(reoveIdx);
                WriteFile(path, list);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 读取程序集信息文件
        /// </summary>
        public static List<AssemblyInfo> ReadFromFile(string path)
        {
            var result = new List<AssemblyInfo>();
            
            if (!File.Exists(path))
            {
                return result;
            }

            var lines = File.ReadAllLines(path, Encoding.UTF8);
            foreach (var line in lines)
            {
                var separateIdx = line.IndexOf(',');
                if (separateIdx < 0)
                {
                    continue;
                }

                AssemblyInfo info = new AssemblyInfo()
                {
                    asmName = line.Substring(0, separateIdx),
                    asmDefPath = line.Substring(separateIdx + 1, line.Length - separateIdx - 1),
                };
                result.Add(info);
            }

            return result;
        }

        /// <summary>
        /// 写程序集信息文件
        /// </summary>
        public static void WriteFile(string path, List<AssemblyInfo> list)
        {
            var stringBuilder = new StringBuilder(1024);
            
            bool isFirst = true;
            foreach (var asm in list)
            {
                if (!isFirst)
                {
                    stringBuilder.Append("\n");
                }

                stringBuilder.Append(asm.asmName).Append(",").Append(asm.asmDefPath);
                isFirst = false;
            }

            File.WriteAllText(path, stringBuilder.ToString());
        }

        /// <summary>
        /// 获取重编译的目标
        /// </summary>
        /// <param name="unityOnly">仅 unity 的程序集</param>
        /// <returns>程序集信息</returns>
        public static List<AssemblyInfo> GetRecompileTarget(bool unityOnly)
        {
            var result = new List<AssemblyInfo>();
            
            var classes = GetOnProcessShaderAssemblies();
            var asmdefs = GetAsmdefs();

            foreach (var asm in classes)
            {
                var name = asm.GetName().Name;

                bool isUnityAssembly =
                    name.StartsWith("Unity.")
                    || name.StartsWith("UnityEngine.")
                    || name.StartsWith("UnityEditor.");

                // 当只需要 Unity 相关程序集时，跳过非 Unity 的程序集
                if (unityOnly && !isUnityAssembly)
                {
                    continue;
                }
                
                if (asmdefs.TryGetValue(name, out UnityEditorInternal.AssemblyDefinitionAsset asset))
                {
                    if (!result.Exists(item => item.asmName == name))
                    {
                        string assetPath = AssetDatabase.GetAssetPath(asset);
                        var info = new AssemblyInfo { asmName = name, asmDefPath = assetPath };
                        result.Add(info);
                    }
                }
            }

            return result;
        }

        // 获取包含 OnProcessShader 方法的程序集
        private static List<Assembly> GetOnProcessShaderAssemblies()
        {
            var result = new List<Assembly>();
            
            var asms = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in asms)
            {
                var types = asm.GetTypes();
                foreach (var type in types)
                {
                    if (type.IsInterface)
                    {
                        continue;
                    }

                    var method = type.GetMethod("OnProcessShader", BindingFlags.Public | BindingFlags.Instance);
                    if (method != null)
                    {
                        var args = method.GetParameters();
                        if (args.Length == 3)
                        {
                            result.Add(asm);
                            break;
                        }
                    }
                }
            }

            return result;
        }
        
        // 获取所有的 asmdef 定义
        private static Dictionary<string, UnityEditorInternal.AssemblyDefinitionAsset> GetAsmdefs()
        {
            var dictionary = new Dictionary<string, UnityEditorInternal.AssemblyDefinitionAsset>();
            
            var guids = AssetDatabase.FindAssets("t:asmdef", new string[] { "Assets", "Packages" });
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var obj = AssetDatabase.LoadAssetAtPath<UnityEditorInternal.AssemblyDefinitionAsset>(assetPath);
                
                if (dictionary.ContainsKey(obj.name))
                {
                    Debug.LogError($"碰到了重命的程序集: {obj.name}");
                    continue;
                }
                
                dictionary.Add(obj.name, obj);
            }

            return dictionary;
        }
        
    }
}