using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Garena.TA
{
    /// <summary>
    /// 逐物体阴影Pass数据
    /// </summary>
    public class GPerObjectShadowData
    {

        /// <summary>
        /// 阴影纹理
        /// </summary>
        public RTHandle ShadowmapTexture;

        /// <summary>
        /// 阴影纹理总宽度
        /// </summary>
        public int ShadowmapTextureWidth = 128;

        /// <summary>
        /// 阴影纹理总高度
        /// </summary>
        public int ShadowmapTextureHeight = 128;

        public const int ShadowmapBufferBits = 16;

        /// <summary>
        /// 有效逐物体阴影数量
        /// </summary>
        public int ValidSliceCount;

        public List<GPerObjectShadowSliceData> CulledSliceData { get; set; } = new List<GPerObjectShadowSliceData>();

        List<GPerObjectShadowSliceData> SliceData { get; set; } = new List<GPerObjectShadowSliceData>();

        public const int MaxPerObjectShadowCount = 16;

        public Matrix4x4[] shadowMatris = new Matrix4x4[MaxPerObjectShadowCount];
        public Vector4[] shadowUVRect = new Vector4[MaxPerObjectShadowCount];
        public float[] shadowIntensity = new float[MaxPerObjectShadowCount];

        public Matrix4x4[] localToWorld = new Matrix4x4[MaxPerObjectShadowCount];

        /// <summary>
        /// 阴影分区偏移值
        /// </summary>
        Vector2[] sliceViewPortOffset = new Vector2[] {
            new Vector2(0,0),
            new Vector2(1,0),
            new Vector2(0,1),
            new Vector2(1,1),

            new Vector2(2,0),
            new Vector2(3,0),
            new Vector2(2,1),
            new Vector2(3,1),

            new Vector2(0,2),
            new Vector2(1,2),
            new Vector2(0,3),
            new Vector2(1,3),

            new Vector2(2,2),
            new Vector2(3,2),
            new Vector2(2,3),
            new Vector2(3,3),
        };

        /// <summary>
        /// 逐物体目标数据
        /// </summary>
        List<GPerObjectShadowTargetData> dataList = new List<GPerObjectShadowTargetData>();

        public GPerObjectShadowData()
        {

        }

        public void Release()
        {
            ShadowmapTexture?.Release();
        }

        public void UpdateSliceData(GPerObjectShadowPassSettings Settings, Quaternion shadowLightRotation, float halfAngleLerp = 0)
        {
            dataList = GPerObjectShadowManager.Instance.FinalTargetList;

            // 这一部分是用于更新 SliceData 列表的，SliceData列表是一个复用的列表
            // 以下部分检查过了，更新后不会出现上一次计算的残留
            if (Settings.CombineBounds)
            {
                ValidSliceCount = 1;

                GPerObjectShadowSliceData slice = null;
                if (SliceData.Count == 0)
                {
                    slice = new GPerObjectShadowSliceData(Settings);
                    SliceData.Add(slice);
                }
                else
                {
                    if (SliceData[0] == null)
                        SliceData[0] = new GPerObjectShadowSliceData(Settings);

                    slice = SliceData[0];
                }

                int padx = 0;// (int)Mathf.Max(Settings.MinPadding, ShadowmapTextureWidth * Settings.PaddingPercent);
                int pady = 0;// (int)Mathf.Max(Settings.MinPadding, ShadowmapTextureHeight * Settings.PaddingPercent);
                slice.ViewPort = new Rect(padx, pady, ShadowmapTextureWidth - 2 * padx, ShadowmapTextureHeight - 2 * pady);

                //slice.ViewPort = new Rect(1, 1, ShadowmapTextureWidth - 2, ShadowmapTextureHeight - 2);

                MergeRenderData(out slice.renderDatas, out slice.BoundsWS);
                bool hasSamplePosition = TryGetMergedTargetPosition(out Vector3 samplePosition);
                Quaternion lightRotation = GetLightRotation(
                    Settings,
                    shadowLightRotation,
                    slice.BoundsWS,
                    samplePosition,
                    hasSamplePosition,
                    out float intensity);
                slice.UpdateMatrix(lightRotation, halfAngleLerp, false);

                shadowMatris[0] = slice.WorldToShadowMatrix;
                shadowUVRect[0] = new Vector4(0, 0, 1, 1);
                shadowIntensity[0] = intensity;
            }
            else
            {
                ValidSliceCount = Math.Min(Settings.MaxCount, dataList.Count);

                for (int i = 0; i < ValidSliceCount; i++)
                {
                    GPerObjectShadowTargetData target = dataList[i];

                    if (SliceData.Count <= i || SliceData[i] == null)
                        SliceData[i] = new GPerObjectShadowSliceData(Settings);
                    

                    var slice = SliceData[i];

                    slice.SliceOffset = sliceViewPortOffset[i];

                    int padx = 0;// (int)Mathf.Max(Settings.MinPadding, ShadowmapTextureWidth * Settings.PaddingPercent);
                    int pady = 0;// (int)Mathf.Max(Settings.MinPadding, ShadowmapTextureHeight * Settings.PaddingPercent);
                    
                    slice.ViewPort = new Rect(sliceViewPortOffset[i].x * Settings.SliceTextureSize + padx,
                        sliceViewPortOffset[i].y * Settings.SliceTextureSize + pady,
                        Settings.SliceTextureSize - 2 * padx,
                        Settings.SliceTextureSize - 2 * pady);


                    slice.renderDatas = target.GetRenderDatas();
                    slice.BoundsWS = target.bounds;
                    bool hasSamplePosition = target.go != null;
                    Vector3 samplePosition = hasSamplePosition ? target.go.transform.position : Vector3.zero;
                    Quaternion finalRotation = GetLightRotation(
                        Settings,
                        shadowLightRotation,
                        target.bounds,
                        samplePosition,
                        hasSamplePosition,
                        out float finalIntensity);
                    
                    slice.UpdateMatrix(finalRotation, halfAngleLerp, false);
                    shadowIntensity[i] = finalIntensity;

                    Vector3 scale = new Vector3((float)Settings.SliceTextureSize / ShadowmapTextureWidth, (float)Settings.SliceTextureSize / ShadowmapTextureHeight, 1);
                    Vector3 offset = slice.SliceOffset * scale;
                    var toSlice = Matrix4x4.TRS(offset, Quaternion.identity, scale);

                    slice.WorldToShadowMatrix = toSlice * slice.WorldToShadowMatrix;

                    shadowMatris[i] = slice.WorldToShadowMatrix;
                    shadowUVRect[i] = new Vector4(
                        slice.SliceOffset.x * (float)Settings.SliceTextureSize / ShadowmapTextureWidth,
                        slice.SliceOffset.y * (float)Settings.SliceTextureSize / ShadowmapTextureHeight,
                        slice.SliceOffset.x * (float)Settings.SliceTextureSize / ShadowmapTextureWidth + scale.x,
                        slice.SliceOffset.y * (float)Settings.SliceTextureSize / ShadowmapTextureHeight + scale.y
                        );
                }
            }

            CulledSliceData = SliceData;

            for (int i = 0; i < CulledSliceData.Count; ++i)
            {
                if (CulledSliceData[i] != null)
                    localToWorld[i] = CulledSliceData[i].ShadowMeshLocalToWorld;
            }
        }

        private Quaternion GetLightRotation(
            GPerObjectShadowPassSettings settings,
            Quaternion shadowLightRotation,
            Bounds bounds,
            Vector3 samplePosition,
            bool hasSamplePosition,
            out float intensity)
        {
            Quaternion lightRotation = shadowLightRotation;
            intensity = 1f;

            if (settings.OverrideLightRotation)
            {
                lightRotation = Quaternion.Euler(settings.LightRotation);
            }

            GPerObjectLightVolumeSample sample;
            if (hasSamplePosition
                && GPerObjectShadowManager.Instance.TryGetLightVolumeSample(
                    bounds,
                    samplePosition,
                    lightRotation,
                    intensity,
                    out sample))
            {
                lightRotation = sample.rotation;
                intensity = sample.intensity;
            }

            return lightRotation;
        }

        private bool TryGetMergedTargetPosition(out Vector3 position)
        {
            position = Vector3.zero;
            int count = 0;

            foreach (var targetData in dataList)
            {
                if (targetData == null || targetData.go == null)
                {
                    continue;
                }

                position += targetData.go.transform.position;
                count++;
            }

            if (count == 0)
            {
                return false;
            }

            position /= count;
            return true;
        }

        /// <summary>
        /// 计算阴影图大小
        /// </summary>
        public void UpdateShadowTextureSize(GPerObjectShadowPassSettings settings)
        {
            // 只在List小于最大范围时更新
            if(SliceData == null || SliceData.Count < settings.MaxCount) 
                SliceData = new List<GPerObjectShadowSliceData>(new GPerObjectShadowSliceData[settings.MaxCount]);

            if (settings.CombineBounds)
            {
                ShadowmapTextureWidth = settings.SliceTextureSize;
                ShadowmapTextureHeight = settings.SliceTextureSize;
            }
            else
            {
                Vector2Int count = Vector2Int.one;
                while (count.x * count.y < settings.MaxCount)
                {
                    if (count.x <= count.y)
                        count.x *= 2;
                    else
                        count.y *= 2;
                }

                ShadowmapTextureWidth = count.x * settings.SliceTextureSize;
                ShadowmapTextureHeight = count.y * settings.SliceTextureSize;
            }
        }

        void MergeRenderData(out List<GPerObjectShadowTargetRenderData> r, out Bounds b)
        {
            r = new List<GPerObjectShadowTargetRenderData>();
            b = new Bounds();

            bool isFirst = true;

            foreach (var targetData in dataList)
            {
                if (isFirst)
                {
                    isFirst = false;
                    b = targetData.bounds;
                }
                else
                {
                    b.Encapsulate(targetData.bounds);
                }

                r.AddRange(targetData.GetRenderDatas());
            }
        }
    }
}
