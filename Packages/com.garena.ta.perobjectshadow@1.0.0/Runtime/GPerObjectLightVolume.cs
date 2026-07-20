/*******************************************************************************
 * File: GPerObjectLightVolume.cs
 * Author: Codex
 * Date: 2026-05-08
 * Description: 逐物体阴影局部光照 Volume，用于在场景区域内覆盖阴影朝向和强度。
 *
 * Notice: Volume 只负责提供区域配置，实际采样和渲染接线由 GPerObjectShadowManager 处理。
 *******************************************************************************/

using UnityEngine;

namespace Garena.TA
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Rendering/Garena TA/Per Object Light Volume")]
    public class GPerObjectLightVolume : MonoBehaviour
    {
        private const float C_MIN_SIZE = 0.01f;

        /// <summary>
        /// Volume 盒子在当前 Transform 局部空间中的中心。
        /// </summary>
        public Vector3 center = Vector3.zero;

        /// <summary>
        /// Volume 盒子在当前 Transform 局部空间中的尺寸。
        /// </summary>
        public Vector3 size = new Vector3(4f, 4f, 4f);

        /// <summary>
        /// Volume 内逐物体阴影使用的光照朝向，单位为欧拉角。
        /// </summary>
        public Vector3 lightRotation = new Vector3(50f, -30f, 0f);

        /// <summary>
        /// 阴影强度倍率，0 表示不产生局部逐物体阴影，1 表示使用完整阴影强度。
        /// </summary>
        [Range(0f, 1f)]
        public float intensity = 1f;

        /// <summary>
        /// Volume 对默认光照配置的覆盖权重，多个 Volume 重叠时会按权重混合。
        /// </summary>
        [Range(0f, 1f)]
        public float weight = 1f;

        /// <summary>
        /// 选中时是否绘制半透明实体盒子。
        /// </summary>
        public bool showSolidGizmo = true;

        /// <summary>
        /// Volume 盒子局部空间到世界空间的矩阵。
        /// </summary>
        public Matrix4x4 VolumeLocalToWorldMatrix
        {
            get
            {
                return transform.localToWorldMatrix * Matrix4x4.TRS(center, Quaternion.identity, size);
            }
        }

        /// <summary>
        /// Volume 盒子世界空间到局部空间的矩阵。
        /// </summary>
        public Matrix4x4 VolumeWorldToLocalMatrix
        {
            get
            {
                return VolumeLocalToWorldMatrix.inverse;
            }
        }

        /// <summary>
        /// 局部光照朝向。
        /// </summary>
        public Quaternion LightRotation
        {
            get
            {
                return Quaternion.Euler(lightRotation);
            }
        }

        /// <summary>
        /// 局部光照方向。
        /// </summary>
        public Vector3 LightDirection
        {
            get
            {
                return LightRotation * Vector3.back;
            }
        }

        private void OnEnable()
        {
            ValidateData();
            GPerObjectShadowManager.Instance.AddLightVolume(this);
        }

        private void OnDisable()
        {
            GPerObjectShadowManager.Instance.RemoveLightVolume(this);
        }

        private void OnValidate()
        {
            ValidateData();
        }

        /// <summary>
        /// 检查世界坐标点是否在 Volume 盒子内。
        /// </summary>
        /// <param name="worldPosition">待检测的世界坐标。</param>
        /// <returns>点在 Volume 内时返回 true。</returns>
        public bool Contains(Vector3 worldPosition)
        {
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition) - center;
            Vector3 halfSize = size * 0.5f;

            return Mathf.Abs(localPosition.x) <= halfSize.x
                && Mathf.Abs(localPosition.y) <= halfSize.y
                && Mathf.Abs(localPosition.z) <= halfSize.z;
        }

        /// <summary>
        /// 检查世界空间包围盒是否与 Volume 盒子相交。
        /// </summary>
        /// <param name="bounds">待检测的世界空间包围盒。</param>
        /// <returns>包围盒与 Volume 相交时返回 true。</returns>
        public bool AffectsBounds(Bounds bounds)
        {
            Bounds localBounds = GPerObjectShadowUtils.TransformBounds(bounds, transform.worldToLocalMatrix);
            Bounds localVolumeBounds = new Bounds(center, size);
            return localVolumeBounds.Intersects(localBounds);
        }

        /// <summary>
        /// 获取 Volume 对指定包围盒的有效权重。
        /// </summary>
        /// <param name="bounds">待检测的世界空间包围盒。</param>
        /// <param name="samplePosition">用于计算距离衰减的世界空间采样点。</param>
        /// <returns>不受影响时返回 0，否则返回距离衰减后的权重。</returns>
        public float GetInfluence(Bounds bounds, Vector3 samplePosition)
        {
            if (!isActiveAndEnabled || weight <= 0f)
            {
                return 0f;
            }

            if (!AffectsBounds(bounds))
            {
                return 0f;
            }

            Vector3 localPosition = transform.InverseTransformPoint(samplePosition) - center;
            return Mathf.Clamp01(weight) * GetBoxFalloff(localPosition);
        }

        /// <summary>
        /// 获取合法化后的阴影强度。
        /// </summary>
        /// <returns>限定在 0 到 1 范围内的强度值。</returns>
        public float GetIntensity()
        {
            return Mathf.Clamp01(intensity);
        }

        private void ValidateData()
        {
            size.x = Mathf.Max(C_MIN_SIZE, Mathf.Abs(size.x));
            size.y = Mathf.Max(C_MIN_SIZE, Mathf.Abs(size.y));
            size.z = Mathf.Max(C_MIN_SIZE, Mathf.Abs(size.z));
            intensity = Mathf.Clamp01(intensity);
            weight = Mathf.Clamp01(weight);
        }

        private float GetBoxFalloff(Vector3 localPosition)
        {
            Vector3 halfSize = size * 0.5f;
            Vector3 normalizedPosition = new Vector3(
                Mathf.Abs(localPosition.x) / halfSize.x,
                Mathf.Abs(localPosition.y) / halfSize.y,
                Mathf.Abs(localPosition.z) / halfSize.z);

            if (normalizedPosition.x >= 1f || normalizedPosition.y >= 1f || normalizedPosition.z >= 1f)
            {
                return 0f;
            }

            return (1f - normalizedPosition.x) * (1f - normalizedPosition.y) * (1f - normalizedPosition.z);
        }

#if UNITY_EDITOR
        private static readonly Color s_solidColor = new Color(0.95f, 0.65f, 0.1f, 0.16f);
        private static readonly Color s_wireColor = new Color(1f, 0.72f, 0.12f, 0.9f);
        private static readonly Color s_directionColor = new Color(1f, 0.95f, 0.35f, 0.95f);

        private void OnDrawGizmos()
        {
            DrawGizmos(false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawGizmos(true);
        }

        private void DrawGizmos(bool selected)
        {
            Matrix4x4 prevMatrix = Gizmos.matrix;
            Color prevColor = Gizmos.color;

            Gizmos.matrix = VolumeLocalToWorldMatrix;
            if (selected && showSolidGizmo)
            {
                Gizmos.color = s_solidColor;
                Gizmos.DrawCube(Vector3.zero, Vector3.one);
            }

            Gizmos.color = s_wireColor;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = s_directionColor;
            Vector3 worldCenter = transform.TransformPoint(center);
            Vector3 direction = -LightDirection.normalized;
            float length = Mathf.Max(transform.lossyScale.magnitude, 1f);
            Gizmos.DrawLine(worldCenter, worldCenter + direction * length);

            Gizmos.matrix = prevMatrix;
            Gizmos.color = prevColor;
        }
#endif
    }
}
