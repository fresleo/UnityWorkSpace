using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using System.Text;

namespace XKT.ShaderVariantStripping
{
    /*
    对于 Unity 2022.2 或更新的版本
    变体剔除的行为已经发生了变化
    对于 2022...
    "multi_compile_fragment" 关键字仅出现在片元阶段。
    */
    
    /// <summary>
    /// 着色器关键字 Mask 获取器（每个片段）
    /// </summary>
    public class ShaderKeywordMaskGetterPerSnippet
    {
        // 类型表
        private static readonly string[] s_shaderTypeTable =
        {
            "progVertex",
            "progFragment",
            "progGeometry",
            "progHull",
            "progDomain",
            "progRayTracing"
        };

        private Shader m_shader;
        private ShaderSnippetData m_shaderSnippetData;
        
        private List<string> m_keywords;
        private HashSet<string> m_validKeywords;
        
        private bool m_isExecuteConstructOnlyKeyword;
        private HashSet<string> m_programTypeOnlyKeyword;

        public ShaderKeywordMaskGetterPerSnippet(Shader _shader, ShaderSnippetData _shaderSnippetData)
        {
            m_shader = _shader;
            m_shaderSnippetData = _shaderSnippetData;
            
            int typeIndex = GetTypeIndex(m_shaderSnippetData.shaderType);
            int subShaderIndex = (int)m_shaderSnippetData.pass.SubshaderIndex;
            int passIndex = (int)m_shaderSnippetData.pass.PassIndex;
            
            ConstructValidKeywords(subShaderIndex, passIndex, s_shaderTypeTable[typeIndex]);
        }
        
        private static int GetTypeIndex(ShaderType type)
        {
            switch (type)
            {
                case ShaderType.Vertex:
                    return 0;
                case ShaderType.Fragment:
                    return 1;
                case ShaderType.Geometry:
                    return 2;
                case ShaderType.Hull:
                    return 3;
                case ShaderType.Domain:
                    return 4;
                case ShaderType.RayTracing:
                    return 5;
            }

            return -1;
        }
        
        private void ConstructValidKeywords(int subShaderIndex, int passIndex, string typeStr)
        {
            var serializeObject = new SerializedObject(m_shader);
            
            // 关键字
            var keywordNamesProp = serializeObject.FindProperty("m_ParsedForm.m_KeywordNames");
            if (keywordNamesProp == null || !keywordNamesProp.isArray)
            {
                return;
            }

            CollectKeywordStrings(keywordNamesProp);

            // 子着色器
            var subShadersProp = serializeObject.FindProperty("m_ParsedForm.m_SubShaders");
            if (subShadersProp == null || !subShadersProp.isArray)
            {
                return;
            }

            int arraySize = subShadersProp.arraySize;
            if (subShaderIndex < 0 || subShaderIndex >= arraySize)
            {
                return;
            }

            var subShaderProp = subShadersProp.GetArrayElementAtIndex(subShaderIndex);
            CreateValidKeywordInSubShader(subShaderProp, passIndex, typeStr);
        }

        private void CollectKeywordStrings(SerializedProperty keywordNamesProp)
        {
            int arraySize = keywordNamesProp.arraySize;
            m_keywords = new List<string>(arraySize);
            for (int i = 0; i < arraySize; ++i)
            {
                var keywordNameProp = keywordNamesProp.GetArrayElementAtIndex(i);
                m_keywords.Add(keywordNameProp.stringValue);
            }
        }
        
        private void CreateValidKeywordInSubShader(SerializedProperty subShaderProp, int passIndex, string typeStr)
        {
            var passesProp = subShaderProp.FindPropertyRelative("m_Passes");
            if (passesProp == null || !passesProp.isArray)
            {
                return;
            }

            int arraySize = passesProp.arraySize;
            if (passIndex < 0 || passIndex >= arraySize)
            {
                return;
            }

            var currentPassProp = passesProp.GetArrayElementAtIndex(passIndex);
            CreateValidKeywordInPass(currentPassProp, typeStr);
        }

        private void CreateValidKeywordInPass(SerializedProperty passProp, string typeStr)
        {
            var stageProp = passProp.FindPropertyRelative(typeStr);
            if (stageProp == null)
            {
                return;
            }

            var masksProp = stageProp.FindPropertyRelative("m_SerializedKeywordStateMask");
            if (masksProp == null || !masksProp.isArray)
            {
                return;
            }

            int arraySize = masksProp.arraySize;
            m_validKeywords = new HashSet<string>(arraySize);
            for (int i = 0; i < arraySize; ++i)
            {
                int index = masksProp.GetArrayElementAtIndex(i).intValue;
                string keywordName = m_keywords[index];
                m_validKeywords.Add(keywordName);
            }
        }

        
        #region 搜索其他程序类型

