#if ENABLE_KILL_OTHER_SHADER_STRIPPER

using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using Unity.CompilationPipeline.Common.Diagnostics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using XKT.ShaderVariantStripping.Shared;

namespace XKT.ShaderVariantStripping.RemoveShaderPostProcessor
{
    /// <summary>
    /// 移除其它的着色器处理器
    /// </summary>
    public class ILRemoveOtherShaderProcessor : ILPostProcessor
    {
        public override ILPostProcessor GetInstance()
        {
            return this;
        }

        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            if (!StripShaderConfigBridge.ShouldRemoveOthers)
            {
                return false;
            }

            // System.IO.File.AppendAllText("debug.txt", "Compile:" + compiledAssembly.Name + "\n");
            
            if (!compiledAssembly.Name.StartsWith("Unity.") &&
                !compiledAssembly.Name.StartsWith("UnityEngine.") &&
                !compiledAssembly.Name.StartsWith("UnityEditor."))
            {
                return false;
            }

            // 排除剔除插件本身
            if (compiledAssembly.Name == "UTJ.StripVariant")
            {
                return false;
            }
            
            bool isForEditor = compiledAssembly.Defines?.Contains("UNITY_EDITOR") == true;
            
            // Debug 因为在类加载之前调用会导致崩溃
            // Debug.Log("ILPostProcess " + compiledAssembly.Name + "::" + isForEditor);
            // System.IO.File.WriteAllText(compiledAssembly.Name + ".txt", "AA:" + compiledAssembly.Name);
            
            if (!isForEditor)
            {
                return false;
            }

            return true;
        }

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            try
            {
                return ProcessBody(compiledAssembly);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"处理 IL 发生异常: \n{ex}");

                // System.IO.File.WriteAllText("" + compiledAssembly.Name + "_err.txt", ex.Message + "\n" + ex.Source + "\n" + ex.StackTrace );
                return new ILPostProcessResult(null, new List<DiagnosticMessage>());
            }
        }

        private ILPostProcessResult ProcessBody(ICompiledAssembly compiledAssembly)
        {
            var resolver = new ILProcessResolver(compiledAssembly);
            var readerParameters = new ReaderParameters
            {
                AssemblyResolver = resolver,
                SymbolStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData),
                SymbolReaderProvider = new PortablePdbReaderProvider(),
                ReadingMode = ReadingMode.Immediate,
            };

            // 读取分析程序集
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(new MemoryStream(compiledAssembly.InMemoryAssembly.PeData), readerParameters);
            resolver.SetSelfAssembly(assemblyDefinition);

            // 从程序集中移除目标程序
            bool isModified = RemoveImpFromAssemblyDefinition(assemblyDefinition);
            if (isModified)
            {
                var peStream = new MemoryStream();
                var pdbStream = new MemoryStream();
                var writeParameters = new WriterParameters
                {
                    SymbolWriterProvider = new PortablePdbWriterProvider(),
                    WriteSymbols = true,
                    SymbolStream = pdbStream
                };

                assemblyDefinition.Write(peStream, writeParameters);
                peStream.Flush();
                pdbStream.Flush();

                var peData = peStream.ToArray();
                var pdbData = pdbStream.ToArray();

                return new ILPostProcessResult(new InMemoryAssembly(peData, pdbData), new List<DiagnosticMessage>());
            }

            return new ILPostProcessResult(null, new List<DiagnosticMessage>());
        }
        
        private bool RemoveImpFromAssemblyDefinition(AssemblyDefinition assemblyDefinition)
        {
            if (assemblyDefinition == null)
            {
                return false;
            }

            var flag = false;
            foreach (var module in assemblyDefinition.Modules)
            {
                flag |= RemoveImpFromModuleDefinition(module);
            }

            return flag;
        }

        private bool RemoveImpFromModuleDefinition(ModuleDefinition module)
        {
            bool flag = false;
            
            foreach (var type in module.Types)
            {
                InterfaceImplementation removeInterface = null;
                
                foreach (var inter in type.Interfaces)
                {
                    if (inter.InterfaceType.FullName == nameof(UnityEditor.Build.IPreprocessShaders))
                    {
                        removeInterface = inter;
                        // System.IO.File.AppendAllText("debug.txt", "Remove:" + type.FullName + "\n");
                        // ToEmptyMethod(type);
                        flag = true;
                        break;
                    }
                }

                if (removeInterface != null)
                {
                    type.Interfaces.Remove(removeInterface);
                }
            }

            return flag;
        }

#if false
        private void ToEmptyMethod(TypeDefinition typeDefinition)
        {
            foreach (var method in typeDefinition.Methods)
            {
                if(method.Name == "OnProcessShader")
                {
                    var processor = method.Body.GetILProcessor();

                    // 执行 Clear 时，运行时在 mono_jit 上会出问题...
                    method.Body.Instructions.Clear();

                    method.Body.Instructions.Insert(0, processor.Create(OpCodes.Ret));
                    method.Body.Instructions.Insert(1, processor.Create(OpCodes.Nop));
                }
            }
        }
#endif
    }
}

#endif // ENABLE_KILL_OTHER_SHADER_STRIPPER