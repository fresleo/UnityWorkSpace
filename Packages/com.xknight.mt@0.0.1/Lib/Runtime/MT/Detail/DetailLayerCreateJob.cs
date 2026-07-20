// Created By: WangYu  Date: 2022-10-10

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace com.xknight.mt.Lib.Runtime.MT.Detail
{
    /// <summary>
    /// 细节层的创建工作
    /// </summary>
    [BurstCompile]
    internal struct DetailLayerCreateJob : IJob
    {
        [ReadOnly] public NativeArray<byte> densityData;
        public int densityX;
        public int densityZ;
        public float3 posParam; //x = offset x, y = offset z, z = patch size
        public float3 localScale;
        public int detailResolutionPerPatch;
        public int detailMaxDensity;
        
        //输入
        public NativeArray<float> noiseSeed;
        public NativeArray<int> dataOffset;
        //属性定义
        public NativeArray<float> minWidth;
        public NativeArray<float> maxWidth;
        public NativeArray<float> minHeight;
        public NativeArray<float> maxHeight;
        public NativeArray<float> noiseSpread;
        public NativeArray<float4> healthyColor;
        public NativeArray<float4> dryColor;

        //输出
        public NativeArray<float3> positions;
        public NativeArray<float3> scales;
        public NativeArray<float4> colors;
        public NativeArray<float> orientations;
        public NativeArray<int> spawnedCount;
        
        
        public void Execute()
        {
            spawnedCount[0] = 0;
            float stride = posParam.z / detailResolutionPerPatch;
            for (int i = 0; i < dataOffset.Length; i++)
            {
                GeneratePatch(i, stride);
            }
        }

        
        //生成补丁
        private void GeneratePatch(int i, float stride)
        {
            for (int z = 0; z < detailResolutionPerPatch; z++)
            {
                for (int x = 0; x < detailResolutionPerPatch; x++)
                {
                    var index = dataOffset[i] + z * detailResolutionPerPatch + x;
                    int density = densityData[index];
                    if (density <= 0)
                    {
                        continue;
                    }
                    
                    density = math.min(16, density);
                    float sx = posParam.x + densityX * posParam.z + x * stride;
                    float sz = posParam.y + densityZ * posParam.z + z * stride;
                    GenerateOnePixel(i, density, sx, sz, stride);
                }
            }
        }

        //生成1像素
        private void GenerateOnePixel(int i, int density, float sx, float sz, float stride)
        {
            int spread = (int)math.floor(math.sqrt(density) + 0.5f);
            float strideX = 1f / spread;
            float strideZ = 1f / spread;
            for (int z = 0; z < spread; z++)
            {
                for (int x = 0; x < spread; x++)
                {
                    int idx = spawnedCount[0];
                    float fx = sx + x * strideX * stride;
                    float fz = sz + z * strideZ * stride;

                    float nx = fx * noiseSpread[i] + noiseSeed[i];
                    float ny = fz * noiseSpread[i] + noiseSeed[i];
                    float globalNoise = CNoise(nx, ny);

                    float snx = (fx + noiseSeed[i]) * detailResolutionPerPatch * noiseSpread[i];
                    float sny = (fz + noiseSeed[i]) * detailResolutionPerPatch * noiseSpread[i];
                    float localNoise = SNoise(snx, sny);
                    
                    float minW = math.min(minWidth[i], maxWidth[i]);
                    float maxW = math.max(minWidth[i], maxWidth[i]);
                    float width = math.lerp(minW, maxW, localNoise);
                    float minH = math.min(minHeight[i], maxHeight[i]);
                    float maxH = math.max(minHeight[i], maxHeight[i]);
                    float height = math.lerp(minH, maxH, localNoise);
                    
                    colors[idx] = math.lerp(healthyColor[i], dryColor[i], globalNoise);
                    positions[idx] = new float3(fx + localNoise, 0, fz + localNoise);
                    scales[idx] = new float3(width * localScale.x, height * localScale.y, height * localScale.z);
                    orientations[idx] = math.lerp(0, 360, localNoise);

                    spawnedCount[0]++;
                }
            }
        }
        
        
        private static float CNoise(float x, float y)
        {
            float2 pos = math.float2(x, y);
            return noise.cnoise(pos);
        }

        private static float SNoise(float x, float y)
        {
            float2 pos = math.float2(x, y);
            return noise.snoise(pos);
        }
        
    }
}