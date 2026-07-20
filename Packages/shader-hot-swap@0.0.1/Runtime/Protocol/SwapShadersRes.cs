using System;
using System.Collections;
using System.Collections.Generic;

namespace ShaderHotSwap.Protocol
{
    [Serializable]
    public class SwapShadersRes
    {
        public string error;
        public string log;
        public List<SwappedShader> shaders;
    }

    [Serializable]
    public class SwappedShader
    {
        public RemoteShader shader;
        public List<SwappedMaterial> materials;
    }

    [Serializable]
    public class SwappedMaterial
    {
        public RemoteMaterial material;
        public List<RemoteRenderer> renderers;
    }
}