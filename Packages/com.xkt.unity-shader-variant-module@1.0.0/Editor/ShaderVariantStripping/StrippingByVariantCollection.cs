
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Build;
using UnityEngine.Rendering;
using System.Text;

namespace XKT.ShaderVariantStripping
{
    /// <summary>
    /// 按变体集合剥离
    /// </summary>
    public partial class StrippingByVariantCollection : IPreprocessShaders
    {
        /// <summary>
        /// 排序着色器关键字用的比较器
        /// </summary>
        private class SortShaderKeywordComparer : IComparer<ShaderKeyword>
        {
            public int Compare(ShaderKeyword left, ShaderKeyword right)
            {
                string leftName = left.name;
                string rightName = right.name;
                return leftName.CompareTo(rightName);
            }
        }

        /// <summary>
        /// 着色器变体信息
        /// </summary>
        private struct ShaderVariantsInfo
        {
            public Shader shader;
            public PassType passType;
            public string[] keywords;
            
            private List<ShaderKeyword> m_shaderKeywordList;

            public List<string> keywordsForCheck;

            public ShaderVariantsInfo(Shader _shader, PassType _pass, string[] words)
            {
                this.shader = _shader;
                this.passType = _pass;
                this.keywords = words;
                
                int wordsLength = 0;
                if (this.keywords != null)
                {
                    wordsLength = this.keywords.Length;
                }
                
                m_shaderKeywordList = new List<ShaderKeyword>(wordsLength);
                if (this.keywords != null)
                {
                    for (int i = 0; i < wordsLength; ++i)
                    {
                        string word = this.keywords[i];
                        if (string.IsNullOrEmpty(word))
                        {
                            continue;
                        }

                        var shKeyword = new ShaderKeyword(this.shader, word);
                        m_shaderKeywordList.Add(shKeyword);
                    }
                }

                keywordsForCheck = new List<string>();
                foreach (var item in m_shaderKeywordList)
                {
                    // 这里的 name 就是上面的 word
                    if (string.IsNullOrEmpty(item.name))
                    {
                        continue;
                    }
                    
                    keywordsForCheck.Add(item.name);
                }
                keywordsForCheck.Sort();
            }
        }

