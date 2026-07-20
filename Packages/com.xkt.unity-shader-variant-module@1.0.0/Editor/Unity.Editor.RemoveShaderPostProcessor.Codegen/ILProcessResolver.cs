using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using Mono.Cecil;

namespace XKT.ShaderVariantStripping.RemoveShaderPostProcessor
{
    // 参考 Netcode 的 GameObject 代码解析逻辑
    internal class ILProcessResolver : IAssemblyResolver
    {
        private DefaultAssemblyResolver m_defaultAssemblyResolver; // 默认的程序集解析器
        
        private readonly ICompiledAssembly m_compiledAssembly;
        private readonly string[] m_assemblyReferences;
        
        private readonly Dictionary<string, AssemblyDefinition> m_assemblyCache = new(); // 缓存已经加载过的程序集，避免重复加载
        private AssemblyDefinition m_selfAssembly;
        
        public ILProcessResolver(ICompiledAssembly compiledAssembly)
        {
            m_defaultAssemblyResolver = new DefaultAssemblyResolver();
            
            m_compiledAssembly = compiledAssembly;
            m_assemblyReferences = compiledAssembly.References;
            
            // System.IO.File.AppendAllText("debuglog.txt", "Start::" + compiledAssembly.Name + "(" + compiledAssembly.References.Length + "\n");
        }

        public void Dispose()
        {
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            try
            {
                var val = m_defaultAssemblyResolver.Resolve(name);
                if (val != null)
                {
                    return val;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{nameof(Resolve)} 发生异常: {ex}");
            }

            return Resolve(name, new ReaderParameters(ReadingMode.Deferred));
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            try
            {
                var val = m_defaultAssemblyResolver.Resolve(name, parameters);
                if (val != null)
                {
                    return val;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{nameof(Resolve)} 发生异常: {ex}");
            }

            lock (m_assemblyCache)
            {
                if (name.Name == m_compiledAssembly.Name)
                {
                    return m_selfAssembly;
                }

                string fileName = FindFile(name);
                if (string.IsNullOrEmpty(fileName))
                {
                    return null;
                }

                var lastWriteTime = File.GetLastWriteTime(fileName);
                var cacheKey = $"{fileName}{lastWriteTime}";
                if (m_assemblyCache.TryGetValue(cacheKey, out AssemblyDefinition result))
                {
                    return result;
                }

                parameters.AssemblyResolver = this;

                // 程序的2进制数据
                var peMs = MemoryStreamFor(fileName);
                
                // 调试符号文件
                var pdbPath = $"{fileName}.pdb";
                if (File.Exists(pdbPath))
                {
                    var pdbMs = MemoryStreamFor(pdbPath);
                    parameters.SymbolStream = pdbMs;
                }

                var assemblyDefinition = AssemblyDefinition.ReadAssembly(peMs, parameters);
                m_assemblyCache.Add(cacheKey, assemblyDefinition);

                return assemblyDefinition;
            }
        }

        
        public void SetSelfAssembly(AssemblyDefinition assemblyDefinition)
        {
            m_selfAssembly = assemblyDefinition;
        }
        
        private string FindFile(AssemblyNameReference name)
        {
            var fileName = m_assemblyReferences.FirstOrDefault(path => Path.GetFileName(path) == $"{name.Name}.dll");
            return fileName;
        }

        private static MemoryStream MemoryStreamFor(string fileName)
        {
            // 读取2进制文件
            MemoryStream ReadBytes()
            {
                using var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                
                byte[] byteArray = new byte[fileStream.Length];
                int readLength = fileStream.Read(byteArray, 0, (int)fileStream.Length);
                if (readLength != fileStream.Length)
                {
                    throw new InvalidOperationException("文件读取长度不是文件的完整长度。");
                }

                return new MemoryStream(byteArray);
            }
            
            MemoryStream ms = Retry(10, TimeSpan.FromSeconds(1), ReadBytes); // 重试10次，每次间隔1秒
            return ms;
        }

        private static MemoryStream Retry(int retryCount, TimeSpan waitTime, Func<MemoryStream> func)
        {
            try
            {
                return func();
            }
            catch (IOException)
            {
                if (retryCount == 0)
                {
                    throw;
                }

                string log = $"捕获的 IO 异常，继续尝试 {retryCount} 次";
                Debug.Log(log);
                
                Thread.Sleep(waitTime);

                return Retry(retryCount - 1, waitTime, func);
            }
        }
        
    }
}