// Created by: WangYu   Date: 2025-09-26

using System;
using UnityEditor;
using UnityEngine;

namespace com.garena.gcommon.engine.tool.assetimporter
{
    internal static class TextureSettingSetter
    {
        private const string c_Prop_maxTextureSize = "maxTextureSize";
        private const string c_Prop_maxTextureSize_NormalMap = c_Prop_maxTextureSize + "_NormalMap";
        private const string c_Prop_maxTextureSize_Shadowmask = c_Prop_maxTextureSize + "_Shadowmask";
        private const string c_Prop_maxTextureSize_Lightmap = c_Prop_maxTextureSize + "_Lightmap";
        private const string c_Prop_maxTextureSize_DirectionalLightmap = c_Prop_maxTextureSize + "_DirectionalLightmap";

        private const string c_Prop_Textureformat = "Textureformat";
        private const string c_Prop_Textureformat_NormalMap = c_Prop_Textureformat + "_NormalMap";
        private const string c_Prop_Textureformat_Shadowmask = c_Prop_Textureformat + "_Shadowmask";
        private const string c_Prop_Textureformat_Lightmap = c_Prop_Textureformat + "_Lightmap";
        private const string c_Prop_Textureformat_DirectionalLightmap = c_Prop_Textureformat + "_DirectionalLightmap";

        private const string c_Prop_TextureCompression = "TextureCompression";
        private const string c_Prop_TextureCompression_NormalMap = c_Prop_TextureCompression + "_NormalMap";
        private const string c_Prop_TextureCompression_Shadowmask = c_Prop_TextureCompression + "_Shadowmask";
        private const string c_Prop_TextureCompression_Lightmap = c_Prop_TextureCompression + "_Lightmap";
        private const string c_Prop_TextureCompression_DirectionalLightmap = c_Prop_TextureCompression + "_DirectionalLightmap";

        private const string c_Prop_overridden = "overridden";
        private const string c_Prop_overridden_NormalMap = c_Prop_overridden + "_NormalMap";
        private const string c_Prop_overridden_Shadowmask = c_Prop_overridden + "_Shadowmask";
        private const string c_Prop_overridden_Lightmap = c_Prop_overridden + "_Lightmap";
        private const string c_Prop_overridden_DirectionalLightmap = c_Prop_overridden + "_DirectionalLightmap";

        private const string c_Prop_mipmapEnabled = "mipmapEnabled";
        private const string c_Prop_mipMapBias = "mipMapBias";
        private const string c_Prop_alphaSource = "alphaSource";
        private const string c_Prop_isReadable = "isReadable";
        private const string c_Prop_textureType = "textureType";
        private const string c_Prop_anisoLevel = "anisoLevel";
        private const string c_Prop_npotScale = "npotScale";
        private const string c_Prop_alphaIsTransparency = "alphaIsTransparency";
        private const string c_Prop_wrapMode = "wrapMode";
        private const string c_Prop_wrapModeU = "wrapModeU";
        private const string c_Prop_wrapModeV = "wrapModeV";
        private const string c_Prop_wrapModeW = "wrapModeW";
        private const string c_Prop_mipStreaming = "mipStreaming";
        private const string c_Prop_mipStreamingPriority = "mipStreamingPriority";
        
        private static bool IsDirectionalLightmap(TextureImporter texImp)
        {
            return AssetImportModel.IsDirectionalLightmap(texImp.textureType);
        }
        
