using System.Collections.Generic;
using com.xknight.mt.Lib.Runtime.MT.Log;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.DataContainer
{
    /// <summary>
    /// 高度图
    /// </summary>
    public class MTHeightMap
    {
        /// <summary>
        /// 高度图的坐标检查范围
        /// </summary>
        public static float HeightmapCheckRange = 1f;
        
        /// <summary>
        /// 1组十字检查点
        /// </summary>
        public static Vector3[] HeightmapCheckPoints =
        {
            new Vector3(0, 0, HeightmapCheckRange),
            new Vector3(0, 0, -HeightmapCheckRange),
            new Vector3(-HeightmapCheckRange, 0, 0),
            new Vector3(HeightmapCheckRange, 0, 0),
        };
        
        //静态数据 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        private static int s_mapWidth = 1, s_mapHeight = 1;
        private static float s_range;
        private static Dictionary<uint, MTHeightMap> s_dict = new ();

        public static uint FormatId(Vector3 pos)
        {
            //transform to (0 ~ short.MaxValue * _mapWidth)
            int x = Mathf.CeilToInt(pos.x + s_range) / s_mapWidth;
            int y = Mathf.CeilToInt(pos.z + s_range) / s_mapHeight;
            
            uint id = (uint)x;
            id = (id << 16) | (uint)y;
            
            return id;
        }

        public static void RegisterMap(MTHeightMap map)
        {
            int mapWidth = Mathf.FloorToInt(map.m_worldBounds.size.x);
            int mapHeight = Mathf.FloorToInt(map.m_worldBounds.size.z);
            
            //因为地块都是等大的，所以都以第1个为准就行了
            if (s_dict.Count == 0)
            {
                s_mapWidth = mapWidth;
                s_mapHeight = mapHeight;
                s_range = Mathf.Max(s_mapWidth, s_mapHeight) * short.MaxValue;
            }

            if (s_mapWidth != mapWidth || s_mapHeight != mapHeight)
            {
                MTLogger.LogError($"高度的尺寸不一致 : {s_mapWidth} - {mapWidth}, {s_mapHeight} - {mapHeight}");
                return;
            }

            uint id = FormatId(map.m_worldBounds.center);
            if (s_dict.ContainsKey(id))
            {
                MTLogger.LogError($"高度图id重叠 : {map.m_worldBounds.center.x}, {map.m_worldBounds.center.z}");
                return;
            }

            s_dict.Add(id, map);
        }

        public static void UnregisterMap(MTHeightMap map)
        {
            uint id = FormatId(map.m_worldBounds.center);
            if (!s_dict.ContainsKey(id))
            {
                MTLogger.LogError($"高度图不存在 : {map.m_worldBounds.center.x}, {map.m_worldBounds.center.z}");
                return;
            }

            s_dict.Remove(id);
        }

        /// <summary>
        /// 获取目标位置的高度插值
        /// </summary>
        public static bool GetHeightInterpolated(Vector3 pos, ref float height)
        {
            float tootalHeight = 0;
            float outHeight = 0;
            int validCounter = 0;
            
            uint centerId = FormatId(pos);
            if (s_dict.ContainsKey(centerId) && s_dict[centerId].GetInterpolatedHeight(pos, out outHeight))
            {
                tootalHeight += outHeight;
                validCounter++;
            }
            
            // 当快速切换所属地形时，会匹配不上想检查的地形，所以这里要多检查一下周围的点
            for (int i = 0; i < HeightmapCheckPoints.Length; i++)
            {
                Vector3 otherPoint = pos + HeightmapCheckPoints[i];
                
                uint otherId = FormatId(otherPoint);
                if (s_dict.ContainsKey(otherId) && s_dict[otherId].GetInterpolatedHeight(pos, out outHeight))
                {
                    tootalHeight += outHeight;
                    validCounter++;
                }
            }

            if (validCounter > 0)
            {
                height = tootalHeight / validCounter;
                return true;
            }
            return false;
        }

        
        //实例 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        //世界空间包围盒
        private Bounds m_worldBounds;
        //高度图的世界空间Y值
        private float m_heightmapWorldY;
        //高度图的分辨率
        private int m_heightmapResolusion = 513;
        //高度图的缩放比
        private Vector3 m_heightScale;
        //高度图的2进制数据
        private byte[] m_heightData;

        public MTHeightMap(
            Bounds worldBounds, float heightmapWorldY, 
            int heightmapResolusion, Vector3 heightScale, 
            byte[] heightDataData)
        {
            m_worldBounds = worldBounds;
            m_heightmapWorldY = heightmapWorldY;
            m_heightmapResolusion = heightmapResolusion;
            m_heightScale = heightScale;
            m_heightData = heightDataData;
            
            RegisterMap(this);
        }
        
        /// <summary>
        /// 获取差值后的高度
        /// </summary>
        public bool GetInterpolatedHeight(Vector3 pos, out float height)
        {
            var checkPos = pos;
            checkPos.y = m_worldBounds.center.y; //用中心点的高度，可以避免误差的问题
            
            if (!m_worldBounds.Contains(checkPos))
            {
                height = 0;
                return false;
            }
            
            float val = GetInterpolatedHeight(pos);
            height = val / 255f * m_heightScale.y + m_heightmapWorldY;
            return true;
        }
        
        private float GetInterpolatedHeight(Vector3 pos)
        {
            int hr = m_heightmapResolusion - 1;
            float hrX = Mathf.Clamp01((pos.x - m_worldBounds.min.x) / m_worldBounds.size.x) * hr;
            float hrZ = Mathf.Clamp01((pos.z - m_worldBounds.min.z) / m_worldBounds.size.z) * hr;
            int x = Mathf.FloorToInt(hrX);
            int z = Mathf.FloorToInt(hrZ);
            
            float tx = hrX - x;
            float tz = hrZ - z;
            
            float y00 = SampleHeightMapData(x, z);
            float y10 = SampleHeightMapData(x + 1, z);
            float y01 = SampleHeightMapData(x, z + 1);
            float y11 = SampleHeightMapData(x + 1, z + 1);

            float y0010 = Mathf.Lerp(y00, y10, tx);
            float y0111 = Mathf.Lerp(y01, y11, tx);
            float val = Mathf.Lerp(y0010, y0111, tz);

            return val;
        }
        
        private float SampleHeightMapData(int hrX, int hrZ)
        {
            int idx = hrZ * 2 * m_heightmapResolusion + hrX * 2;
            
            byte h = m_heightData[idx];
            byte l = m_heightData[idx + 1];
            
            float heightVal = h + (l / 255f);
            return heightVal;
        }
        
    }
}