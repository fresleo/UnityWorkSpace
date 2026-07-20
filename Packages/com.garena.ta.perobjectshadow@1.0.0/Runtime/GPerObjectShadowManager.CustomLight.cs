/*******************************************************************************
 * File: GPerObjectShadowManager.CustomLight.cs
 * Author: Codex
 * Date: 2026-05-08
 * Description: 逐物体阴影局部光照 Volume 的注册、采样和混合逻辑。
 *
 * Notice: Volume 采样结果只影响逐物体阴影 slice，不会修改场景 Light 组件。
 *******************************************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace Garena.TA
{
    /// <summary>
    /// 局部光照 Volume 的采样结果。
    /// </summary>
    public struct GPerObjectLightVolumeSample
    {
        /// <summary>
        /// 混合后的光照朝向。
        /// </summary>
        public Quaternion rotation;

        /// <summary>
        /// 混合后的阴影强度倍率。
        /// </summary>
        public float intensity;

        /// <summary>
        /// 局部 Volume 对默认配置的覆盖权重。
        /// </summary>
        public float weight;
    }

    public partial class GPerObjectShadowManager
    {
        private const float C_MIN_VOLUME_WEIGHT = 0.0001f;
        private const float C_MIN_VOLUME_VERTICAL = 0.0001f;

        /// <summary>
        /// 场景中启用的逐物体阴影局部光照 Volume。
        /// </summary>
        private List<GPerObjectLightVolume> _lightVolumes = new List<GPerObjectLightVolume>();

        /// <summary>
        /// 注册局部光照 Volume。
        /// </summary>
        /// <param name="volume">待注册的 Volume。</param>
        public void AddLightVolume(GPerObjectLightVolume volume)
        {
            if (volume == null)
            {
                return;
            }

            CleanupLightVolumes();
            if (_lightVolumes.Contains(volume))
            {
                return;
            }

            _lightVolumes.Add(volume);
        }

        /// <summary>
        /// 移除局部光照 Volume。
        /// </summary>
        /// <param name="volume">待移除的 Volume。</param>
        public void RemoveLightVolume(GPerObjectLightVolume volume)
        {
            if (volume == null)
            {
                return;
            }

            _lightVolumes.Remove(volume);
        }

        /// <summary>
        /// 按包围盒采样所有局部光照 Volume。
        /// </summary>
        /// <param name="bounds">逐物体阴影目标的世界空间包围盒。</param>
        /// <param name="samplePosition">用于计算距离衰减的目标世界坐标。</param>
        /// <param name="baseRotation">未命中 Volume 时的默认光照朝向。</param>
        /// <param name="baseIntensity">未命中 Volume 时的默认阴影强度。</param>
        /// <param name="sample">输出采样结果。</param>
        /// <returns>至少命中一个有效 Volume 时返回 true。</returns>
        public bool TryGetLightVolumeSample(
            Bounds bounds,
            Vector3 samplePosition,
            Quaternion baseRotation,
            float baseIntensity,
            out GPerObjectLightVolumeSample sample)
        {
            sample = new GPerObjectLightVolumeSample
            {
                rotation = baseRotation,
                intensity = Mathf.Clamp01(baseIntensity),
                weight = 0f
            };

            CleanupLightVolumes();

            float volumeWeightSum = 0f;
            for (int i = 0; i < _lightVolumes.Count; i++)
            {
                GPerObjectLightVolume volume = _lightVolumes[i];
                if (volume == null)
                {
                    continue;
                }

                volumeWeightSum += volume.GetInfluence(bounds, samplePosition);
            }

            if (volumeWeightSum <= C_MIN_VOLUME_WEIGHT)
            {
                return false;
            }

            float volumeWeightScale = 1f / volumeWeightSum;
            float resolvedVolumeWeight = 0f;
            float intensitySum = 0f;
            Vector3 shadowOffsetSum = Vector3.zero;
            float verticalSignSum = 0f;

            for (int i = 0; i < _lightVolumes.Count; i++)
            {
                GPerObjectLightVolume volume = _lightVolumes[i];
                if (volume == null)
                {
                    continue;
                }

                float influence = volume.GetInfluence(bounds, samplePosition) * volumeWeightScale;
                if (influence <= C_MIN_VOLUME_WEIGHT)
                {
                    continue;
                }

                resolvedVolumeWeight += influence;
                intensitySum += volume.GetIntensity() * influence;
                AddWeightedShadowOffset(ref shadowOffsetSum, ref verticalSignSum, volume.LightDirection, influence);
            }

            if (resolvedVolumeWeight <= C_MIN_VOLUME_WEIGHT)
            {
                return false;
            }

            sample.rotation = ResolveRotation(shadowOffsetSum, verticalSignSum, baseRotation);
            sample.intensity = Mathf.Clamp01(intensitySum / resolvedVolumeWeight);
            sample.weight = Mathf.Clamp01(resolvedVolumeWeight);

            return true;
        }

        private void CleanupLightVolumes()
        {
            for (int i = _lightVolumes.Count - 1; i >= 0; i--)
            {
                if (_lightVolumes[i] != null)
                {
                    continue;
                }

                _lightVolumes.RemoveAt(i);
            }
        }

        private static void AddWeightedShadowOffset(
            ref Vector3 shadowOffsetSum,
            ref float verticalSignSum,
            Vector3 direction,
            float weight)
        {
            if (weight <= C_MIN_VOLUME_WEIGHT || direction.sqrMagnitude <= C_MIN_VOLUME_WEIGHT)
            {
                return;
            }

            direction.Normalize();
            float vertical = direction.y;
            float verticalAbs = Mathf.Max(Mathf.Abs(vertical), C_MIN_VOLUME_VERTICAL);
            shadowOffsetSum += new Vector3(direction.x / verticalAbs, 0f, direction.z / verticalAbs) * weight;
            verticalSignSum += (vertical >= 0f ? 1f : -1f) * weight;
        }

        private static Quaternion ResolveRotation(Vector3 shadowOffset, float verticalSign, Quaternion fallback)
        {
            Vector3 fallbackDirection = fallback * Vector3.back;
            float resolvedVerticalSign = Mathf.Abs(verticalSign) > C_MIN_VOLUME_WEIGHT
                ? Mathf.Sign(verticalSign)
                : (Mathf.Abs(fallbackDirection.y) > C_MIN_VOLUME_WEIGHT ? Mathf.Sign(fallbackDirection.y) : -1f);

            Vector3 direction = new Vector3(shadowOffset.x, resolvedVerticalSign, shadowOffset.z);
            if (direction.sqrMagnitude <= C_MIN_VOLUME_WEIGHT)
            {
                direction = new Vector3(0f, resolvedVerticalSign, 0f);
            }

            direction.Normalize();
            Vector3 up = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.99f
                ? Vector3.forward
                : Vector3.up;
            return Quaternion.LookRotation(-direction, up);
        }
    }
}
