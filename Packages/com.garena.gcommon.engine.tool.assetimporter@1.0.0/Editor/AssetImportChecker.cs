using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Linq;
using EImporterType = com.garena.gcommon.engine.tool.assetimporter.AssetImportModel.EImporterType;

namespace com.garena.gcommon.engine.tool.assetimporter
{
    public class AssetImportChecker : EditorWindow
    {
        private string m_rootPath = "Assets/OriginalRes"; // 默认根目录
        // 默认输出文件
        private string m_outputCheckAllTexture = "Assets/../AssetImportChecker_AllTexture.txt";
        private string m_outputCheckAllModel = "Assets/../AssetImportChecker_AllModel.txt";
        private string m_outputDiffPropDir = "Assets/../AssetImportChecker_DifferentPropDir.xml";
        private bool m_includeFilePaths = true; // 是否导出文件路径（只对少数值有效）
        
        public const string USER_ASSETIMPORT_WORK_DIR = "Assets/Editor/ImporterRule/";
        public const string USER_ASSETIMPORT_XML_PATH = USER_ASSETIMPORT_WORK_DIR + "Importer.xml";
        public const string USER_ASSETIMPORT_ASSET_PATH = USER_ASSETIMPORT_WORK_DIR + "AssetImportSettings.asset";

        private static GUIStyle s_bigButtonStyle;

        [MenuItem("Tools/GarenaAssetImporterTool")]
        public static void ShowWindow()
        {
            AssetImportChecker window = GetWindow<AssetImportChecker>("AssetImporterTool");
            window.minSize = new Vector2(500, 300);
            // window.maxSize = new Vector2(2000, 400);

            // 设置窗口初始大小和位置
            window.position = new Rect(100, 100, 500, 300); // x, y, width, heigh

            try
            {
                bool isNeedRefresh = false;
                if (!Directory.Exists(USER_ASSETIMPORT_WORK_DIR))
                {
                    Directory.CreateDirectory(USER_ASSETIMPORT_WORK_DIR);
                    isNeedRefresh = true;
                }

                if (!File.Exists(USER_ASSETIMPORT_XML_PATH))
                {
                    File.Copy("Packages/com.garena.gcommon.engine.tool.assetimporter/Samples~/ImporterRule/Importer.xml", USER_ASSETIMPORT_XML_PATH, true);
                    isNeedRefresh = true;
                }

                if (!File.Exists(USER_ASSETIMPORT_ASSET_PATH))
                {
                    File.Copy("Packages/com.garena.gcommon.engine.tool.assetimporter/Editor/AssetImportSettings.asset", USER_ASSETIMPORT_ASSET_PATH, true);
                    isNeedRefresh = true;
                }

                if (isNeedRefresh)
                {
                    AssetDatabase.Refresh();
                }
            }
            catch (Exception ex)
            {
                string log = $"文件系统操作异常:\n{ex}";
                Debug.LogError(log);
                throw;
            }
        }

        private void OnGUI()
        {
            if (s_bigButtonStyle == null)
            {
                s_bigButtonStyle = new(GUI.skin.button);
                s_bigButtonStyle.fontSize = 16;
            }
            
            GUILayout.Space(5);
            GUILayout.Label("按照 Importer.xml 规则检查和应用设置", EditorStyles.boldLabel);

            GUILayout.Space(5);
            m_rootPath = EditorGUILayout.TextField("检查根目录:", m_rootPath);
            m_outputCheckAllTexture = EditorGUILayout.TextField("输出所有与 Importer 规则不一致贴图日志路径:", m_outputCheckAllTexture);
            m_outputCheckAllModel = EditorGUILayout.TextField("输出所有与 Importer 规则不一致模型日志路径:", m_outputCheckAllModel);
            m_outputDiffPropDir = EditorGUILayout.TextField("输出包含不同属性文件夹日志路径:", m_outputDiffPropDir);

            GUILayout.Space(10);
            using (new GUILayout.VerticalScope("box"))
            {
                GUILayout.Label("检查与 Importer.xml 规则不一致的资源");
                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("贴图"))
                    {
                        AssetImportModel.CheckAllAssets(m_rootPath, EImporterType.TextureImporter, false, m_outputCheckAllTexture, true);
                    }

                    GUILayout.Space(10);
                    if (GUILayout.Button("模型"))
                    {
                        AssetImportModel.CheckAllAssets(m_rootPath, EImporterType.ModelImporter, false, m_outputCheckAllModel, true);
                    }
                    
                    GUILayout.Space(10);
                    if (GUILayout.Button("音频"))
                    {
                        AssetImportModel.CheckAllAssets(m_rootPath, EImporterType.AudioImporter, false, m_outputCheckAllModel, true);
                    }
                }
                