        // 设置纹理的单个属性
        public static string SetTextureSingleProp(TextureImporter texImp, TextureImporterPlatformSettings texPlatformImp, string name, string value, bool isApplyChange)
        {
            string dirtyValue = "";

            switch (name)
            {
                #region maxTextureSize
                case c_Prop_maxTextureSize:
                {
                    SetMaxTextureSize(ref dirtyValue, texImp, texPlatformImp, value, isApplyChange);
                }
                    break;
                case c_Prop_maxTextureSize_NormalMap:
                {
                    if (texImp.textureType == TextureImporterType.NormalMap)
                    {
                        SetMaxTextureSize(ref dirtyValue, texImp, texPlatformImp, value, isApplyChange);
                    }
                }
                    break;
                case c_Prop_maxTextureSize_Shadowmask:
                {
                    if (texImp.textureType == TextureImporterType.Shadowmask)
                    {
                        SetMaxTextureSize(ref dirtyValue, texImp, texPlatformImp, value, isApplyChange);
                    }
                }
                    break;
                case c_Prop_maxTextureSize_Lightmap:
                {
                    if (texImp.textureType == TextureImporterType.Lightmap)
                    {
                        SetMaxTextureSize(ref dirtyValue, texImp, texPlatformImp, value, isApplyChange);
                    }
                }
                    break;
                case c_Prop_maxTextureSize_DirectionalLightmap:
                {
                    if (IsDirectionalLightmap(texImp))
                    {
                        SetMaxTextureSize(ref dirtyValue, texImp, texPlatformImp, value, isApplyChange);
                    }
                }
                    break;
                #endregion maxTextureSize
                
                #region Textureformat
                case c_Prop_Textureformat:
                {
                    SetTextureFormat(ref dirtyValue, texImp, texPlatformImp, value, isApplyChange);
                }
                    break;
                case c_Prop_Textureformat_NormalMap:
                {
                    if (texImp.textureType == TextureImporterType.NormalMap)
                    {
                        SetTextureFormat(ref dirtyValue, texImp, texPlatformImp, value, isApplyChange);
                    }
                }
                    break;
                case c_Prop_Textureformat_Shadowmask:
                {
                    if (texImp.textureType == TextureImporterType.Shadowmask)
                    {
                        SetTextureFormat(ref dirtyValue, texImp, texPlatformImp, value, isApplyChange);
                    }
                }
                    break;
                case c_Prop_Textureformat_Lightmap:
                {
                    if (texImp.textureType == TextureImporterType.Lightmap)
                    {
                        SetTextureFormat(ref dirtyValue, texImp, texPlatformImp, value, isApplyChange);
                    }
                }
                    break;
                case c_Prop_Textureformat_DirectionalLightmap:
                {
                    if (IsDirectionalLightmap(texImp))
                    {
                        SetTextureFormat(ref dirtyValue, texImp, texPlatformImp, value, isApplyChange);
                    }
                }
                    break;
                #endregion Textureformat
                
                #region TextureCompression
                case c_Prop_TextureCompression:
                {
                    SetTextureCompression(ref dirtyValue, texImp, texPlatformImp, value, isApplyChange);
                }
                    break;
                case c_Prop_TextureCompression_NormalMap:
                {
                    if (texImp.textureType == TextureImporterType.NormalMap)
                    {
                        SetTextureCompression(ref dirtyValue, texImp, texPlatformImp, value, isApplyChange);
                    }
                }
                    break;
                case c_Prop_TextureCompression_Shadowmask:
                {
                    if (texImp.textureType == TextureImporterType.Shadowmask)
                    {
                        SetTextureCompression(ref dirtyValue, texImp, texPlatformImp, value, isApplyChange);
                    }
                }
                    break;
                case c_Prop_TextureCompression_Lightmap:
                {
                    if (texImp.textureType == TextureImporterType.Lightmap)
                    {
                        SetTextureCompression(ref dirtyValue, texImp, texPlatformImp, value, isApplyChange);
                    }
                }
                    break;
                case c_Prop_TextureCompression_DirectionalLightmap:
                {
                    if (IsDirectionalLightmap(texImp))
                    {
                        SetTextureCompression(ref dirtyValue, texImp, texPlatformImp, value, isApplyChange);
                    }
                }
                    break;
                #endregion TextureCompression
                
                #region overridden
                case c_Prop_overridden:
                {
                    SetTextureOverridden(ref dirtyValue, texImp, texPlatformImp, value, isApplyChange);
                }
                    break;
                case c_Prop_overridden_NormalMap:
                {
                    if (texImp.textureType == TextureImporterType.NormalMap)
                    {
                        SetTextureOverridden(ref dirtyValue, texImp, texPlatformImp, value, isApplyChange);
                    }
                }
                    break;
                case c_Prop_overridden_Shadowmask:
                {
                    if (texImp.textureType == TextureImporterType.Shadowmask)
                    {
                        SetTextureOverridden(ref dirtyValue, texImp, texPlatformImp, value, isApplyChange);
                    }
                }
                    break;
                case c_Prop_overridden_Lightmap:
                {
                    if (texImp.textureType == TextureImporterType.Lightmap)
                    {
                        SetTextureOverridden(ref dirtyValue, texImp, texPlatformImp, value, isApplyChange);
                    }
                }
                    break;
                case c_Prop_overridden_DirectionalLightmap:
                {
                    if (IsDirectionalLightmap(texImp))
                    {
                        SetTextureOverridden(ref dirtyValue, texImp, texPlatformImp, value, isApplyChange);
                    }
                }
                    break;
                #endregion overridden
                
                case c_Prop_mipmapEnabled:
                    SetMipmapEnabled(ref dirtyValue, texImp, value, isApplyChange);
                    break;
                case c_Prop_mipMapBias:
                    SetMipMapBias(ref dirtyValue, texImp, value, isApplyChange);
                    break;
                case c_Prop_alphaSource:
                    SetAlphaSource(ref dirtyValue, texImp, value, isApplyChange);
                    break;
                case c_Prop_isReadable:
                    SetIsReadable(ref dirtyValue, texImp, value, isApplyChange);
                    break;
                case c_Prop_textureType:
                    SetTextureType(ref dirtyValue, texImp, value, isApplyChange);
                    break;
                case c_Prop_anisoLevel:
                    SetAnisoLevel(ref dirtyValue, texImp, value, isApplyChange);
                    break;
                case c_Prop_npotScale:
                    SetNpotScale(ref dirtyValue, texImp, value, isApplyChange);
                    break;
                case c_Prop_alphaIsTransparency:
                    SetAlphaIsTransparency(ref dirtyValue, texImp, value, isApplyChange);
                    break;
                case c_Prop_wrapMode:
                    SetWrapMode(ref dirtyValue, texImp, value, isApplyChange);
                    break;
                case c_Prop_wrapModeU:
                    SetWrapModeU(ref dirtyValue, texImp, value, isApplyChange);
                    break;
                case c_Prop_wrapModeV:
                    SetWrapModeV(ref dirtyValue, texImp, value, isApplyChange);
                    break;
                case c_Prop_wrapModeW:
                    SetWrapModeW(ref dirtyValue, texImp, value, isApplyChange);
                    break;
                case c_Prop_mipStreaming:
                    SetMipStreaming(ref dirtyValue, texImp, value, isApplyChange);
                    break;
                case c_Prop_mipStreamingPriority:
                    SetMipStreamingPriority(ref dirtyValue, texImp, value, isApplyChange);
                    break;
            }

            return dirtyValue;
        }
        
