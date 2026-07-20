// Created by: WangYu   Date: 2025-10-21

using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace XKT.ShaderVariantStripping
{
    public partial class StrippingByVariantCollection
    {
        private bool IsExistsVariant(Dictionary<Shader, HashSet<ShaderVariantsInfo>> shaderVariants, Shader shader)
        {
            if (shaderVariants.TryGetValue(shader, out HashSet<ShaderVariantsInfo> variantsHashSet))
            {
                return (variantsHashSet.Count > 0);
            }

            return false;
        }
        
        private bool IsExistsVariant(
            HashSet<ShaderVariantsInfo> variantsHashSet,
            Shader shader, ShaderSnippetData snippetData, ShaderCompilerData compilerData,
            ShaderKeywordMaskGetterPerSnippet maskGetter)
        {
            var keywords = compilerData.shaderKeywordSet.GetShaderKeywords();
            var stringKeywords = Convert(keywords);

            // 没关键字
            if (stringKeywords.Count == 0)
            {
                return true;
            }
            
            if (variantsHashSet == null)
            {
                Debug.LogError($"{nameof(variantsHashSet)} 是 null");
                return false;
            }

            var targetInfo = new ShaderVariantsInfo(shader, snippetData.passType, stringKeywords.ToArray());
            
            bool flag = variantsHashSet.Contains(targetInfo);
            if (!flag && EditorStripShaderVariantConfig.IgnoreStageOnlyKeyword)
            {
                bool isRemoved = RemoveStageOnlyKeyword(stringKeywords, maskGetter);
                if (isRemoved)
                {
                    // 仅限程序关键字
                    if (stringKeywords.Count == 0)
                    {
                        return true;
                    }
                    
                    flag |= variantsHashSet.Contains(targetInfo);
                }
            }

            return flag;
        }

        private bool RemoveStageOnlyKeyword(List<string> compiledKeyword, ShaderKeywordMaskGetterPerSnippet maskGetter)
        {
            var removeIndex = new List<int>();
            
            for (int i = 0, count = compiledKeyword.Count; i < count; ++i)
            {
                var keyword = compiledKeyword[i];
                if (maskGetter.IsThisProgramTypeOnlyKeyword(keyword))
                {
                    removeIndex.Add(i);
                }
            }

            bool result = (removeIndex.Count > 0);
            return result;
        }

        private List<string> Convert(ShaderKeyword[] keywords)
        {
            int len = keywords.Length;
            var converted = new List<string>(len);
            
            for (int i = 0; i < len; ++i)
            {
                string keywordName = keywords[i].name;
                if (string.IsNullOrEmpty(keywordName))
                {
                    continue;
                }
                
                converted.Add(keywordName);
            }

            converted.Sort();

            return converted;
        }

        private HashSet<ShaderVariantsInfo> CreateCurrentStageVariantsInfo(HashSet<ShaderVariantsInfo> originHashSet, ShaderKeywordMaskGetterPerSnippet maskGetter)
        {
            if (!maskGetter.HasCutoffKeywords())
            {
                return originHashSet;
            }

            var copyData = new HashSet<ShaderVariantsInfo>(originHashSet, new ShaderVariantInfoComparer());
            foreach (var info in originHashSet)
            {
                var newKeywords = maskGetter.ConvertValidOnlyKeywords(info.keywords);
                if (newKeywords == null)
                {
                    continue;
                }

                var newVariant = new ShaderVariantsInfo(info.shader, info.passType, newKeywords);
                copyData.Add(newVariant);
            }

            return copyData;
        }
        
    }
}