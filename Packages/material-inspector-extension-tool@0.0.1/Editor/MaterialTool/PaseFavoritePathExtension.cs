using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;

namespace MaterialInspectorExtensionTool.Editor.MaterialTool
{
    /// <summary>
    /// 通过收藏夹对路径进行扩展
    /// </summary>
    public static class PaseFavoritePathExtension
    {
        public static List<string> s_stringName = new();
        public static List<string> s_stringPath = new();

        private static string s_path = FileChange.PreferencesPath + @"\SearchFilters";

        [InitializeOnLoadMethod]
        public static void ReadYAML()
        {
            string[] arrays = File.ReadAllLines(s_path, Encoding.UTF8);
            
            s_stringName.Clear();
            s_stringPath.Clear();
            // 解析文件，把名字和路径放入表里
            PaseYAML(arrays, ref s_stringName, ref s_stringPath);
            // 在最前面占位
            s_stringName.Insert(0, "Asset");
            s_stringPath.Insert(0, "Assets");
        }

        private static void PaseYAML(string[] text, ref List<string> stringName, ref List<string> stringPath)
        {
            for (int i = 3; i < text.Length; i++) // 从3行开始，  
            {
                if (text[i].StartsWith("  - m_Name"))
                {
                    var tempName = text[i].Substring(12);
                    if (tempName[0] == '\"')
                    {
                        tempName = tempName.Substring(1, tempName.Length - 2);
                        tempName = Regex.Unescape(tempName); // 转换\转义 
                    }

                    stringName.Add(tempName);
                    
                    // 前面固定长度所以可以这样去掉没用的
                    for (int j = i + 1; j < text.Length; j++)
                    {
                        if ( (text[j].StartsWith("      m_ClassNames") && !text[j].Contains("[]")) 
                            || (text[j].StartsWith("      m_Folders") && text[j].Contains("[]")) )
                        {
                            stringName.RemoveAt(stringName.Count - 1); // 如果不是真实的收藏路径就把名字表去掉 
                            i = j;
                            break;
                        }

                        if (text[j].StartsWith("      - Assets"))
                        {
                            var str = text[j].Substring(8);
                            if (Directory.Exists(str))
                            {
                                stringPath.Add(text[j].Substring(8));
                            }
                            else
                            {
                                stringName.RemoveAt(stringName.Count - 1);
                            }

                            i = j;
                            break;
                        }

                        if (text[j].StartsWith("      - \"Assets")) // 中文路径会多一对双引号
                        {
                            var str = text[j].Substring(9, text[j].Length - 10);
                            // var str = text[j].Substring(8); 
                            //  str=  UnicodeToString(str);
                            str = Regex.Unescape(str);
                            
                            if (Directory.Exists(str))
                            {
                                stringPath.Add(str);
                            }
                            else
                            {
                                stringName.RemoveAt(stringName.Count - 1);
                            }

                            i = j;
                            break;
                        }
                    }
                }
            }
        }
        
    }
}