        private static void SetMaxTextureSize(ref string dirtyValue, TextureImporter texImp, TextureImporterPlatformSettings texPlatformImp, 
            string value, bool isApplyChange)
        {
            int size = 0;
            if (!int.TryParse(value, out size))
            {
                return;
            }

            if (texPlatformImp.maxTextureSize != size)
            {
                dirtyValue = texPlatformImp.maxTextureSize.ToString();
                if (isApplyChange)
                {
                    texPlatformImp.maxTextureSize = size;
                    texImp.SetPlatformTextureSettings(texPlatformImp);
                }
            }
        }

        /*
        public static void GetTextureRealWidthAndHeight(TextureImporter texImpoter, ref int width, ref int height)
        {
            System.Type type = typeof(TextureImporter);
            System.Reflection.MethodInfo method = type.GetMethod("GetWidthAndHeight",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var args = new object[] { width, height };
            method.Invoke(texImpoter, args);
            width = (int)args[0];
            height = (int)args[1];
        }
        */

        private static void SetTextureFormat(ref string dirtyValue, TextureImporter texImp, TextureImporterPlatformSettings texPlatformImp,
            string value, bool isApplyChange)
        {
            if (Enum.TryParse(value, out TextureImporterFormat format))
            {
                if (texPlatformImp.format != format)
                {
                    dirtyValue = texPlatformImp.format.ToString();
                    if (isApplyChange)
                    {
                        texPlatformImp.format = format;
                        texImp.SetPlatformTextureSettings(texPlatformImp);
                    }
                }
            }
        }

