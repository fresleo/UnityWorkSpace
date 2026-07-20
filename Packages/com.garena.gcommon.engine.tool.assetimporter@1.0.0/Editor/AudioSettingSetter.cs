// Created by: WangYu   Date: 2025-09-26

using UnityEditor;
using UnityEngine;

namespace com.garena.gcommon.engine.tool.assetimporter
{
    internal static class AudioSettingSetter
    {
        private const string c_Prop_loadType = "loadType";
        
        public static string SetAudioSingleProp(AudioImporter ai, string name, string value, bool isApplyChange)
        {
            string dirtyValue = "";
            
            switch (name)
            {
                case c_Prop_loadType:
                {
                    AudioImporterSampleSettings sampleSetting = ai.defaultSampleSettings;
                    
                    if ("CompressedInMemory" == value)
                    {
                        if (sampleSetting.loadType != AudioClipLoadType.CompressedInMemory)
                        {
                            dirtyValue = sampleSetting.loadType.ToString();
                            if (isApplyChange)
                            {
                                sampleSetting.loadType = AudioClipLoadType.CompressedInMemory;
                            }
                        }
                    }

                    if ("DecompressOnLoad" == value)
                    {
                        if (sampleSetting.loadType != AudioClipLoadType.DecompressOnLoad)
                        {
                            dirtyValue = sampleSetting.loadType.ToString();
                            if (isApplyChange)
                            {
                                sampleSetting.loadType = AudioClipLoadType.DecompressOnLoad;
                            }
                        }
                    }

                    if ("Streaming" == value)
                    {
                        if (sampleSetting.loadType != AudioClipLoadType.Streaming)
                        {
                            dirtyValue = sampleSetting.loadType.ToString();
                            if (isApplyChange)
                            {
                                sampleSetting.loadType = AudioClipLoadType.Streaming;
                            }
                        }
                    }

                    if (isApplyChange)
                    {
                        ai.defaultSampleSettings = sampleSetting;
                    }
                }
                    break;
                
                default:
                    break;
            }

            return dirtyValue;
        }
        
    }
}