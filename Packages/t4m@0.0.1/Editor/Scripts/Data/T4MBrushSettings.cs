/********************************************************
 * File:    T4MBrushSettings.cs
 * Description: T4M 笔刷设置数据模型
 *********************************************************/

using System;
using UnityEngine;

namespace T4MEditor.Data
{
    /// <summary>
    /// 笔刷预览模式
    /// </summary>
    public enum T4MPaintHandle
    {
        Classic = 0,
        Follow_Normal_Circle,
        Follow_Normal_WireCircle,
        Hide_preview
    }

    /// <summary>
    /// 笔刷设置数据类，封装所有笔刷相关状态
    /// 替代 T4MSC 上的 public static 字段
    /// </summary>
    [Serializable]
    public class T4MBrushSettings
    {
        public const float C_MIN_SIZE = 0.1f;
        public const float C_MAX_SIZE = 36f;

        /// <summary>
        /// 笔刷大小 (0.1-36)
        /// </summary>
        [SerializeField]
        [Range(C_MIN_SIZE, C_MAX_SIZE)]
        private float _size = 16f;

        public float Size
        {
            get { return _size; }
            set { SetSize(value); }
        }

        /// <summary>
        /// 笔刷大小百分比（实际像素大小）
        /// </summary>
        public int SizeInPourcent;

        /// <summary>
        /// 笔刷强度 (0.05-1)
        /// </summary>
        [Range(0.05f, 1f)]
        public float Strength = 0.5f;

        /// <summary>
        /// 当前选中的笔刷索引
        /// </summary>
        public int SelectedBrush;

        /// <summary>
        /// 当前选中的纹理索引
        /// </summary>
        public int SelectedTexture;

        /// <summary>
        /// 笔刷 Alpha 值数组
        /// </summary>
        public float[] Alpha;

        /// <summary>
        /// 预览模式
        /// </summary>
        public T4MPaintHandle PreviewMode = T4MPaintHandle.Classic;

        /// <summary>
        /// 是否使用 UV4
        /// </summary>
        public bool UseUV4;

        /// <summary>
        /// 目标绘制颜色（控制图1）
        /// </summary>
        public Color TargetColor;

        /// <summary>
        /// 目标绘制颜色（控制图2）
        /// </summary>
        public Color TargetColor2;

        /// <summary>
        /// 笔刷纹理数组
        /// </summary>
        public Texture[] BrushTextures;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public T4MBrushSettings()
        {
            Size = 16f;
            Strength = 0.5f;
            PreviewMode = T4MPaintHandle.Classic;
        }

        public static float ClampSize(float size)
        {
            return Mathf.Clamp(size, C_MIN_SIZE, C_MAX_SIZE);
        }

        public void SetSize(float size)
        {
            _size = ClampSize(size);
        }

        /// <summary>
        /// 获取选中笔刷的基础纹理
        /// </summary>
        public Texture GetSelectedBrushTexture()
        {
            if (BrushTextures != null && BrushTextures.Length > 0 && SelectedBrush < BrushTextures.Length)
            {
                return BrushTextures[SelectedBrush];
            }
            return null;
        }

        /// <summary>
        /// 获取指定索引的笔刷纹理
        /// </summary>
        public Texture GetBrushTexture(int index)
        {
            if (BrushTextures != null && BrushTextures.Length > 0 && index >= 0 && index < BrushTextures.Length)
            {
                return BrushTextures[index];
            }
            return null;
        }

        /// <summary>
        /// 更新笔刷 Alpha 数组。笔刷在控制图上的像素半径与旧版 T4MSC 一致：<c>(Size * controlMapWidth) / 100</c>。
        /// </summary>
        /// <param name="brushTexture">笔刷形状纹理</param>
        /// <param name="controlMapWidth">控制图宽度（与 <see cref="T4MEditorState.MaskTex"/> 一致）</param>
        public void UpdateBrushAlpha(Texture2D brushTexture, int controlMapWidth)
        {
            if (brushTexture == null)
            {
                Alpha = null;
                return;
            }

            if (controlMapWidth <= 0)
            {
                controlMapWidth = 1024;
            }

            SetSize(_size);
            SizeInPourcent = Mathf.Max(1, Mathf.RoundToInt(Size * controlMapWidth / 100f));
            Alpha = new float[SizeInPourcent * SizeInPourcent];

            for (int i = 0; i < SizeInPourcent; i++)
            {
                for (int j = 0; j < SizeInPourcent; j++)
                {
                    float u = (float)i / SizeInPourcent;
                    float v = (float)j / SizeInPourcent;
                    Alpha[j * SizeInPourcent + i] = brushTexture.GetPixelBilinear(u, v).a;
                }
            }
        }

        /// <summary>
        /// 根据选中的纹理索引设置目标颜色
        /// </summary>
        public void UpdateTargetColor()
        {
            switch (SelectedTexture)
            {
                case 0:
                    TargetColor = new Color(1, 0, 0, 0);
                    TargetColor2 = new Color(0, 0, 0, 0);
                    break;
                case 1:
                    TargetColor = new Color(0, 1, 0, 0);
                    TargetColor2 = new Color(0, 0, 0, 0);
                    break;
                case 2:
                    TargetColor = new Color(0, 0, 1, 0);
                    TargetColor2 = new Color(0, 0, 0, 0);
                    break;
                case 3:
                    TargetColor = new Color(0, 0, 0, 1);
                    TargetColor2 = new Color(0, 0, 0, 0);
                    break;
                case 4:
                    TargetColor = new Color(0, 0, 0, 0);
                    TargetColor2 = new Color(1, 0, 0, 0);
                    break;
                case 5:
                    TargetColor = new Color(0, 0, 0, 0);
                    TargetColor2 = new Color(0, 1, 0, 0);
                    break;
                default:
                    TargetColor = new Color(1, 0, 0, 0);
                    TargetColor2 = new Color(0, 0, 0, 0);
                    break;
            }
        }
    }
}