        private static void SetTextureOverridden(ref string dirtyValue, TextureImporter texImp, TextureImporterPlatformSettings texPlatformImp, 
            string value, bool isApplyChange)
        {
            if (bool.TryParse(value, out bool overridden))
            {
                if (texPlatformImp.overridden != overridden)
                {
                    dirtyValue = texPlatformImp.overridden.ToString();
                    if (isApplyChange)
                    {
                        texPlatformImp.overridden = overridden;

                        if (texPlatformImp.compressionQuality != 100)
                        {
                            texPlatformImp.compressionQuality = 100;
                        }
                        
                        texImp.SetPlatformTextureSettings(texPlatformImp);
                    }
                }
            }
        }

        private static void SetTextureCompression(ref string dirtyValue, TextureImporter texImp, TextureImporterPlatformSettings textureImporterPlatformSettings,
            string value, bool isApplyChange)
        {
            if (textureImporterPlatformSettings.name != AssetImportModel.PLATFORM_DEFAULT)
            {
                return;
            }
            
            if (Enum.TryParse(value, out TextureImporterCompression compression))
            {
                if (textureImporterPlatformSettings.textureCompression != compression)
                {
                    dirtyValue = textureImporterPlatformSettings.textureCompression.ToString();
                    if (isApplyChange)
                    {
                        textureImporterPlatformSettings.textureCompression = compression;
                        texImp.SetPlatformTextureSettings(textureImporterPlatformSettings);
                    }
                }
            }
        }


        private static void SetMipmapEnabled(ref string dirtyValue, TextureImporter texImp, string value, bool isApplyChange)
        {
            if (bool.TryParse(value, out bool flag))
            {
                if (texImp.mipmapEnabled != flag)
                {
                    dirtyValue = texImp.mipmapEnabled.ToString();
                    if (isApplyChange)
                    {
                        texImp.mipmapEnabled = flag;
                    }
                }
            }
        }

        private static void SetMipMapBias(ref string dirtyValue, TextureImporter texImp, string value, bool isApplyChange)
        {
            if (float.TryParse(value, out float bias))
            {
                if (!Mathf.Approximately(texImp.mipMapBias, bias))
                {
                    dirtyValue = texImp.mipMapBias.ToString();
                    if (isApplyChange)
                    {
                        texImp.mipMapBias = bias;
                    }
                }
            }
        }

        private static void SetAlphaSource(ref string dirtyValue, TextureImporter texImp, string value, bool isApplyChange)
        {
            if (Enum.TryParse(value, out TextureImporterAlphaSource target))
            {
                if (texImp.alphaSource != target)
                {
                    dirtyValue = texImp.alphaSource.ToString();
                    if (isApplyChange)
                    {
                        texImp.alphaSource = target;
                    }
                }
            }
        }

        private static void SetIsReadable(ref string dirtyValue, TextureImporter texImp, string value, bool isApplyChange)
        {
            if (bool.TryParse(value, out bool flag))
            {
                if (texImp.isReadable != flag)
                {
                    dirtyValue = texImp.isReadable.ToString();
                    if (isApplyChange)
                    {
                        texImp.isReadable = flag;
                    }
                }
            }
        }

        private static void SetTextureType(ref string dirtyValue, TextureImporter texImp, string value, bool isApplyChange)
        {
            if (!AssetImportModel.TryParseTextureImporterType(value, out TextureImporterType target))
            {
                target = TextureImporterType.SingleChannel;
            }

            if (texImp.textureType != target)
            {
                dirtyValue = texImp.textureType.ToString();
                if (isApplyChange)
                {
                    texImp.textureType = target;
                }
            }
        }

