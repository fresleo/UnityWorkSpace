// Created By: WangYu  Date: 2024-09-10

using System.Diagnostics;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ShaderHotSwap.Util
{
    public static class CommandLineExecutor
    {
        /// <summary>
        /// 执行命令行命令
        /// </summary>
        public static (string result, string errorMsg) ExecuteCommand(string workingDirectory, string command, string arguments)
        {
            // 进程启动信息
            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory,
            };
            
            // 创建并启动进程
            Process process = new Process
            {
                StartInfo = processStartInfo
            };

            // 注册输出和错误数据接收事件
            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();
            process.OutputDataReceived += (sender, args) => output.AppendLine(args.Data);
            process.ErrorDataReceived += (sender, args) => error.AppendLine(args.Data);
            // 启动进程
            process.Start();
            // 开始异步读取输出和错误数据
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            // 等待进程退出
            process.WaitForExit();
            
            string result = output.ToString().Trim();
            string errorMsg = error.ToString().Trim();
            
            Debug.Log($"命令行结果输出:\n{result}");
            if (!string.IsNullOrEmpty(errorMsg))
            {
                Debug.LogError($"命令行报错:\n{errorMsg}");
            }

            return (result, errorMsg);
        }
    }
}