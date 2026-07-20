using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MaterialInspectorExtensionTool.Editor.SelectTextureTool
{
    public class SelectTextureWindowData : ScriptableObject
    {
        [FormerlySerializedAs("TextureSize")] public float textureSize;
        
        [FormerlySerializedAs("NowMaterial")] public Material nowMaterial;
        [FormerlySerializedAs("NowTextruePropertyName")] public string nowTextruePropertyName;

        [FormerlySerializedAs("Paths")] public List<string> paths = new();
        [FormerlySerializedAs("Names")] public List<string> names = new();

        [FormerlySerializedAs("Materials")] public List<Material> materials = new();
        
        
        [FormerlySerializedAs("SeachString")] public List<string> seachString;
        
        [FormerlySerializedAs("WindowBackgroundColor")] public Color windowBackgroundColor = Color.black;
        [FormerlySerializedAs("WindowBackgroundTexture")] public Texture windowBackgroundTexture;
        
        [FormerlySerializedAs("SelectColor")] public Color selectColor = Color.yellow;
        
        [FormerlySerializedAs("SplitSize")] public float splitSize;

        
        public Dictionary<int, bool> textureSizeTypes = new();
    }
}