// Created By: WangYu  Date: 2025-04-29

using AirSticker.Runtime.Logic;
using AirSticker.Runtime.Render;

namespace AirSticker
{
    public class DecalConfigGUIFactory
    {
        public static AbsDecalConfigGUI Create(AbsDecalConfig config)
        {
            AbsDecalConfigGUI configGui = null;
            
            if (config is BaseDecalConfig bdConfig)
            {
                configGui = new BaseDecalConfigGUI(bdConfig);
            }
            else if(config is KnifeMarkDecalConfig kmdConfig)
            {
                configGui = new KnifeMarkDecalConfigGUI(kmdConfig);
            }

            return configGui;
        }
        
    }
}