        private static void SetAnisoLevel(ref string dirtyValue, TextureImporter texImp, string value, bool isApplyChange)
        {
            if (int.TryParse(value, out int level))
            {
                if (texImp.anisoLevel != level)
                {
                    dirtyValue = texImp.anisoLevel.ToString();
                    if (isApplyChange)
                    {
                        texImp.anisoLevel = level;
                    }
                }
            }
        }

        private static void SetNpotScale(ref string dirtyValue, TextureImporter texImp, string value, bool isApplyChange)
        {
            if (Enum.TryParse(value, out TextureImporterNPOTScale npotScale))
            {
                if (texImp.npotScale != npotScale)
                {
                    dirtyValue = texImp.npotScale.ToString();
                    if (isApplyChange)
                    {
                        texImp.npotScale = npotScale;
                    }
                }
            }
        }

        private static void SetAlphaIsTransparency(ref string dirtyValue, TextureImporter texImp, string value, bool isApplyChange)
        {
            if (bool.TryParse(value, out bool alphaIsTransparency))
            {
                if (texImp.alphaIsTransparency != alphaIsTransparency)
                {
                    dirtyValue = texImp.alphaIsTransparency.ToString();
                    if (isApplyChange)
                    {
                        texImp.alphaIsTransparency = alphaIsTransparency;
                    }
                }
            }
        }

        private static void SetWrapMode(ref string dirtyValue, TextureImporter texImp, string value, bool isApplyChange)
        {
            if (Enum.TryParse(value, out TextureWrapMode wrapMode))
            {
                if (texImp.wrapMode != wrapMode)
                {
                    dirtyValue = texImp.wrapMode.ToString();
                    if (isApplyChange)
                    {
                        texImp.wrapMode = wrapMode;
                    }
                }
            }
        }

        private static void SetWrapModeU(ref string dirtyValue, TextureImporter texImp, string value, bool isApplyChange)
        {
            if (Enum.TryParse(value, out TextureWrapMode wrapMode))
            {
                if (texImp.wrapMode != wrapMode)
                {
                    dirtyValue = texImp.wrapMode.ToString();
                    if (isApplyChange)
                    {
                        texImp.wrapModeU = wrapMode;
                    }
                }
            }
        }

        private static void SetWrapModeV(ref string dirtyValue, TextureImporter texImp, string value, bool isApplyChange)
        {
            if (Enum.TryParse(value, out TextureWrapMode wrapMode))
            {
                if (texImp.wrapMode != wrapMode)
                {
                    dirtyValue = texImp.wrapMode.ToString();
                    if (isApplyChange)
                    {
                        texImp.wrapModeV = wrapMode;
                    }
                }
            }
        }

        private static void SetWrapModeW(ref string dirtyValue, TextureImporter texImp, string value, bool isApplyChange)
        {
            if (Enum.TryParse(value, out TextureWrapMode wrapMode))
            {
                if (texImp.wrapMode != wrapMode)
                {
                    dirtyValue = texImp.wrapMode.ToString();
                    if (isApplyChange)
                    {
                        texImp.wrapModeW = wrapMode;
                    }
                }
            }
        }

        private static void SetMipStreaming(ref string dirtyValue, TextureImporter texImp, string value, bool isApplyChange)
        {
            if (bool.TryParse(value, out bool mipStreaming))
            {
                if (texImp.streamingMipmaps != mipStreaming)
                {
                    dirtyValue = texImp.streamingMipmaps.ToString();
                    if (isApplyChange)
                    {
                        texImp.streamingMipmaps = mipStreaming;
                    }
                }
            }
        }

        private static void SetMipStreamingPriority(ref string dirtyValue, TextureImporter texImp, string value, bool isApplyChange)
        {
            if (int.TryParse(value, out int mipStreamingPriority))
            {
                if (texImp.streamingMipmapsPriority != mipStreamingPriority)
                {
                    dirtyValue = texImp.streamingMipmapsPriority.ToString();
                    if (isApplyChange)
                    {
                        texImp.streamingMipmapsPriority = mipStreamingPriority;
                    }
                }
            }
        }
        
    }
}
