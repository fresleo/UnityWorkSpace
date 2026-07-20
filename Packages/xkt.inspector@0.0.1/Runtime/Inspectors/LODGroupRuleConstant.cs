/*******************************************************************************
 * File: LODGroupRuleConstant.cs
 * Author: WangYu
 * Date: 2026-05-21
 * Description: 类或文件功能描述
 *
 * Notice: 注意事项描述（无可省略）
 *******************************************************************************/

namespace XKT.Editor.Inspectors
{
    public class LODGroupRuleConstant
    {
        public const string C_LOD0_SUFFIX = "_LOD0";
        public const string C_LOD1_SUFFIX = "_LOD1";
        public const string C_LOD2_SUFFIX = "_LOD2";
        
        public static readonly string[] S_LOD_SUFFIXES =
        {
            C_LOD0_SUFFIX, C_LOD1_SUFFIX, C_LOD2_SUFFIX
        };
        
        public const string C_SHADOW_ONLY_SUFFIX = "_ShadowOnly";
        
    }
}