        public void ConstructOnlyKeyword()
        {
            m_isExecuteConstructOnlyKeyword = true;
            
            int typeIndex = GetTypeIndex(m_shaderSnippetData.shaderType);
            int subShaderIndex = (int)m_shaderSnippetData.pass.SubshaderIndex;
            int passIndex = (int)m_shaderSnippetData.pass.PassIndex;
            
            var serializeObject = new SerializedObject(m_shader);
            
            var subShadersProp = serializeObject.FindProperty("m_ParsedForm.m_SubShaders");
            if (subShadersProp == null || !subShadersProp.isArray)
            {
                return;
            }

            int subShadersSize = subShadersProp.arraySize;
            if (subShaderIndex < 0 || subShaderIndex >= subShadersSize)
            {
                return;
            }

            var subShaderProp = subShadersProp.GetArrayElementAtIndex(subShaderIndex);
            var passesProp = subShaderProp.FindPropertyRelative("m_Passes");
            if (passesProp == null || !passesProp.isArray)
            {
                return;
            }

            int passesSize = passesProp.arraySize;
            if (passIndex < 0 || passIndex >= passesSize)
            {
                return;
            }

            var currentPassProp = passesProp.GetArrayElementAtIndex(passIndex);
            var onlyKeywordIndex = ConstructPassKeywordStateMask(currentPassProp, s_shaderTypeTable[typeIndex]);
            for (int i = 0; i < s_shaderTypeTable.Length; i++)
            {
                if (i != typeIndex)
                {
                    RemovePassKeywordStateMaskIfExist(currentPassProp, s_shaderTypeTable[i], onlyKeywordIndex);
                }
            }

            m_programTypeOnlyKeyword = new HashSet<string>();
            foreach (var index in onlyKeywordIndex)
            {
                m_programTypeOnlyKeyword.Add(m_keywords[index]);
            }
        }

        private HashSet<int> ConstructPassKeywordStateMask(SerializedProperty passProp, string typeStr)
        {
            var stageProp = passProp.FindPropertyRelative(typeStr);
            if (stageProp == null)
            {
                return null;
            }

            var masksProp = stageProp.FindPropertyRelative("m_SerializedKeywordStateMask");
            if (masksProp == null || !masksProp.isArray)
            {
                return null;
            }
            
            int arraySize = masksProp.arraySize;
            var passKeywordStateMask = new HashSet<int>(arraySize);
            for (int i = 0; i < arraySize; ++i)
            {
                int index = masksProp.GetArrayElementAtIndex(i).intValue;
                passKeywordStateMask.Add(index);
            }

            return passKeywordStateMask;
        }

        private void RemovePassKeywordStateMaskIfExist(SerializedProperty passProp, string typeStr, HashSet<int> passKeywordStateMask)
        {
            if (passKeywordStateMask == null)
            {
                return;
            }

            var stageProp = passProp.FindPropertyRelative(typeStr);
            if (stageProp == null)
            {
                return;
            }

            var masksProp = stageProp.FindPropertyRelative("m_SerializedKeywordStateMask");
            if (masksProp == null || !masksProp.isArray)
            {
                return;
            }

            int arraySize = masksProp.arraySize;
            for (int i = 0; i < arraySize; ++i)
            {
                int index = masksProp.GetArrayElementAtIndex(i).intValue;
                passKeywordStateMask.Remove(index);
            }
        }

        #endregion 搜索其他程序类型


        public bool HasCutoffKeywords()
        {
            if (m_validKeywords == null)
            {
                return false;
            }

            return (m_validKeywords.Count != m_keywords.Count);
        }

        public string LogFileName
        {
            get
            {
                string shaderPath = m_shader.name.Replace("/", "_");
                string txt = $"{shaderPath}-{m_shaderSnippetData.shaderType}-{m_shaderSnippetData.pass.SubshaderIndex}-{m_shaderSnippetData.pass.PassIndex}.log";
                return txt;
            }
        }

        public string GetLogStr()
        {
            var stringBuilder = new StringBuilder(1024);
            
            stringBuilder.Append("Shader: ").Append(m_shader.name).Append("\n");
            stringBuilder.Append("ShaderType: ").Append(m_shaderSnippetData.shaderType).Append("\n");
            stringBuilder.Append("SubShaderIndex: ").Append(m_shaderSnippetData.pass.SubshaderIndex).Append("\n");
            stringBuilder.Append("PassIndex: ").Append(m_shaderSnippetData.pass.PassIndex).Append("\n");
            stringBuilder.Append("PassType: ").Append(m_shaderSnippetData.passType).Append("\n");
            stringBuilder.Append("PassName: ").Append(m_shaderSnippetData.passName).Append("\n");
            stringBuilder.Append("Keywords: ").Append(m_keywords.Count).Append("\n");
            
            foreach (var keyword in m_keywords)
            {
                bool validFlag = ValidKeyword(keyword);
                stringBuilder.Append("  ").Append(keyword).Append(" : ").Append(validFlag);
                
                if (m_isExecuteConstructOnlyKeyword)
                {
                    bool result = IsThisProgramTypeOnlyKeyword(keyword);
                    stringBuilder.Append("  OnlyThisProgramType: ").Append(result);
                }

                stringBuilder.Append("\n");
            }

            return stringBuilder.ToString();
        }

        public bool ValidKeyword(string keyword)
        {
            if (m_validKeywords == null)
            {
                return true;
            }

            return m_validKeywords.Contains(keyword);
        }

        public bool IsThisProgramTypeOnlyKeyword(string keyword)
        {
            if (m_programTypeOnlyKeyword == null)
            {
                return true;
            }

            return m_programTypeOnlyKeyword.Contains(keyword);
        }

        public string[] ConvertValidOnlyKeywords(string[] keywords)
        {
            if (keywords == null)
            {
                return null;
            }

            int validCount = 0;
            int length = keywords.Length;
            for (int i = 0; i < length; ++i)
            {
                if (ValidKeyword(keywords[i]))
                {
                    validCount++;
                }
            }

            if (validCount == length || validCount == 0)
            {
                return null;
            }

            var newKeywords = new string[validCount];
            int index = 0;
            for (int i = 0; i < length; ++i)
            {
                if (ValidKeyword(keywords[i]))
                {
                    newKeywords[index] = keywords[i];
                    ++index;
                }
            }

            return newKeywords;
        }
        
    }
}