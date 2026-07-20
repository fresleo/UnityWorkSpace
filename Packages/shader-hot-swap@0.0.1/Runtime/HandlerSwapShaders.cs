using System;
using System.Collections.Generic;
using System.Linq;
using ShaderHotSwap.Protocol;
using ShaderHotSwap.Util;
using UnityEngine;

namespace ShaderHotSwap
{
    /// <summary>
    /// 切换着色器处理器
    /// </summary>
    public static class HandlerSwapShaders
    {
        private static string s_logHeader = $"[{nameof(HandlerSwapShaders)}]";
        
        public const string c_url = "/swapShaders";
        
        public static string HandlerMain(string jsonRequest)
        {
            try
            {
                string reqStr = jsonRequest.Substring(0, 128);
                MemoryLogger.Log($"{s_logHeader} 切换着色器 req: {reqStr}");
                
                var req = JsonUtility.FromJson<SwapShadersReq>(jsonRequest);
                var res = new SwapShadersRes();
                SwapShaders(req, res);

                return OkString(res);
            }
            catch(Exception ex)
            {
                return ErrorString(ex.ToString());
            }
        }

        private class TargetMaterial
        {
            public Material mat;
            public List<TargetRenderer> renderers;
        }
        
        private class TargetRenderer
        {
            public Renderer render;
            public int matIndex;
        }

        private static void SwapShaders(SwapShadersReq req, SwapShadersRes res)
        {
            var assetBundle = LoadAssetBundle(req.assetBundleBase64);
            MemoryLogger.Log($"{s_logHeader} 解码加载 AssetBundle = " + assetBundle);
            
            var renderers = Resources.FindObjectsOfTypeAll<Renderer>().Where(x => ! IsAsset(x.gameObject));
            res.shaders = new List<SwappedShader>();

            if (req.shaders == null)
            {
                Debug.LogError($"{s_logHeader} req.shaders == null");
                return;
            }
            
            foreach (var remoteShader in req.shaders)
            {
                if (remoteShader == null)
                {
                    Debug.LogError($"{s_logHeader} remoteShader == null");
                    continue;
                }
                
                var shaderName = remoteShader.name;
                var shaderLoadName = remoteShader.guid;
                var shader = assetBundle.LoadAsset<Shader>(shaderLoadName);
                if (shader == null)
                {
                    Debug.LogError($"{s_logHeader} shader == null, shaderName: {shaderName}");
                    continue;
                }
                
                var targetMaterials = new List<TargetMaterial>();
                
                // 第1步 : 找到材质和渲染器
                foreach (var renderer in renderers)
                {
                    var mats = renderer.sharedMaterials;
                    for (int i = 0; i < mats.Length; ++i)
                    {
                        var mat = mats[i];
                        if (mat == null) continue;
                        if (mat.shader.name != shaderName) continue;
                        
                        var foundMat = targetMaterials.FirstOrDefault(x => x.mat == mat);
                        if (foundMat == null)
                        {
                            foundMat = new TargetMaterial();
                            foundMat.mat = mat;
                            foundMat.renderers = new List<TargetRenderer>();
                            targetMaterials.Add(foundMat);
                        }
                        
                        var render = new TargetRenderer();
                        render.render = renderer;
                        render.matIndex = i;

                        foundMat.renderers.Add(render);
                    }
                }
                
                // 第2步 : 替换材质
                foreach(var targetMat in targetMaterials)
                {
                    var newMat = new Material(targetMat.mat);
                    newMat.shader = shader;
                    newMat.name += " (HotSwapped)";
                    
                    foreach (var targetRender in targetMat.renderers)
                    {
                        MemoryLogger.Log($"{s_logHeader} 替换材质 Shader:{shader.name}, Material:{targetMat.mat.name}, Renderer:{targetRender.render.name}, Material Index:{targetRender.matIndex}");

                        int matLength = targetRender.render.sharedMaterials.Length;
                        if (matLength > 1 && targetRender.matIndex > 0)
                        {
                            var mats = new Material[matLength];
                            for(int i = 0; i < matLength; ++i )
                            {
                                if (i == targetRender.matIndex)
                                {
                                    mats[i] = newMat;
                                }
                                else
                                {
                                    mats[i] = targetRender.render.sharedMaterials[i];
                                }
                            }

                            targetRender.render.materials = mats;
                        }
                        else
                        {
                            targetRender.render.material = newMat;
                        }
                    }
                }
                
                // 第3步 : 记录结果
                var swappedShader = new SwappedShader();
                swappedShader.shader = remoteShader;
                swappedShader.materials = new List<SwappedMaterial>();
                res.shaders.Add(swappedShader);

                foreach (var targetMat in targetMaterials)
                {
                    var swappedMaterial = new SwappedMaterial();
                    swappedMaterial.material = new RemoteMaterial();
                    swappedMaterial.material.name = targetMat.mat.name;
                    swappedMaterial.material.instanceID = targetMat.mat.GetInstanceID();
                    swappedMaterial.renderers = new List<RemoteRenderer>();

                    foreach (var targetRender in targetMat.renderers)
                    {
                        var remoteRenderer = new RemoteRenderer();
                        remoteRenderer.name = targetRender.render.name;
                        remoteRenderer.instanceID = targetRender.render.GetInstanceID();

                        swappedMaterial.renderers.Add(remoteRenderer);
                    }

                    swappedShader.materials.Add(swappedMaterial);
                }
            }
            
            // 卸载 AssetBundle
            assetBundle.Unload(false);
            Resources.UnloadUnusedAssets();
        }

        private static bool IsAsset(GameObject go)
        {
            return go.scene.rootCount == 0;
        }

        private static AssetBundle LoadAssetBundle(string assetBundleBase64)
        {
            var bytes = Convert.FromBase64String(assetBundleBase64);
            var assetBundle = AssetBundle.LoadFromMemory(bytes);
            return assetBundle;
        }
        

        private static string OkString(SwapShadersRes res)
        {
            res.error = string.Empty;
            res.log = MemoryLogger.Flush();
            return JsonUtility.ToJson(res);
        }
        
        private static string ErrorString(string reason)
        {
            var res = new SwapShadersRes();;
            res.error = reason;
            res.log = MemoryLogger.Flush();
            return JsonUtility.ToJson(res);
        }
        
    }
}