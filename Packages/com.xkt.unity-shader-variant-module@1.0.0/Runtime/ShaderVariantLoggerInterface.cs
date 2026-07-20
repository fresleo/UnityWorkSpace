#if UNITY_EDITOR

using System.Runtime.InteropServices;

namespace XKT.ShaderVariantLogger
{
    public class ShaderVariantLoggerInterface
    {
        private const string DllName = "ShaderVariantLogger";

        [DllImport(DllName)]
        private static extern void _ShaderCompileWatcherForEditorSetupFile(string file);
        
        [DllImport(DllName)]
        private static extern System.IntPtr _ShaderCompileWatcherForEditorGetCurrentFile();

        [DllImport(DllName)]
        private static extern void _ShaderCompileWatcherForEditorSetFrame(int idx);

        [DllImport(DllName)]
        private static extern void _ShaderCompileWatcherForEditorSetEnable(bool enable);

        [DllImport(DllName)]
        private static extern bool _ShaderCompileWatcherForEditorGetEnable();

        
        /// <summary>
        /// 设置日志文件路径
        /// </summary>
        public static void SetupFile(string file)
        {
            _ShaderCompileWatcherForEditorSetupFile(file);
        }
        
        /// <summary>
        /// 获取日志文件路径
        /// </summary>
        public static string GetCurrentFile()
        {
            var ptr = _ShaderCompileWatcherForEditorGetCurrentFile();
            return Marshal.PtrToStringAnsi(ptr);
        }

        /// <summary>
        /// 设置当前帧数
        /// </summary>
        public static void SetFrame(int index)
        {
            _ShaderCompileWatcherForEditorSetFrame(index);
        }

        /// <summary>
        /// 启用或禁用
        /// </summary>
        public static void SetEnable(bool flag)
        {
            _ShaderCompileWatcherForEditorSetEnable(flag);
        }

        /// <summary>
        /// 获取启用状态
        /// </summary>
        public static bool GetEnable()
        {
            return _ShaderCompileWatcherForEditorGetEnable();
        }
    }
}

#endif // UNITY_EDITOR
