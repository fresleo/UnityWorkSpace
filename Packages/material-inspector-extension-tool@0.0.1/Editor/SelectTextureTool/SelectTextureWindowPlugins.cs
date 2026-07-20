namespace MaterialInspectorExtensionTool.Editor.SelectTextureTool
{
    public abstract class SelectTextureWindowPlugins
    {
        /// <summary>
        /// 工具名称，在设置面板上显示的名字
        /// </summary>
        public abstract string PluginName { set; get; }

        /// <summary>
        /// 工具提示或简介：设置面板上显示
        /// </summary>
        public abstract string PluginTips { set; get; }

        /// <summary>
        /// 插件启用状态
        /// </summary>
        public abstract bool IsEnable { set; get; }

        /// <summary>
        /// 绘制方法
        /// </summary>
        public abstract void Draw();
    }
}