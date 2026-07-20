using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace XKT.ShaderVariantStripping
{
    /// <summary>
    /// 着色器关键字掩码获取器
    /// </summary>
    public class ShaderKeywordMaskGetter
    {
        private const int FLAG_VERTEX = 0x01;
        private const int FLAG_FRAGMENT = 0x02;
        private const int FLAG_GEOMETRY = 0x04;
        private const int FLAG_HULL = 0x08;
        private const int FLAG_DOMAIN = 0x10;
        private const int FLAG_RAYTRACE = 0x20;

        private Shader m_shader;
        
        private List<string> m_keywords;
        private Dictionary<string, int> m_keywordFlags;

        public ShaderKeywordMaskGetter(Shader _shader)
        {
            m_shader = _shader;
            ConstructFlag();

            // Debug 
            //DebugKeywords();
        }

        public List<string> allKeywords => m_keywords;


        // 构造 Flag
        private void ConstructFlag()
        {
            var serializeObject = new SerializedObject(m_shader);
            
            // m_KeywordNames
            var keywordsProp = serializeObject.FindProperty("m_ParsedForm.m_KeywordNames");
            if (keywordsProp == null || !keywordsProp.isArray)
            {
                return;
            }
            
            CollectKeywordStrings(keywordsProp);

            // m_SubShaders
            var subShadersProp = serializeObject.FindProperty("m_ParsedForm.m_SubShaders");
            if (subShadersProp == null || !subShadersProp.isArray)
            {
                return;
            }

            m_keywordFlags = new Dictionary<string, int>();
            
            int arraySize = subShadersProp.arraySize;
            for (int i = 0; i < arraySize; ++i)
            {
                var subShader = subShadersProp.GetArrayElementAtIndex(i);
                ExecuteSubShader(subShader, i);
            }
        }

        // 收集关键字字符串
        private void CollectKeywordStrings(SerializedProperty keywordsProp)
        {
            int arraySize = keywordsProp.arraySize;
            m_keywords = new List<string>(arraySize);
            for (int i = 0; i < arraySize; ++i)
            {
                var keyword = keywordsProp.GetArrayElementAtIndex(i);
                m_keywords.Add(keyword.stringValue);
            }
        }

        private void ExecuteSubShader(SerializedProperty subShaderProp, int subShaderIdx)
        {
            var passesProp = subShaderProp.FindPropertyRelative("m_Passes");
            if (passesProp == null || !passesProp.isArray)
            {
                return;
            }

            int arraySize = passesProp.arraySize;
            for (int i = 0; i < arraySize; ++i)
            {
                var itemPass = passesProp.GetArrayElementAtIndex(i);
                
                ExecutePass(itemPass, "progVertex", FLAG_VERTEX, subShaderIdx, i);
                ExecutePass(itemPass, "progFragment", FLAG_FRAGMENT, subShaderIdx, i);
                ExecutePass(itemPass, "progGeometry", FLAG_GEOMETRY, subShaderIdx, i);
                ExecutePass(itemPass, "progHull", FLAG_HULL, subShaderIdx, i);
                ExecutePass(itemPass, "progDomain", FLAG_DOMAIN, subShaderIdx, i);
                ExecutePass(itemPass, "progRayTracing", FLAG_RAYTRACE, subShaderIdx, i);
            }
        }

        private void ExecutePass(SerializedProperty passProp, string pgStage, int flag, int subShaderIdx, int passIdx)
        {
            var stageProp = passProp.FindPropertyRelative(pgStage);
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
                var itemMask = masksProp.GetArrayElementAtIndex(i);
                
                int index = itemMask.intValue;
                string keywordName = m_keywords[index];
                
                if (m_keywordFlags.TryGetValue(keywordName, out int flags))
                {
                    m_keywordFlags[keywordName] = flags | flag;
                }
                else
                {
                    m_keywordFlags.Add(keywordName, flag);
                }
            }
        }

        private void DebugKeywords()
        {
            foreach (var keyword in m_keywords)
            {
                string str = keyword + "::";

                if (IsUsedForVertexProgram(keyword))
                {
                    str += "v";
                }
                else
                {
                    str += "-";
                }

                if (IsUsedForFragmentProgram(keyword))
                {
                    str += "f";
                }
                else
                {
                    str += "-";
                }

                Debug.Log(str);
            }
        }


        #region 获取标志

        private bool IsUsedForProgram(string keyword, int flag)
        {
            int val;
            if (m_keywordFlags == null)
            {
                return true;
            }

            if (!m_keywordFlags.TryGetValue(keyword, out val))
            {
                return true;
            }

            return ((val & flag) == flag);
        }
        
        public bool IsUsedForVertexProgram(string keyword)
        {
            return IsUsedForProgram(keyword, FLAG_VERTEX);
        }

        public bool IsUsedForFragmentProgram(string keyword)
        {
            return IsUsedForProgram(keyword, FLAG_FRAGMENT);
        }

        public bool IsUsedForGeometryProgram(string keyword)
        {
            return IsUsedForProgram(keyword, FLAG_GEOMETRY);
        }

        public bool IsUsedForHullProgram(string keyword)
        {
            return IsUsedForProgram(keyword, FLAG_HULL);
        }

        public bool IsUsedForDomainProgram(string keyword)
        {
            return IsUsedForProgram(keyword, FLAG_DOMAIN);
        }

        public bool IsUsedForRaytraceProgram(string keyword)
        {
            return IsUsedForProgram(keyword, FLAG_RAYTRACE);
        }

        #endregion 获取标志
        
    }
}