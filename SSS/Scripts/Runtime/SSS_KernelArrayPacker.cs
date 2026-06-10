/// <summary>
/// author : calvin
/// date   : 2026-05-29
/// desc   : 把多个材质的 disc kernel（各 N×1）打包成一张 Texture2DArray。
///          每个 slice = 一个材质的采样表，slice 索引 = profile ID。
///          供延迟渲染单 pass SSSS 按 per-pixel profile ID 选核。
///
/// 用法（编辑器）：
///   var array = SSS_KernelArrayPacker.Pack(new[]{ skinKernel, jadeKernel, waxKernel });
///   AssetDatabase.CreateAsset(array, "Assets/.../SSS_KernelArray.asset");
///   → slice 0=skin, 1=jade, 2=wax，与材质 _SSS_ProfileIndex 对应
/// </summary>

using System.Collections.Generic;
using UnityEngine;

namespace Garena.TA.SSS
{
    public static class SSS_KernelArrayPacker
    {
        /// <summary>
        /// 把若干 N×1 kernel 打包成 Texture2DArray。
        /// 所有 kernel 必须同宽（同 sample count）、同格式。
        /// </summary>
        public static Texture2DArray Pack(IList<Texture2D> kernels)
        {
            if (kernels == null || kernels.Count == 0)
            {
                Debug.LogError("[KernelArrayPacker] kernel 列表为空。");
                return null;
            }

            // 以第一张为基准
            var first = kernels[0];
            int width  = first.width;
            int height = first.height;   // 通常 1
            var format = first.format;

            // 校验一致性
            for (int i = 0; i < kernels.Count; i++)
            {
                var k = kernels[i];
                if (k == null)
                {
                    Debug.LogError($"[KernelArrayPacker] slice {i} 为空。");
                    return null;
                }
                if (k.width != width || k.height != height)
                {
                    Debug.LogError($"[KernelArrayPacker] slice {i} 尺寸 " +
                        $"({k.width}x{k.height}) 与基准 ({width}x{height}) 不一致。" +
                        "所有 kernel 必须同 sample count。");
                    return null;
                }
            }

            var array = new Texture2DArray(
                width, height, kernels.Count, format, mipChain: false, linear: true)
            {
                filterMode = FilterMode.Point,   // kernel 是查找表，不能插值
                wrapMode   = TextureWrapMode.Clamp,
                name       = "SSS_KernelArray",
            };

            for (int slice = 0; slice < kernels.Count; slice++)
            {
                // 直接拷贝像素（要求 kernel 可读 Read/Write Enabled）
                Graphics.CopyTexture(kernels[slice], 0, 0, array, slice, 0);
            }

            array.Apply(false, false);
            return array;
        }
    }
}
