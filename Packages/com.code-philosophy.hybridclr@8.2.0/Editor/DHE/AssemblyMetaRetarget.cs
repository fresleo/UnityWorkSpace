using dnlib.DotNet;
using dnlib.DotNet.Writer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HybridCLR.Editor.DHE
{
    public class AssemblyMetaRetarget
    {
        private readonly ModuleContext _modCtx;
        private readonly AssemblyResolver _asmResolver;

        private readonly ModuleDefMD _oldAss;
        private readonly ModuleDefMD _newAss;

        private readonly Regex _burstDirectCallPattern;
        private readonly Regex _postfixBurstDelegatePattern;

        private readonly byte[] _newDllBytes;
        private bool _anyRetargeted = false;

        public AssemblyMetaRetarget(byte[] oldDll, byte[] newDll)
        {
            _newDllBytes = newDll;
            _modCtx = ModuleDef.CreateModuleContext();
            _asmResolver = (AssemblyResolver)_modCtx.AssemblyResolver;
            _asmResolver.EnableTypeDefCache = true;
            _asmResolver.UseGAC = false;

            _oldAss = ModuleDefMD.Load(oldDll, _modCtx);
            _newAss = ModuleDefMD.Load(newDll, _modCtx);

            _burstDirectCallPattern = new Regex(@"(.+)_([0-9A-Fa-f]+)\$BurstDirectCall");
            _postfixBurstDelegatePattern = new Regex(@"(.+)_([0-9A-Fa-f]+)\$PostfixBurstDelegate");
        }


        public void Retarget()
        {
            foreach (var type in _newAss.GetTypes())
            {
                if (type.DefinitionAssembly == null || type.DeclaringType == null)
                {
                    continue;
                }
                string typeName = type.Name;
                Match match = _burstDirectCallPattern.Match(typeName);
                if (match.Success)
                {
                    RetargetType(type, _burstDirectCallPattern);
                    continue;
                }
                match = _postfixBurstDelegatePattern.Match(typeName);
                if (match.Success)
                {
                    RetargetType(type, _postfixBurstDelegatePattern);
                    continue;
                }
                
            }
        }

        private string GetBurstOriginalName(string typeName, Regex pattern)
        {
            Match match = pattern.Match(typeName);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return null;
        }

        private MethodDef GetInvokeMethod(TypeDef type)
        {
            return type.Methods.FirstOrDefault(m => m.Name == "Invoke");
        }

        private HashSet<TypeDef> _oldRetargedTypes = new HashSet<TypeDef>();

        private void RetargetType(TypeDef newType, Regex typeNamPat)
        {
            //UnityEngine.Debug.Log($"retarget type:{newType.FullName}");
            string decalringTypeName = newType.DeclaringType.FullName;
            TypeDef oldDeclaringType = _oldAss.Find(decalringTypeName, false);
            if (oldDeclaringType == null)
            {
                UnityEngine.Debug.LogError($"retarget type:{newType}. cant' find declaringType in old assembly");
                return;
            }
            string burstName = GetBurstOriginalName(newType.Name, typeNamPat);
            MethodDef invokeMethod = GetInvokeMethod(newType);
            if (invokeMethod == null)
            {
                UnityEngine.Debug.LogError($"retarget type:{newType.FullName} cant' find Invoke method");
                return;
            }
            //UnityEngine.Debug.Log($"retarget type:{newType.FullName} burstName:{burstName}");
            foreach (var oldType in oldDeclaringType.NestedTypes)
            {
                string subBurstName = GetBurstOriginalName(oldType.Name, typeNamPat);
                if (subBurstName == null || burstName != subBurstName)
                {
                    continue;
                }
                MethodDef subInvokeMethod = GetInvokeMethod(oldType);
                if (subInvokeMethod == null)
                {
                    UnityEngine.Debug.LogError($"retarget type:{newType.FullName} cant' find Invoke method in subType:{oldType.FullName}");
                    continue;
                }
                if (subInvokeMethod.MethodSig.ToString() != invokeMethod.MethodSig.ToString())
                {
                    continue;
                }
                if (!_oldRetargedTypes.Add(oldType))
                {
                    UnityEngine.Debug.LogWarning($"retarget type:{newType.FullName} subType:{oldType.FullName} already retargeted");
                    continue;
                }
                UnityEngine.Debug.Log($"retarget newType:{newType.FullName} ==> oldType:{oldType.FullName}");
                newType.Name = oldType.Name;
                _anyRetargeted = true;
            }
        }

        public void Save(string outputDll)
        {
            if (!_anyRetargeted)
            {
                UnityEngine.Debug.LogWarning($"assembly:{_newAss.Name} no type retargeted");
                File.WriteAllBytes(outputDll, _newDllBytes);
                return;
            }
            UnityEngine.Debug.Log($"assembly:{_newAss.Name} retarget {_oldRetargedTypes.Count} type.");

            var options = new ModuleWriterOptions(_newAss);
            options.MetadataOptions.Flags |= MetadataFlags.PreserveRids;
            _newAss.Write(outputDll, options);
        }
    }
}
