using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace XKT.ShaderVariantLogger
{
    /// <summary>
    /// Shader Pass 与 LightMode 标签转换器
    /// </summary>
    public class ShaderPassLightModeConverter
    {
        private static Dictionary<Shader, ShaderPassLightModeDictionary> s_ShaderDictionary;

        public static string GetLightModeByPassName(Shader shader, string pass)
        {
            if (s_ShaderDictionary == null)
            {
                s_ShaderDictionary = new Dictionary<Shader, ShaderPassLightModeDictionary>();
            }

            string lightMode;
            
            ShaderPassLightModeDictionary dictionary;
            if (s_ShaderDictionary.TryGetValue(shader, out dictionary))
            {
                lightMode = dictionary.GetLightMode(pass);
                return lightMode;
            }

            dictionary = new ShaderPassLightModeDictionary(shader);
            s_ShaderDictionary.Add(shader, dictionary);

            lightMode = dictionary.GetLightMode(pass);
            return lightMode;
        }
    }

    internal class ShaderPassLightModeDictionary
    {
        private Shader m_targetShader;
        
        private Dictionary<string, string> m_passToLightMode;
        private List<Dictionary<string, string>> m_passBySubShader;
        
        private Dictionary<string, string> m_currentSubShaderWork;

        public string GetLightMode(string pass)
        {
            if (m_passToLightMode == null)
            {
                return "";
            }

            string result;
            if (m_passToLightMode.TryGetValue(pass, out result))
            {
                return result;
            }

            return "";
        }


        public ShaderPassLightModeDictionary(Shader shader)
        {
            Init(shader);
        }

        private void Init(Shader shader)
        {
            m_targetShader = shader;
            
            m_passToLightMode = new Dictionary<string, string>();
            m_passBySubShader = new List<Dictionary<string, string>>();
            
            if (m_targetShader)
            {
                var serializedObject = new SerializedObject(m_targetShader);
                
                SerializedProperty subShadersProp = serializedObject.FindProperty("m_ParsedForm.m_SubShaders");
                int subShaderNum = subShadersProp.arraySize;
                for (int i = 0; i < subShaderNum; ++i)
                {
                    m_currentSubShaderWork = new Dictionary<string, string>();
                    
                    var currentSubShaderProp = subShadersProp.GetArrayElementAtIndex(i);
                    ExecSubShaderProp(currentSubShaderProp, i);
                    
                    m_passBySubShader.Add(m_currentSubShaderWork);
                }
            }
        }

        private void ExecSubShaderProp(SerializedProperty prop, int subIdx)
        {
            if (prop == null) return;

            var passesProp = prop.FindPropertyRelative("m_Passes");
            int passCnt = passesProp.arraySize;
            for (int i = 0; i < passCnt; ++i)
            {
                var currentPassProp = passesProp.GetArrayElementAtIndex(i);
                ExecPassProp(currentPassProp, subIdx, i);
            }
        }

        private void ExecPassProp(SerializedProperty prop, int subIdx, int passIdx)
        {
            if (prop == null) return;

            string pass = GetPassName(m_targetShader, subIdx, passIdx);
            string lightMode = GetLightMode(prop);
            
            if (!m_passToLightMode.TryAdd(pass, lightMode))
            {
                Debug.Log("已存在: " + pass + " ,LightMode: " + lightMode);
            }
            if (!m_currentSubShaderWork.TryAdd(pass, lightMode))
            {
                Debug.LogWarning("已存在 SubShader Idx: " + subIdx + "::" + pass + " ,LightMode: " + lightMode);
            }
        }

        private string GetPassName(Shader shader, int subIdx, int passIdx)
        {
            var data = ShaderUtil.GetShaderData(shader);
            if (data == null)
            {
                return "";
            }

            var sub = data.GetSubshader(subIdx);
            if (sub == null)
            {
                return "";
            }

            // Universal Render Pipeline/Lit 着色器会超 6 的边界
            if (passIdx >= sub.PassCount)
            {
                return "";
            }
            
            var pass = sub.GetPass(passIdx);
            if (pass == null)
            {
                return "";
            }
            
            return pass.Name;
        }

        private string GetLightMode(SerializedProperty prop)
        {
            const string k_lightMode = "LIGHTMODE";
            string str;

            var sp_tt = prop.FindPropertyRelative("m_Tags.tags");
            str = GetTagValue(sp_tt, k_lightMode);
            if (!string.IsNullOrEmpty(str))
            {
                return str;
            }

            var sp_stt = prop.FindPropertyRelative("m_State.m_Tags.tags");
            str = GetTagValue(sp_stt, k_lightMode);
            return str;
        }

        private string GetTagValue(SerializedProperty prop, string key)
        {
            if (prop == null) return null;

            key = key.ToUpper();
            
            int tagsCount = prop.arraySize;
            for (int i = 0; i < tagsCount; ++i)
            {
                var tagInfo = prop.GetArrayElementAtIndex(i);

                var firstProp = tagInfo.FindPropertyRelative("first");
                if (key == firstProp.stringValue.ToUpper().Trim())
                {
                    var secondProp = tagInfo.FindPropertyRelative("second");
                    return secondProp.stringValue;
                }
            }

            return null;
        }
        
    }
}