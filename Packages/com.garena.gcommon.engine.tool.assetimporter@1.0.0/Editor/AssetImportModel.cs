// Created by: WangYu   Date: 2025-09-26

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace com.garena.gcommon.engine.tool.assetimporter
{
    public static class AssetImportModel
    {
        /// <summary>
        /// 导入器的类型
        /// </summary>
        public enum EImporterType
        {
            TextureImporter = 0,
            ModelImporter,
            AudioImporter
        }

        public const string 
            PLATFORM_DEFAULT = "Default", 
            PLATFORM_STANDALONE = "PC", 
            PLATFORM_ANDROID = "Android", 
            PLATFORM_IPHONE = "iPhone";
        
        internal const string 
            TEXTURE_IMPORTER_TYPE_DIRECTIONALLIGHTMAP_NAME = "DirectionalLightmap", 
            TEXTURE_IMPORTER_TYPE_DIRECTIONAL_LIGHTMAP_NAME = "Directional Lightmap";
        
        private static AssetImportSettings s_assetImportSettings;
        
        private static XmlDocument s_importRule;
        
        // 平台设置字典
        private static Dictionary<string /* platform */
            , Dictionary<string /* filepath */
                , Dictionary<string /* prop */, string /* value */>>> s_platformSettings = new();
        // 全部脏属性的日志字典
        private static Dictionary<string, List<string>> s_allDirtyPropLogs = new();
        
        // 可能是有后缀的属性
        private static string[] s_mayBeSuffixAttributes = { "maxTextureSize", "Textureformat" };
        
        
        private static string TimeSpan2String(TimeSpan timeSpan)
        {
            return timeSpan.ToString(@"hh\:mm\:ss");
        }

        internal static bool TryParseTextureImporterType(string value, out TextureImporterType textureType)
        {
            // 能覆盖大多数常规类型判断
            if (Enum.TryParse(value, true, out textureType))
            {
                return true;
            }

            // Directional Lightmap 的情况比较多
            if (string.Equals(value, TEXTURE_IMPORTER_TYPE_DIRECTIONALLIGHTMAP_NAME, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, TEXTURE_IMPORTER_TYPE_DIRECTIONAL_LIGHTMAP_NAME, StringComparison.OrdinalIgnoreCase))
            {
                textureType = TextureImporterType.DirectionalLightmap;
                return true;
            }

            return false;
        }

        internal static bool IsDirectionalLightmap(TextureImporterType textureType)
        {
            return textureType == TextureImporterType.DirectionalLightmap;
        }

        internal static string GetTextureImporterTypeName(TextureImporterType textureType)
        {
            return IsDirectionalLightmap(textureType) ? TEXTURE_IMPORTER_TYPE_DIRECTIONALLIGHTMAP_NAME : textureType.ToString();
        }
        
        
        // 初始化
        private static void Init()
        {
            OpenXmlFile();
            
            s_platformSettings.Clear();
            s_allDirtyPropLogs.Clear();
        }
        
        // 读取 xml 规则文件
        private static void OpenXmlFile()
        {
            string xmlPath;
            s_importRule = new XmlDocument();
            
            try
            {
                xmlPath = AssetImportChecker.USER_ASSETIMPORT_XML_PATH;
                s_importRule.Load(xmlPath);
            }
            catch
            {
                xmlPath = Path.GetFullPath("Packages/com.garena.gcommon.engine.tool.editor/Samples~/ImporterRule/Importer.xml");
                s_importRule.Load(xmlPath);
            }
        }
        
        
        // 只有在非导入 Worker 环境下，才能使用批量编辑
        private static bool CanUseBatchEditing()
        {
            // 在导入 Worker 进程中会返回 true，禁止 Start/StopAssetEditing 与写回
            return !AssetDatabase.IsAssetImportWorkerProcess();
        }
        
        /// <summary>
        /// 设置，并重新导入目标资源
        /// </summary>
        public static void ImportTargetAssets(UnityEngine.Object[] assets, bool showTips)
        {
            if (!IsEnableGarenaAssetImporterTool())
            {
                return;
            }

            using (new TemporaryDeselectScope())
            {
                Stopwatch totalSw = Stopwatch.StartNew(); // 开始计时

                Init();

                bool isApplyChange = true;
                bool startedBatch = false;

                bool isCancel = false;
                bool isAllDone = true;
                string failAssetPath = "";

                try
                {
                    // 暂停所有资源的导入
                    if (isApplyChange && CanUseBatchEditing())
                    {
                        AssetDatabase.StartAssetEditing();
                        startedBatch = true;
                    }

                    for (int i = 0, max = assets.Length; i < max; i++)
                    {
                        float progress = (float)i / max;

                        var iAsset = assets[i];
                        string assetPath = AssetDatabase.GetAssetPath(iAsset);
                        if (IsSkipAsset(assetPath)) continue;

                        isCancel = EditorUtility.DisplayCancelableProgressBar("资源处理中...", $"耗时: {TimeSpan2String(totalSw.Elapsed)}", progress);
                        if (isCancel) break;

                        AssetImporter importer = AssetImporter.GetAtPath(assetPath);
                        if (importer == null) continue;

                        bool isDone = ImporterProcessor(importer, assetPath, isApplyChange);
                        if (!isDone)
                        {
                            failAssetPath += $"\n{assetPath}";
                        }

                        isAllDone &= isDone;
                    }
                }
                finally
                {
                    // 重启资源导入，并将之前暂停的都导进来
                    if (startedBatch) AssetDatabase.StopAssetEditing();
                }

                totalSw.Stop(); // 停止计时

                if (!isAllDone)
                {
                    string noAllDoneTxt = $"部分资源操作失败，请检查:{failAssetPath}";
                    Debug.LogError(noAllDoneTxt);
                }

                EditorUtility.ClearProgressBar();
                if (showTips && isCancel)
                {
                    EditorUtility.DisplayDialog("已取消", $"总耗时: {TimeSpan2String(totalSw.Elapsed)}", "ok");
                }

                if (showTips && !isCancel)
                {
                    EditorUtility.DisplayDialog("导入完成", $"总耗时: {TimeSpan2String(totalSw.Elapsed)}", "ok");
                }
            }
        }

        /// <summary>
        /// 设置，并重新导入目标资源
        /// </summary>
        public static void ImportTargetAssets(string[] assetPaths, bool showTips)
        {
            if (!IsEnableGarenaAssetImporterTool())
            {
                return;
            }

            using (new TemporaryDeselectScope())
            {

                Stopwatch totalSw = Stopwatch.StartNew(); // 开始计时

                Init();

                bool isApplyChange = true;
                bool startedBatch = false;

                bool isCancel = false;
                bool isAllDone = true;
                string failAssetPath = "";

                try
                {
                    // 暂停所有资源的导入
                    if (isApplyChange && CanUseBatchEditing())
                    {
                        AssetDatabase.StartAssetEditing();
                        startedBatch = true;
                    }

                    for (int i = 0, max = assetPaths.Length; i < max; i++)
                    {
                        float progress = (float)i / max;

                        string assetPath = assetPaths[i];
                        if (IsSkipAsset(assetPath)) continue;

                        isCancel = EditorUtility.DisplayCancelableProgressBar("资源处理中...", $"耗时: {TimeSpan2String(totalSw.Elapsed)}", progress);
                        if (isCancel) break;

                        AssetImporter importer = AssetImporter.GetAtPath(assetPath);
                        if (importer == null) continue;

                        bool isDone = ImporterProcessor(importer, assetPath, isApplyChange);
                        if (!isDone)
                        {
                            failAssetPath += $"\n{assetPath}";
                        }

                        isAllDone &= isDone;
                    }
                }
                finally
                {
                    // 重启资源导入，并将之前暂停的都导进来
                    if (startedBatch) AssetDatabase.StopAssetEditing();
                }

                totalSw.Stop(); // 停止计时

                if (!isAllDone)
                {
                    string noAllDoneTxt = $"部分资源操作失败，请检查:{failAssetPath}";
                    Debug.LogError(noAllDoneTxt);
                }

                EditorUtility.ClearProgressBar();
                if (showTips && isCancel)
                {
                    EditorUtility.DisplayDialog("已取消", $"总耗时: {TimeSpan2String(totalSw.Elapsed)}", "ok");
                }

                if (showTips && !isCancel)
                {
                    EditorUtility.DisplayDialog("导入完成", $"总耗时: {TimeSpan2String(totalSw.Elapsed)}", "ok");
                }
            }
        }

        /// <summary>
        /// 检查全部资源
        /// </summary>
        public static void CheckAllAssets(string directory, EImporterType importerType, bool isApplyChange, string outputLog, bool showTips)
        {
            if (!IsEnableGarenaAssetImporterTool())
            {
                return;
            }

            using (new TemporaryDeselectScope())
            {
                Stopwatch totalSw = Stopwatch.StartNew(); // 开始计时

                string[] assetGUIDs = new string[0];
                string importerTypeLabel = "";
                if (importerType == EImporterType.TextureImporter)
                {
                    assetGUIDs = AssetDatabase.FindAssets("t:texture2D", new[] { directory });
                    importerTypeLabel = "纹理";
                }
                else if (importerType == EImporterType.ModelImporter)
                {
                    assetGUIDs = AssetDatabase.FindAssets("t:Model", new[] { directory });
                    importerTypeLabel = "模型";
                }
                else if (importerType == EImporterType.AudioImporter)
                {
                    assetGUIDs = AssetDatabase.FindAssets("t:Audio", new[] { directory });
                    importerTypeLabel = "音频";
                }

                Init();

                bool startedBatch = false;

                bool isCancel = false;
                bool isAllDone = true;
                string failAssetPath = "";

                try
                {
                    // 暂停所有资源的导入
                    if (isApplyChange && CanUseBatchEditing())
                    {
                        AssetDatabase.StartAssetEditing();
                        startedBatch = true;
                    }

                    for (int i = 0, max = assetGUIDs.Length; i < max; i++)
                    {
                        float progress = (float)i / max;

                        string guid = assetGUIDs[i];
                        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        if (IsSkipAsset(assetPath)) continue;

                        isCancel = EditorUtility.DisplayCancelableProgressBar("资源处理中...", $"耗时: {TimeSpan2String(totalSw.Elapsed)}", progress);
                        if (isCancel) break;

                        AssetImporter importer = AssetImporter.GetAtPath(assetPath);
                        if (importer == null) continue;

                        bool isDone = ImporterProcessor(importer, assetPath, isApplyChange);
                        if (!isDone)
                        {
                            failAssetPath += $"\n{assetPath}";
                        }

                        isAllDone &= isDone;
                    }
                }
                finally
                {
                    // 重启资源导入，并将之前暂停的都导进来
                    if (startedBatch) AssetDatabase.StopAssetEditing();
                }

                totalSw.Stop(); // 停止计时

                if (!isAllDone)
                {
                    string noAllDoneTxt = $"部分资源操作失败，请检查:{failAssetPath}";
                    Debug.LogError(noAllDoneTxt);
                }

                OutputDirtyLog(outputLog);

                EditorUtility.ClearProgressBar();
                if (showTips && isCancel)
                {
                    EditorUtility.DisplayDialog("已取消", $"总耗时: {TimeSpan2String(totalSw.Elapsed)}", "ok");
                }

                if (showTips && !isCancel)
                {
                    string title = "检查完成";
                    string message2 = isApplyChange ? "已应用修改" : "已完成检查";
                    string message = importerTypeLabel + message2 + $" 总耗时: {TimeSpan2String(totalSw.Elapsed)}";
                    EditorUtility.DisplayDialog(title, message, "ok");
                }
            }
        }

        /// <summary>
        /// 根据配置文件，检查资源工具是否有被启用
        /// </summary>
        public static bool IsEnableGarenaAssetImporterTool()
        {
            if (s_assetImportSettings == null)
            {
                s_assetImportSettings = AssetDatabase.LoadAssetAtPath<AssetImportSettings>(AssetImportChecker.USER_ASSETIMPORT_ASSET_PATH);
            }

            if (s_assetImportSettings == null)
            {
                return false;
            }

            if (!s_assetImportSettings.Enable_Assetimporter)
            {
                return false;
            }

            return true;
        }

        // 是需要跳过的资源
        private static bool IsSkipAsset(string assetPath)
        {
            // 包里的资源都不改
            if (assetPath.StartsWith("Packages/"))
            {
                return true;
            }
            
            // 序列化配置不需要改
            string extension = System.IO.Path.GetExtension(assetPath);
            if (extension == ".asset")
            {
                return true;
            }

            return false;
        }
        
        // 输出改动日志
        private static void OutputDirtyLog(string logPath)
        {
            Debug.Log("资源导入期输出日志 ok，位置: " + logPath);

            using (StreamWriter writer = new StreamWriter(logPath, false))
            {
                foreach (var kFile in s_allDirtyPropLogs)
                {
                    writer.WriteLine(kFile.Key + ":");

                    List<string> props = kFile.Value;
                    foreach (var kProp in props)
                    {
                        writer.WriteLine(kProp);
                    }

                    writer.WriteLine("\n");
                }
            }
            s_allDirtyPropLogs.Clear();
        }

        // 是否匹配规则
        private static bool IsMatchRule(AssetImporter assetImporter, EImporterType importerType, string path, XmlElement rule)
        {
            bool result = false;
            
            switch (rule.Name)
            {
                case "Include": // 包含
                {
                    //Path case
                    string strs = rule.GetAttribute("path").Replace("\t", "").Replace("\r", "").Replace("\n", "");
                    string keywordRule = rule.GetAttribute("KeyWordRule");
                    string[] strpath = strs.Split("|".ToCharArray());
                    foreach (string str in strpath)
                    {
                        bool isMatchPath = false;
                        if (!string.IsNullOrEmpty(str))
                        {
                            if (str.Contains("*"))
                            {
                                string[] splitstr = str.Split("*".ToCharArray());
                                string matchstr = @"" + splitstr[0];
                                for (int i = 1; i < splitstr.Length; i++)
                                {
                                    matchstr = matchstr + @"\w*" + splitstr[i];
                                }

                                var match = Regex.Match(path, matchstr, RegexOptions.IgnoreCase);
                                result = result || match.Success;
                            }
                            else
                            {
                                result = result || path.ToLower().StartsWith(str.ToLower());
                            }
                        }
                        else if (!string.IsNullOrEmpty(keywordRule))
                        {
                            string _keyword = rule.GetAttribute("FileKeyWord").ToLower();
                            string filename = Path.GetFileNameWithoutExtension(path).ToLower();
                            if ("Include" == keywordRule)
                            {
                                string[] splitstr = filename.Split("_".ToCharArray());
                                string subkeyword = _keyword.Substring(1, _keyword.Length - 1);
                                foreach (string s in splitstr)
                                {
                                    result = result || subkeyword.Equals(s);
                                }
                            }

                            if ("BeginWith" == keywordRule)
                            {
                                result = result || filename.StartsWith(_keyword);
                            }

                            if ("EndWith" == keywordRule)
                            {
                                result = result || filename.EndsWith(_keyword);
                            }
                        }
                    }
                }
                    break;
                
                case "Exclude": // 排除
                {
                    result = true;
                    string strs = rule.GetAttribute("path").Replace("\t", "").Replace("\r", "").Replace("\n", "");
                    string[] strpath = strs.Split("|".ToCharArray());
                    foreach (string str in strpath)
                    {
                        if ("" != str)
                        {
                            if (str.Contains("*"))
                            {
                                string[] splitstr = str.Split("*".ToCharArray());
                                string matchstr = @"" + splitstr[0];
                                for (int i = 1; i < splitstr.Length; i++)
                                {
                                    matchstr = matchstr + @"\w*" + splitstr[i];
                                }

                                var match = Regex.Match(path, matchstr, RegexOptions.IgnoreCase);

                                result = result && !match.Success;
                            }
                            else
                            {
                                result = result && !path.ToLower().StartsWith(str.ToLower());
                            }
                        }
                        else //ext case
                        {
                            string rulecase = rule.GetAttribute("KeyWordRule");
                            string _keyword = rule.GetAttribute("FileKeyWord").ToLower();
                            string filename = Path.GetFileNameWithoutExtension(path).ToLower();
                            if ("Include" == rulecase)
                            {
                                result = result && !filename.Contains(_keyword);
                            }

                            if ("BeginWith" == rulecase)
                            {
                                result = result && !filename.StartsWith(_keyword);
                            }

                            if ("EndWith" == rulecase)
                            {
                                result = result && !filename.EndsWith(_keyword);
                            }
                        }
                    }
                }
                    break;
                
                case "FileLength": // 文件长度
                {
                    string rulecase = rule.GetAttribute("size");
                    short length = XmlConvert.ToInt16(rulecase);
                    string keyword = rule.GetAttribute("KeyWordRule");
                    FileInfo info = new FileInfo(path);
                    if ("greater" == keyword)
                    {
                        result = (info.Length > length * 1024);
                    }
                    else
                    {
                        result = (info.Length < length * 1024);
                    }
                }
                    break;
                
                default:
                    break;
            }

            if (rule.HasChildNodes && result)
            {
                foreach (XmlElement xe in rule)
                {
                    result = result && IsMatchRule(assetImporter, importerType, path, xe);
                }
            }

            return result;
        }
        
        // 处理导入的资源
        private static bool ImporterProcessor(AssetImporter assetImporter, string path, bool isApplyChange)
        {
            if (!IsEnableGarenaAssetImporterTool())
            {
                return false;
            }

            TextureImporterType curTextureType = TextureImporterType.Default;
            string importer = "";
            EImporterType importType = EImporterType.TextureImporter;
            if (assetImporter is TextureImporter)
            {
                TextureImporter textureImporter = assetImporter as TextureImporter;
                curTextureType = textureImporter.textureType;
                
                importer = "TextureImporter";
                importType = EImporterType.TextureImporter;
            }
            else if (assetImporter is ModelImporter)
            {
                importer = "ModelImporter";
                importType = EImporterType.ModelImporter;
            }
            else if (assetImporter is AudioImporter)
            {
                importer = "AudioImporter";
                importType = EImporterType.AudioImporter;
            }
            else
            {
                return false;
            }

            s_platformSettings[PLATFORM_DEFAULT] = new Dictionary<string, Dictionary<string, string>>();
            s_platformSettings[PLATFORM_STANDALONE] = new Dictionary<string, Dictionary<string, string>>();
            s_platformSettings[PLATFORM_ANDROID] = new Dictionary<string, Dictionary<string, string>>();
            s_platformSettings[PLATFORM_IPHONE] = new Dictionary<string, Dictionary<string, string>>();

            // 读取 xml 中的规则
            XmlNodeList rootNode = s_importRule.SelectSingleNode("AssetImportRules").SelectSingleNode(importer).ChildNodes;
            foreach (XmlNode childNode in rootNode)
            {
                // 跳过注释
                if (childNode.NodeType == XmlNodeType.Comment)
                {
                    continue;
                }

                XmlElement child = (XmlElement)childNode;
                
                // 默认设置节点
                if (child.Name.ToLower() == "defaultsetting")
                {
                    foreach (XmlElement item in child)
                    {
                        // 平台属性字典
                        var platformPropDict = new Dictionary<string, string>();
                        string platformStr = PLATFORM_DEFAULT;
                        
                        if (item.Name == "setting")
                        {
                            XmlAttributeCollection attrs = item.Attributes;
                            foreach (XmlAttribute attr in attrs)
                            {
                                if (attr.Name.ToLower() == "platform")
                                {
                                    if (!string.IsNullOrEmpty(attr.Value))
                                    {
                                        platformStr = attr.Value;
                                    }
                                    continue;
                                }
                                
                                platformPropDict[attr.Name] = attr.Value;
                            }

                            SetPlatformByPath(platformStr, "", platformPropDict);
                        }
                    }
                }
                else
                {
                    foreach (XmlNode ruleNode in child.ChildNodes)
                    {
                        // 跳过注释
                        if (ruleNode.NodeType == XmlNodeType.Comment)
                        {
                            continue;
                        }
                        
                        XmlNodeList settingList = ruleNode.SelectNodes("setting");
                        XmlNodeList includeList = ruleNode.SelectNodes("Include");
                        
                        foreach (var kSetting in settingList)
                        {
                            // 设置属性字典
                            var settingsPropDict = new Dictionary<string, string>();
                            string platformStr = PLATFORM_DEFAULT;
                            
                            XmlElement eSetting = (XmlElement)kSetting;
                            foreach (XmlAttribute kAttri in eSetting.Attributes)
                            {
                                if (kAttri.Name == "Platform")
                                {
                                    if (!string.IsNullOrEmpty(kAttri.Value))
                                    {
                                        platformStr = kAttri.Value;
                                    }
                                    continue;
                                }

                                settingsPropDict[kAttri.Name] = kAttri.Value;
                            }

                            foreach (var kInclude in includeList)
                            {
                                XmlElement element = (XmlElement)kInclude;
                                bool isMatch = IsMatchRule(assetImporter, importType, path, element);
                                if (!isMatch) continue;
                                
                                string filePath = element.GetAttribute("path");
                                string textureType = element.GetAttribute("TextureType");

                                // 要区分贴图的类型了
                                if (!string.IsNullOrEmpty(textureType))
                                {
                                    if (TryParseTextureImporterType(textureType, out TextureImporterType eType))
                                    {
                                        if (eType == curTextureType)
                                        {
                                            string textureTypeName = GetTextureImporterTypeName(curTextureType);
                                            string tempFilePath = filePath + "*." + textureTypeName + "/";

                                            for (int i = 0; i < s_mayBeSuffixAttributes.Length; i++)
                                            {
                                                string overrideProp = s_mayBeSuffixAttributes[i];
                                                if (settingsPropDict.ContainsKey(overrideProp))
                                                {
                                                    // xxx_NormalMap, xxx_ShadowMask, xxx_Lightmap, xxx_DirectionalLightmap
                                                    string overrideProp_TextureType = overrideProp + "_" + textureTypeName;
                                                    settingsPropDict[overrideProp_TextureType] = settingsPropDict[overrideProp];
                                                }
                                            }

                                            SetPlatformByPath(platformStr, tempFilePath, settingsPropDict);
                                        }
                                    }
                                }
                                else
                                {
                                    SetPlatformByPath(platformStr, filePath, settingsPropDict);
                                }
                            }
                        }
                    }
                }
            }
            
            ApplySettings(assetImporter, importType, path, isApplyChange);
            
            return true;
        }

        // 根据路径设置平台
        private static void SetPlatformByPath(string platform, string filepath, Dictionary<string, string> dicProp)
        {
            if (platform.Contains("*"))
            {
                SetPlatformSetting(PLATFORM_DEFAULT, filepath, dicProp);
                SetPlatformSetting(PLATFORM_STANDALONE, filepath, dicProp);
                SetPlatformSetting(PLATFORM_ANDROID, filepath, dicProp);
                SetPlatformSetting(PLATFORM_IPHONE, filepath, dicProp);
            }
            else if (platform.Contains("|"))
            {
                string[] splitArray = platform.Split("|");
                for (int i = 0; i < splitArray.Length; i++)
                {
                    string strTemp = splitArray[i].Trim();
                    SetPlatformSetting(strTemp, filepath, dicProp);
                }
            }
            else
            {
                SetPlatformSetting(platform, filepath, dicProp);
            }
        }

        // 设置平台设置
        private static void SetPlatformSetting(string platform, string path, Dictionary<string, string> dicProp)
        {
            if (s_platformSettings.ContainsKey(platform))
            {
                var platformSetting = s_platformSettings[platform];
                if (!platformSetting.ContainsKey(path))
                {
                    platformSetting[path] = new Dictionary<string, string>();
                }

                Dictionary<string, string> dicSettingsProp = platformSetting[path];
                foreach (var iter in dicProp)
                {
                    dicSettingsProp[iter.Key] = iter.Value;
                }
            }
        }
        
        
        // 查找最匹配的规则
        private static Dictionary<string, string> FindBestMatchSetting(string platform)
        {
            var finalPropSetting = new Dictionary<string, string>();
            
            if (s_platformSettings.ContainsKey(platform))
            {
                // 按文件路径组织配置
                Dictionary<string, Dictionary<string, string>> filesProp = s_platformSettings[platform];
                
                List<string> files = new List<string>(filesProp.Keys);
                files.Sort(SortString);
                
                for (int i = 0; i < files.Count; i++)
                {
                    string filePath = files[i];
                    Dictionary<string, string> fileProp = filesProp[filePath];
                    foreach (var iter in fileProp)
                    {
                        finalPropSetting[iter.Key] = iter.Value;
                    }
                }
            }

            return finalPropSetting;
        }

        private static int SortString(string left, string right)
        {
            return left.Length.CompareTo(right.Length);
        }

        // 根据平台跳过属性
        private static bool IsSkipPropForPlatform(string platform, string propName, string propValue)
        {
            if (platform == PLATFORM_DEFAULT)
            {
                if (propName.Contains("NormalMap") || propName.Contains("Shadowmask") || propName.Contains("Lightmap"))
                {
                    return true;
                }

                if (propValue.Contains("ASTC"))
                {
                    return true;
                }
            }

            if (platform == PLATFORM_STANDALONE)
            {
                if (propValue.Contains("ASTC"))
                {
                    return true;
                }
            }

            return false;
        }


        // 应用设置
        private static void ApplySettings(AssetImporter assetImporter, EImporterType importerType, string path, bool isApplyChange)
        {
            var listLogs = new List<string>();
            bool isFileDirty = false;
            bool isFindTestFile = false;

            Dictionary<string, string> finalDefaultSetting;
            var finalPlatformSetting = new Dictionary<string, Dictionary<string, string>>();

            // 用默认配置作底子
            finalDefaultSetting = FindBestMatchSetting(PLATFORM_DEFAULT);
            finalPlatformSetting[PLATFORM_DEFAULT] = finalDefaultSetting;
            
            // 是纹理导入器
            if (importerType == EImporterType.TextureImporter)
            {
                // 读取各平台的特殊配置，缺少的用默认的补齐
                
                // pc 设置
                Dictionary<string, string> finalPCSetting = FindBestMatchSetting(PLATFORM_STANDALONE);
                foreach (var kDefault in finalDefaultSetting)
                {
                    if (!finalPCSetting.ContainsKey(kDefault.Key))
                    {
                        finalPCSetting[kDefault.Key] = kDefault.Value;
                    }
                }
                finalPlatformSetting[PLATFORM_STANDALONE] = finalPCSetting;

                // android 设置
                Dictionary<string, string> finalAndroidSetting = FindBestMatchSetting(PLATFORM_ANDROID);
                foreach (var kDefault in finalDefaultSetting)
                {
                    if (!finalAndroidSetting.ContainsKey(kDefault.Key))
                    {
                        finalAndroidSetting[kDefault.Key] = kDefault.Value;
                    }
                }
                finalPlatformSetting[PLATFORM_ANDROID] = finalAndroidSetting;

                // ios 设置
                Dictionary<string, string> finalIOSSetting = FindBestMatchSetting(PLATFORM_IPHONE);
                foreach (var kDefault in finalDefaultSetting)
                {
                    if (!finalIOSSetting.ContainsKey(kDefault.Key))
                    {
                        finalIOSSetting[kDefault.Key] = kDefault.Value;
                    }
                }
                finalPlatformSetting[PLATFORM_IPHONE] = finalIOSSetting;
            }

            // 应用设置
            foreach (var kPlatformSetting in finalPlatformSetting)
            {
                string platformName = kPlatformSetting.Key;
                Dictionary<string, string> props = kPlatformSetting.Value;

                foreach (var kProp in props)
                {
                    if (IsSkipPropForPlatform(platformName, kProp.Key, kProp.Value)) continue;

                    if (kProp.Value == "Automatic")
                    {
                        Debug.LogError($"{path} {platformName} {kProp.Key} {kProp.Value}");
                    }
                    
                    string dirtyValue = ApplySettingsForPlatform(assetImporter, importerType, 
                        platformName, kProp.Key, kProp.Value, 
                        isApplyChange);
                    if (!string.IsNullOrEmpty(dirtyValue))
                    {
                        string log = $"{platformName}  {kProp.Key}:   {dirtyValue}  ===>  {kProp.Value}";
                        listLogs.Add(log);
                        isFileDirty = true;
                    }
                }
            }

            if (isFileDirty)
            {
                if (isApplyChange)
                {
                    /*
                    // 调试日志
                    string outLog = "";
                    foreach (var item in listLogs)
                    {
                        outLog += item + "\n";
                    }
                    Debug.Log($"有脏修改: {path}\n{outLog}");
                    */
                    
                    // 仅在主线程/非 Worker 环境写回导入设置
                    if (CanUseBatchEditing())
                    {
                        AssetDatabase.WriteImportSettingsIfDirty(path);
                    }
                }

                // 记录日志
                if (s_allDirtyPropLogs.ContainsKey(path))
                {
                    s_allDirtyPropLogs[path].AddRange(listLogs);
                }
                else
                {
                    s_allDirtyPropLogs[path] = listLogs;
                }
            }
        }
        
        private static string ApplySettingsForPlatform(AssetImporter assetImporter, EImporterType importerType,
            string platform, string name, string value, 
            bool isApplyChange)
        {
            string dirtyValue = "";
            
            // 纹理导入器
            if (importerType == EImporterType.TextureImporter)
            {
                TextureImporter ti = assetImporter as TextureImporter;
                TextureImporterPlatformSettings textureImporterSetting = ti.GetDefaultPlatformTextureSettings();
                
                if (platform == PLATFORM_STANDALONE)
                {
                    textureImporterSetting = ti.GetPlatformTextureSettings(PLATFORM_STANDALONE);
                    /*
                    if (!textureImporterSetting.overridden)
                    {
                        return dirtyValue;
                    }
                    */
                }
                else if (platform == PLATFORM_ANDROID)
                {
                    textureImporterSetting = ti.GetPlatformTextureSettings(PLATFORM_ANDROID);
                    /*
                    if (!textureImporterSetting.overridden)
                    {
                        return dirtyValue;
                    }
                    */
                }
                else if (platform == PLATFORM_IPHONE)
                {
                    textureImporterSetting = ti.GetPlatformTextureSettings(PLATFORM_IPHONE);
                    /*
                    if (!textureImporterSetting.overridden)
                    {
                        return dirtyValue;
                    }
                    */
                }

                return TextureSettingSetter.SetTextureSingleProp(ti, textureImporterSetting, name, value, isApplyChange);
            }
            // 模型导入器
            else if (importerType == EImporterType.ModelImporter)
            {
                ModelImporter ti = assetImporter as ModelImporter;
                return ModelSettingSetter.SetModelSingleProp(ti, name, value, isApplyChange);
            }
            // 音频导入器
            else if (importerType == EImporterType.AudioImporter)
            {
                AudioImporter ai = assetImporter as AudioImporter;
                return AudioSettingSetter.SetAudioSingleProp(ai, name, value, isApplyChange);
            }

            return dirtyValue;
        }
        
    }
}
