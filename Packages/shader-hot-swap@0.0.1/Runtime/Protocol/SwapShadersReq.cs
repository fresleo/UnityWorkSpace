using System;
using System.Collections;
using System.Collections.Generic;

namespace ShaderHotSwap.Protocol
{
    [Serializable]
    public class SwapShadersReq
    {
        public List<RemoteShader> shaders;
        public string assetBundleBase64;
    }
}
