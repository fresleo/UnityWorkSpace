using System.Collections.Generic;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.DataContainer
{
    /// <summary>
    /// 水高度的供应商
    /// </summary>
    public interface IMTWaterHeightProvider
    {
        /// <summary>
        /// 在的范围内
        /// </summary>
        bool Contains(Vector3 groundWorldPos);
        
        /// <summary>
        /// 获取水的高度
        /// </summary>
        float GetHeight(Vector3 groundWorldPos);
    }

    /// <summary>
    /// 水的高度
    /// </summary>
    public class MTWaterHeight
    {
        static readonly List<IMTWaterHeightProvider> s_providers = new ();

        public static void RegProvider(IMTWaterHeightProvider provider)
        {
            s_providers.Add(provider);
        }

        public static void UnRegProvider(IMTWaterHeightProvider provider)
        {
            s_providers.Remove(provider);
        }

        public static float GetWaterHeight(Vector3 groundWorldPos)
        {
            float groundHeight = groundWorldPos.y;
            
            for (int i = 0; i < s_providers.Count; i++)
            {
                var water = s_providers[i];
                if (water.Contains(groundWorldPos))
                {
                    float waterHeight = water.GetHeight(groundWorldPos);
                    if (waterHeight > groundHeight)
                    {
                        return waterHeight;
                    }
                }
            }

            return groundHeight;
        }
    }
}