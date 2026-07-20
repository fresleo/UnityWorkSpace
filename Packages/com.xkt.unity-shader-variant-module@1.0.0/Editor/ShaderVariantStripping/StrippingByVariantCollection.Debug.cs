// Created by: WangYu   Date: 2025-10-21

using System.Collections.Generic;
using System.Text;
using UnityEditor.Rendering;
using UnityEngine;

namespace XKT.ShaderVariantStripping
{
    public partial class StrippingByVariantCollection
    {
        // 调试着色器变体的剥离
        private void DebugStrippingShaderVariant(string header, Shader shader, ShaderSnippetData data, HashSet<ShaderVariantsInfo> variants)
        {
            return;
            
            var stringBuilder = new StringBuilder(1024);
            
            stringBuilder.Append("--------------- ").Append(header).AppendLine(" ---------------");
            stringBuilder.Append("Shader: ").AppendLine(shader.name);
            stringBuilder.Append("SubShader-Pass: ").Append(data.pass.SubshaderIndex).Append("-").Append(data.pass.PassIndex).Append("\n");
            stringBuilder.Append("PassType: ").Append(data.passType).Append("\n");
            stringBuilder.Append("ShaderType: ").Append(data.shaderType).Append("\n");
            stringBuilder.Append("KeywordList: ").Append(variants.Count).Append("\n");
            
            foreach (var info in variants)
            {
                foreach (var keyword in info.keywords)
                {
                    stringBuilder.Append(keyword).Append(" ");
                }
                stringBuilder.Append("\n");
            }
            stringBuilder.Append("\n");
            
            System.IO.File.AppendAllText("variant_log.log", stringBuilder.ToString());
        }
        
    }
}