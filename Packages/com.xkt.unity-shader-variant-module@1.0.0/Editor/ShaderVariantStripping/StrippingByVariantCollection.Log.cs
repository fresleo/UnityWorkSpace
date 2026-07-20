// Created by: WangYu   Date: 2025-10-21

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace XKT.ShaderVariantStripping
{
    public partial class StrippingByVariantCollection
    {
        /// <summary>
        /// 构建日志的目录
        /// 这个路径指向项目的根目录，和 Assets, Library, Temp 在同一级位置
        /// </summary>
        public const string k_LogDirectory = "BuildShaderVariants";
        
        private struct ShaderInfoData : IEquatable<ShaderInfoData>
        {
            public Shader shader;
            public ShaderSnippetData shaderSnippetData;

            public ShaderInfoData(Shader _shader, ShaderSnippetData _shaderSnippetData)
            {
                this.shader = _shader;
                this.shaderSnippetData = _shaderSnippetData;
            }
            
            public bool Equals(ShaderInfoData other)
            {
                if (this.shader != other.shader)
                {
                    return false;
                }
                
                if (this.shaderSnippetData.passName == other.shaderSnippetData.passName &&
                    this.shaderSnippetData.passType == other.shaderSnippetData.passType &&
                    this.shaderSnippetData.shaderType == other.shaderSnippetData.shaderType)
                {
                    return true;
                }
                
                return false;
            }
            
            public override bool Equals(object obj)
            {
                return obj is ShaderInfoData other && Equals(other);
            }
            
            public override int GetHashCode()
            {
                int hashCode = 0;
                if (this.shader != null)
                {
                    hashCode += this.shader.name.GetHashCode();
                }
                hashCode += this.shaderSnippetData.passName.GetHashCode();
                hashCode += this.shaderSnippetData.passType.GetHashCode();
                hashCode += this.shaderSnippetData.shaderType.GetHashCode();
                return hashCode;
            }
        }
        
        // 已经写入日志的
        private HashSet<ShaderInfoData> m_alreadyWriteShader = new();

        private string m_dateTimeStr;

        private StringBuilder m_includeVariantsBuffer, m_excludeVariantsBuffer;
        private StringBuilder m_shaderKeywordBuffer0, m_shaderKeywordBuffer1;
        private StringBuilder m_projectVaritantsBuffer;
        
        private void InitLogInfo()
        {
            m_alreadyWriteShader.Clear();
            
            var dateTime = System.DateTime.Now;
            m_dateTimeStr = dateTime.ToString("yyyyMMdd_HHmmss");
            
            m_includeVariantsBuffer = new StringBuilder(1024);
            m_excludeVariantsBuffer = new StringBuilder(1024);
            m_shaderKeywordBuffer0 = new StringBuilder();
            m_shaderKeywordBuffer1 = new StringBuilder();
            m_projectVaritantsBuffer = new StringBuilder();
        }

        private void SaveProjectVariants()
        {
            var list = new List<ShaderVariantsInfo>(1024);
            
            foreach (var variantHashSet in m_shaderVariants.Values)
            {
                foreach (var val in variantHashSet)
                {
                    list.Add(val);
                }
            }

            int SortFunc(ShaderVariantsInfo left, ShaderVariantsInfo right)
            {
                int shaderName = left.shader.name.CompareTo(right.shader.name);
                if (shaderName != 0)
                {
                    return shaderName;
                }

                int passType = left.passType - right.passType;
                if (passType != 0)
                {
                    return passType;
                }

                int keywordLengthVal = left.keywords.Length - right.keywords.Length;
                if (keywordLengthVal != 0)
                {
                    return keywordLengthVal;
                }

                m_shaderKeywordBuffer0.Length = 0;
                foreach (var keyword in left.keywords)
                {
                    m_shaderKeywordBuffer0.Append(keyword).Append(" ");
                }

                m_shaderKeywordBuffer1.Length = 0;
                foreach (var keyword in right.keywords)
                {
                    m_shaderKeywordBuffer1.Append(keyword).Append(" ");
                }

                string leftStr = m_shaderKeywordBuffer0.ToString();
                string rightStr = m_shaderKeywordBuffer1.ToString();
                return leftStr.CompareTo(rightStr);
            }
            list.Sort(SortFunc);

            string shaderName = null;
            foreach (var variant in list)
            {
                if (shaderName != variant.shader.name)
                {
                    m_projectVaritantsBuffer.Append(variant.shader.name).Append("\n");
                    shaderName = variant.shader.name;
                }

                m_projectVaritantsBuffer.Append(" type: ").Append(variant.passType).Append("\n");
                m_projectVaritantsBuffer.Append(" keyword: ");
                
                foreach (var keyword in variant.keywords)
                {
                    m_projectVaritantsBuffer.Append(keyword).Append(" ");
                }

                m_projectVaritantsBuffer.Append("\n\n");
            }

            string logDir = k_LogDirectory + "/" + m_dateTimeStr;
            if (!System.IO.Directory.Exists(logDir))
            {
                System.IO.Directory.CreateDirectory(logDir);
            }

            string logPath = logDir + "/ProjectVariants.log";
            System.IO.File.WriteAllText(logPath, m_projectVaritantsBuffer.ToString());
        }
        
        
        private void LogAllInVariantCollection(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> shaderCompilerData)
        {
            if (!EditorStripShaderVariantConfig.IsLogEnable) return;

            for (int i = 0; i < shaderCompilerData.Count; ++i)
            {
                AppendShaderInfo(m_includeVariantsBuffer, shader, snippet, shaderCompilerData[i]);
            }

            LogIncludeBuffer(nameof(LogAllInVariantCollection), shader, snippet);
        }

        private void LogNotInVariantCollection(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> shaderCompilerData)
        {
            if (!EditorStripShaderVariantConfig.IsLogEnable) return;

            for (int i = 0; i < shaderCompilerData.Count; ++i)
            {
                AppendShaderInfo(m_excludeVariantsBuffer, shader, snippet, shaderCompilerData[i]);
            }

            LogExcludeBuffer(nameof(LogNotInVariantCollection), shader, snippet);
        }
        
        
        private void LogIncludeBuffer(string head, Shader shader, ShaderSnippetData snippet)
        {
            m_includeVariantsBuffer.Append($"\n================== {head} ==================\n\n\n");
            
            string dir = k_LogDirectory + "/" + m_dateTimeStr + "/IncludeLog/";
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            
            string logFileName = shader.name.Replace("/", "_");
            string logFilePath = System.IO.Path.Combine(dir, logFileName) + ".log";
            
            System.IO.File.AppendAllText(logFilePath, m_includeVariantsBuffer.ToString());
        }

        private void LogExcludeBuffer(string head, Shader shader, ShaderSnippetData snippet)
        {
            m_excludeVariantsBuffer.Append($"\n================== {head} ==================\n\n\n");
            
            string dir = k_LogDirectory + "/" + m_dateTimeStr + "/ExcludeLog/";
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            
            string logFileName = shader.name.Replace("/", "_");
            string logFilePath = System.IO.Path.Combine(dir, logFileName) + ".log";
            
            System.IO.File.AppendAllText(logFilePath, m_excludeVariantsBuffer.ToString());
        }
        
        private void AppendShaderInfo(StringBuilder sb, Shader shader, ShaderSnippetData snippet, ShaderCompilerData compilerData)
        {
            if (sb.Length == 0)
            {
                sb.Append("Shader: " + shader.name).Append("\n");
                sb.Append("ShaderType: ").Append(snippet.shaderType).Append("\n");
                sb.Append("PassName: ").Append(snippet.passName).Append("\n");
                sb.Append("PassType: ").Append(snippet.passType).Append("\n\n");
            }

            var keywords = compilerData.shaderKeywordSet.GetShaderKeywords();

            var sortKeywords = new ShaderKeyword[keywords.Length];
            for (int i = 0; i < keywords.Length; ++i)
            {
                sortKeywords[i] = keywords[i];
            }
            System.Array.Sort(sortKeywords, new SortShaderKeywordComparer());

            sb.Append(" Keyword: ");
            foreach (var keyword in sortKeywords)
            {
                sb.Append(keyword.name).Append(" ");
            }

            sb.Append("\n KeywordType: ");
            foreach (var keyword in sortKeywords)
            {
                if (!ShaderKeyword.IsKeywordLocal(keyword))
                {
                    var keywordType = ShaderKeyword.GetGlobalKeywordType(keyword);
                    sb.Append(keywordType).Append(" ");
                }
                else
                {
                    var localKeyword = new LocalKeyword(shader, keyword.name);
                    sb.Append(localKeyword.type).Append(" ");
                }
            }

            sb.Append("\n LocalkeywordInfo: ");
            foreach (var keyword in sortKeywords)
            {
                if (!ShaderKeyword.IsKeywordLocal(keyword))
                {
                    sb.Append("Global ");
                }
                else
                {
                    var localKeyword = new LocalKeyword(shader, keyword.name);
                    sb.Append(localKeyword.isDynamic ? "Dynamic-" : "Static-");
                    sb.Append(localKeyword.isOverridable ? "isOverridable " : "nonOverridable ");
                }
            }

            sb.Append("\n");
        }
        
        
        private void LogIncludeAndExcludeBuffer(Shader shader, ShaderSnippetData snippet)
        {
            LogIncludeBuffer(nameof(LogIncludeAndExcludeBuffer), shader, snippet);
            LogExcludeBuffer(nameof(LogIncludeAndExcludeBuffer), shader, snippet);
        }

        private void LogKeywordMask(ShaderKeywordMaskGetterPerSnippet maskGetter)
        {
            string maskLogDir = k_LogDirectory + "/" + m_dateTimeStr + "/KeywordLog/";

            if (!System.IO.Directory.Exists(maskLogDir))
            {
                System.IO.Directory.CreateDirectory(maskLogDir);
            }

            string maskLogPath = System.IO.Path.Combine(maskLogDir, maskGetter.LogFileName);
            
            System.IO.File.AppendAllText(maskLogPath, maskGetter.GetLogStr());
        }
        
    }
}