        /// <summary>
        /// 着色器信息比较器
        /// </summary>
        private class ShaderVariantInfoComparer : IEqualityComparer<ShaderVariantsInfo>
        {
            public bool Equals(ShaderVariantsInfo left, ShaderVariantsInfo right)
            {
                if (left.shader != right.shader)
                {
                    return false;
                }

                if (left.passType != right.passType)
                {
                    return false;
                }

                // 可能是没有关键字的着色器
                if (left.keywordsForCheck == null && right.keywordsForCheck == null)
                {
                    return true;
                }
                // 单方面为空，那就是不相等了
                if (left.keywordsForCheck == null)
                {
                    return false;
                }
                if (right.keywordsForCheck == null)
                {
                    return false;
                }

                if (left.keywordsForCheck.Count != right.keywordsForCheck.Count)
                {
                    return false;
                }
                
                for (int i = 0, max = left.keywordsForCheck.Count; i < max; ++i)
                {
                    if (left.keywordsForCheck[i] != right.keywordsForCheck[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            public int GetHashCode(ShaderVariantsInfo obj)
            {
                int hashCode = 0;
                
                if (obj.shader != null)
                {
                    hashCode += obj.shader.GetHashCode();
                }

                hashCode += obj.passType.GetHashCode();
                
                if (obj.keywordsForCheck != null)
                {
                    foreach (var keyword in obj.keywordsForCheck)
                    {
                        hashCode += keyword.GetHashCode();
                    }
                }

                return hashCode;
            }
        }

        
        // 单例
        private static StrippingByVariantCollection s_instance;

        private bool m_isInitialized;
        private List<ShaderCompilerData> m_compileResultBuffer;
        private Dictionary<Shader, HashSet<ShaderVariantsInfo>> m_shaderVariants;
        
        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            // 强制确保实例存在
            if (s_instance == null)
            {
                new StrippingByVariantCollection();
                Debug.Log($"确保 {nameof(StrippingByVariantCollection)} 实例存在");
            }
        }
        
        // 构造是 public 的，unity 才能调到
        public StrippingByVariantCollection()
        {
            s_instance = this;
            Initialize();
        }

        /// <summary>
        /// 重新初始化
        /// </summary>
        public static void ResetInitialize()
        {
            if (s_instance != null)
            {
                s_instance.m_isInitialized = false;
            }
        }

        private void Initialize()
        {
            // 避免重复初始化
            if (m_isInitialized) return;
            
            m_compileResultBuffer = new List<ShaderCompilerData>(1024);
            m_shaderVariants = new Dictionary<Shader, HashSet<ShaderVariantsInfo>>();
            
            var psvcs = GetProjectShaderVariantCollections();
            foreach (var psvc in psvcs)
            {
                CollectVariants(m_shaderVariants, psvc);
            }

            InitLogInfo();
            if (EditorStripShaderVariantConfig.IsLogEnable)
            {
                SaveProjectVariants();
            }

            m_isInitialized = true;
        }

        // 获取项目中的变体集合
        private static List<ShaderVariantCollection> GetProjectShaderVariantCollections()
        {
            var collections = new List<ShaderVariantCollection>();
            
            // 收集所有的变体集合
            var guids = AssetDatabase.FindAssets("t:ShaderVariantCollection");
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var obj = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(assetPath);
                if(obj == null) continue;
                
                collections.Add(obj);
            }

            // 把需要排除的去掉
            var excludeList = EditorStripShaderVariantConfig.GetExcludeVariantCollectionAsset();
            foreach (var exclude in excludeList)
            {
                if(exclude == null) continue;
                
                collections.Remove(exclude);
            }

            return collections;
        }

        // 收集变体
        private void CollectVariants(Dictionary<Shader, HashSet<ShaderVariantsInfo>> variants, ShaderVariantCollection variantCollection)
        {
            var so = new SerializedObject(variantCollection);
            
            var shadersProp = so.FindProperty("m_Shaders");
            if(shadersProp == null) return;
            
            for (int i = 0; i < shadersProp.arraySize; ++i)
            {
                var shaderProp = shadersProp.GetArrayElementAtIndex(i);
                if(shaderProp == null) continue;

                var shader = shaderProp.FindPropertyRelative("first").objectReferenceValue as Shader;
                if(shader == null) continue;
                
                var variantsProp = shaderProp.FindPropertyRelative("second.variants");
                if(variantsProp == null) continue;

                CollectVariants(variants, shader, variantsProp);
            }
        }

        private void CollectVariants(Dictionary<Shader, HashSet<ShaderVariantsInfo>> variants, Shader shader, SerializedProperty variantsProp)
        {
            if (!variants.TryGetValue(shader, out HashSet<ShaderVariantsInfo> targetHashset))
            {
                targetHashset = new HashSet<ShaderVariantsInfo>(new ShaderVariantInfoComparer());
                variants.Add(shader, targetHashset);
            }

            for (int i = 0; i < variantsProp.arraySize; ++i)
            {
                var variantProp = variantsProp.GetArrayElementAtIndex(i);
                
                var keywords = variantProp.FindPropertyRelative("keywords").stringValue;
                var passType = variantProp.FindPropertyRelative("passType").intValue;

                keywords = keywords?.Trim();

                string[] keywordsArray = null;
                if (string.IsNullOrEmpty(keywords))
                {
                    keywordsArray = new string[] { "" };
                }
                else
                {
                    keywordsArray = keywords.Split(' ');
                }

                var variant = new ShaderVariantsInfo(shader, (PassType)passType, keywordsArray);
                targetHashset.Add(variant);
            }
        }

        
        /// <summary>
        /// 回调顺序
        /// </summary>
        public int callbackOrder => EditorStripShaderVariantConfig.Order;

        /// <summary>
        /// IPreprocessShaders 接口提供的方法，用于在着色器编译前处理变体剥离
        /// </summary>
        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> shaderCompilerData)
        {
            // Debug.Log($"{nameof(StrippingByVariantCollection)}.{nameof(OnProcessShader)} : {shader.name}");
            
            Initialize();

            if (!EditorStripShaderVariantConfig.IsEnable)
            {
                LogAllInVariantCollection(shader, snippet, shaderCompilerData);
                return;
            }

            double startTime = EditorApplication.timeSinceStartup;
            int startVariants = shaderCompilerData.Count;

            var maskGetter = new ShaderKeywordMaskGetterPerSnippet(shader, snippet);
            if (EditorStripShaderVariantConfig.IgnoreStageOnlyKeyword)
            {
                maskGetter.ConstructOnlyKeyword();
            }
            
            m_includeVariantsBuffer.Length = 0;
            m_excludeVariantsBuffer.Length = 0;

            // 检查是否是在项目中的变体集合中出现过的 shader
            bool isExistsVariant = IsExistsVariant(m_shaderVariants, shader);
            if (!isExistsVariant)
            {
                if (EditorStripShaderVariantConfig.StrictVariantStripping)
                {
                    LogNotInVariantCollection(shader, snippet, shaderCompilerData);
                    shaderCompilerData.Clear();
                }
                else
                {
                    LogAllInVariantCollection(shader, snippet, shaderCompilerData);
                }

                return;
            }
            
            // isExistsVariant = true 时，才能走到这里
            if (m_shaderVariants.TryGetValue(shader, out HashSet<ShaderVariantsInfo> variantsHashSet))
            {
                DebugStrippingShaderVariant("Before", shader, snippet, variantsHashSet);
                variantsHashSet = CreateCurrentStageVariantsInfo(variantsHashSet, maskGetter);
                DebugStrippingShaderVariant("After", shader, snippet, variantsHashSet);
            }

            // 收集编译结果
            m_compileResultBuffer.Clear();
            for (int i = 0; i < shaderCompilerData.Count; ++i)
            {
                ShaderCompilerData itemData = shaderCompilerData[i];
                
                isExistsVariant = IsExistsVariant(variantsHashSet, shader, snippet, itemData, maskGetter);

                if (EditorStripShaderVariantConfig.IsLogEnable)
                {
                    var buffer = isExistsVariant ? m_includeVariantsBuffer : m_excludeVariantsBuffer;
                    AppendShaderInfo(buffer, shader, snippet, itemData);
                }

                if (isExistsVariant)
                {
                    m_compileResultBuffer.Add(itemData);
                }
            }

            // 替换着色器编译结果
            shaderCompilerData.Clear();
            foreach (var data in m_compileResultBuffer)
            {
                shaderCompilerData.Add(data);
            }

            // 写执行结果日志
            if (EditorStripShaderVariantConfig.IsLogEnable)
            {
                LogExecuteResult(shader, snippet, shaderCompilerData, startTime, startVariants);

                var data = new ShaderInfoData(shader, snippet);
                if (!m_alreadyWriteShader.Contains(data))
                {
                    LogIncludeAndExcludeBuffer(shader, snippet);
                    LogKeywordMask(maskGetter);
                    m_alreadyWriteShader.Add(data);
                }
            }
        }

        private void LogExecuteResult(
            Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> shaderCompilerData
            , double startTime, int startVariants)
        {
            string dir = k_LogDirectory + "/" + m_dateTimeStr + "/ExecuteLog/";
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            string logFileName = shader.name.Replace("/", "_");
            string logFilePath = System.IO.Path.Combine(dir, logFileName) + ".log";

            var tmpSb = new StringBuilder();

            // 1行 pass 的信息
            tmpSb.Append("Info:").Append(snippet.shaderType);
            tmpSb.Append(" pass").Append(snippet.pass.SubshaderIndex).Append("-").Append(snippet.pass.PassIndex);
            tmpSb.Append(" ").Append(snippet.passType);
            tmpSb.Append(" \"").Append(snippet.passName).Append("\"");
            tmpSb.Append("\n");

            double endTime = EditorApplication.timeSinceStartup;
            int endVariants = shaderCompilerData.Count;
            
            tmpSb.Append("执行时间: ").Append(endTime - startTime).Append(" sec\n");
            tmpSb.Append("变体: ").Append(startVariants).Append("->").Append(endVariants).Append("\n");

            // 写入文件
            System.IO.File.AppendAllText(logFilePath, tmpSb.ToString());
        }

    }
}