                GUILayout.Space(5);
                if (GUILayout.Button("检查文件夹下属性不一致资源并且导出 XML"))
                {
                    GenerateXmlReport(m_rootPath, m_outputDiffPropDir, m_includeFilePaths);
                    Debug.Log($"✅ 报表已生成: {m_outputDiffPropDir}");
                }
            }
            
            GUILayout.Space(10);
            using (new GUILayout.VerticalScope("box"))
            {
                GUILayout.Label("根据 Import.xml 规则设置，并重新导入资源");
                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("贴图"))
                    {
                        AssetImportModel.CheckAllAssets(m_rootPath, EImporterType.TextureImporter, true, m_outputCheckAllTexture, true);
                    }

                    GUILayout.Space(5);
                    if (GUILayout.Button("模型"))
                    {
                        AssetImportModel.CheckAllAssets(m_rootPath, EImporterType.ModelImporter, true, m_outputCheckAllModel, true);
                    }
                    
                    GUILayout.Space(5);
                    if (GUILayout.Button("音频"))
                    {
                        AssetImportModel.CheckAllAssets(m_rootPath, EImporterType.AudioImporter, true, m_outputCheckAllModel, true);
                    }
                }

                GUILayout.Space(5);
                if (GUILayout.Button("选中资源应用 Import.xml 规则并且重新导入"))
                {
                    AssetImportModel.ImportTargetAssets(Selection.objects, true);
                }
            }
            
            GUILayout.Space(5);
        }

        private void GenerateXmlReport(string path, string outputFile, bool includeFilePaths)
        {
            if (!Directory.Exists(path))
            {
                Debug.LogError($"路径不存在: {path}");
                return;
            }

            XmlDocument xmlDoc = new XmlDocument();
            XmlElement rootNode = xmlDoc.CreateElement("ImportSettingsReport");
            xmlDoc.AppendChild(rootNode);

            // 针对 Standalone / Android / iOS / Default 平台依次检查
            BuildTargetGroup[] platforms = 
            {
                BuildTargetGroup.Standalone,
                BuildTargetGroup.Android,
                BuildTargetGroup.iOS,
                BuildTargetGroup.Unknown // 用于 Default
            };
            foreach (var platform in platforms)
            {
                AnalyzePlatform(path, platform, xmlDoc, rootNode, includeFilePaths);
            }

            xmlDoc.Save(outputFile);
            AssetDatabase.Refresh();
        }

        private void AnalyzePlatform(string rootPath, BuildTargetGroup platform, XmlDocument xmlDoc, XmlElement rootNode, bool includeFilePaths)
        {
            // folder -> property -> value -> file list
            var stats = new Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>>();
            
            string[] guids = AssetDatabase.FindAssets("", new[] { rootPath });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(assetPath);
                if (importer == null) continue;

                string folder = Path.GetDirectoryName(assetPath).Replace("\\", "/");
                if (!stats.ContainsKey(folder))
                {
                    stats[folder] = new Dictionary<string, Dictionary<string, List<string>>>();
                }

                if (importer is TextureImporter texImporter)
                {
                    string maxTexSize = GetTextureImporterPlatformValue(texImporter, platform, "maxTextureSize");
                    string format = GetTextureImporterPlatformValue(texImporter, platform, "format");

                    CollectStat(stats[folder], "maxTextureSize", maxTexSize, assetPath);
                    CollectStat(stats[folder], "format", format, assetPath);
                    CollectStat(stats[folder], "mipmapEnabled", texImporter.mipmapEnabled.ToString(), assetPath);
                    CollectStat(stats[folder], "anisoLevel", texImporter.anisoLevel.ToString(), assetPath);
                    CollectStat(stats[folder], "isReadable", texImporter.isReadable.ToString(), assetPath);
                    CollectStat(stats[folder], "alphaIsTransparency", texImporter.alphaIsTransparency.ToString(), assetPath);
                    CollectStat(stats[folder], "alphaSource", texImporter.alphaSource.ToString(), assetPath);
                    CollectStat(stats[folder], "npotScale", texImporter.npotScale.ToString(), assetPath);
                }
                else if (importer is ModelImporter modelImporter)
                {
                    CollectStat(stats[folder], "isReadable", modelImporter.isReadable.ToString(), assetPath);
                    CollectStat(stats[folder], "meshCompression", modelImporter.meshCompression.ToString(), assetPath);
                    CollectStat(stats[folder], "materialImportMode", modelImporter.materialImportMode.ToString(), assetPath);
                    CollectStat(stats[folder], "animationCompression", modelImporter.animationCompression.ToString(), assetPath);
                    CollectStat(stats[folder], "animationRotationError", modelImporter.animationRotationError.ToString(), assetPath);
                    CollectStat(stats[folder], "animationPositionError", modelImporter.animationPositionError.ToString(), assetPath);
                    CollectStat(stats[folder], "removeConstantScaleCurves", modelImporter.removeConstantScaleCurves.ToString(), assetPath);
                }
                else if (importer is AudioImporter audioImporter)
                {
                    var settings = audioImporter.defaultSampleSettings;
                    CollectStat(stats[folder], "loadType", settings.loadType.ToString(), assetPath);
                    CollectStat(stats[folder], "compressionFormat", settings.compressionFormat.ToString(), assetPath);
                }
            }

            // 输出到 XML，只导出存在不一致的属性
            foreach (var folderEntry in stats)
            {
                foreach (var propEntry in folderEntry.Value)
                {
                    if (propEntry.Value.Count <= 1) continue; // 属性值一致则跳过

                    XmlElement folderNode = xmlDoc.CreateElement("Folder");
                    folderNode.SetAttribute("path", folderEntry.Key);
                    folderNode.SetAttribute("platform", GetPlatformName(platform));
                    folderNode.SetAttribute("property", propEntry.Key);
                    rootNode.AppendChild(folderNode);

                    // 找到数量最多的属性值
                    int maxCount = propEntry.Value.Max(v => v.Value.Count);

                    foreach (var valEntry in propEntry.Value)
                    {
                        XmlElement valNode = xmlDoc.CreateElement("Value");
                        valNode.SetAttribute("data", valEntry.Key);
                        valNode.SetAttribute("count", valEntry.Value.Count.ToString());
                        folderNode.AppendChild(valNode);

                        // 仅对少数值输出文件列表
                        if (includeFilePaths && valEntry.Value.Count < maxCount)
                        {
                            foreach (var file in valEntry.Value)
                            {
                                XmlElement fileNode = xmlDoc.CreateElement("File");
                                fileNode.SetAttribute("data", file);
                                valNode.AppendChild(fileNode);
                            }
                        }
                    }
                }
            }
        }

        private void CollectStat(Dictionary<string, Dictionary<string, List<string>>> folderStats, string key, string value, string assetPath)
        {
            if (!folderStats.ContainsKey(key))
                folderStats[key] = new Dictionary<string, List<string>>();

            if (!folderStats[key].ContainsKey(value))
                folderStats[key][value] = new List<string>();

            folderStats[key][value].Add(assetPath);
        }

        private string GetPlatformName(BuildTargetGroup platform)
        {
            switch (platform)
            {
                case BuildTargetGroup.Android: return "Android";
                case BuildTargetGroup.iOS: return "iOS";
                case BuildTargetGroup.Standalone: return "Standalone";
                case BuildTargetGroup.Unknown: return "Default";
                default: return platform.ToString();
            }
        }

        // 获取 TextureImporter 平台属性值，如果平台没有 override，则继承 Default
        private string GetTextureImporterPlatformValue(TextureImporter texImporter, BuildTargetGroup platform, string property)
        {
            string platformName = GetPlatformName(platform);
            TextureImporterPlatformSettings settings = texImporter.GetPlatformTextureSettings(platformName);

            // 如果当前平台没有 override，则尝试读取 Default 平台值
            if (!settings.overridden)
            {
                TextureImporterPlatformSettings defaultSettings = texImporter.GetPlatformTextureSettings("DefaultTexturePlatform");
                if (defaultSettings.overridden)
                    settings = defaultSettings;
            }

            switch (property)
            {
                case "maxTextureSize":
                    return settings.maxTextureSize.ToString();
                case "format":
                    return settings.overridden ? settings.format.ToString() : texImporter.textureCompression.ToString();
                default:
                    return "";
            }
        }
        
    }
}
