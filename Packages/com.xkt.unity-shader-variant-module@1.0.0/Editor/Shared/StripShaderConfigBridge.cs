// Created by: WangYu   Date: 2025-10-20

namespace XKT.ShaderVariantStripping.Shared
{
    /// <summary>
    /// 共享 StripShaderConfig 的桥梁
    /// </summary>
    public static class StripShaderConfigBridge
    {
        /// <summary>
        /// 应该移除其它的着色器处理器
        /// </summary>
        public static bool ShouldRemoveOthers { get; set; }
        
    }
}