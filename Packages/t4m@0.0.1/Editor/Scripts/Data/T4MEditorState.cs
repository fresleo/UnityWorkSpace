/********************************************************
 * File:    T4MEditorState.cs
 * Description: T4M 编辑器全局状态管理
 *********************************************************/

using T4MEditor.Services;
using UnityEngine;

namespace T4MEditor.Data
{
    /// <summary>
    /// 编辑器全局状态，集中管理所有跨类共享状态
    /// 替代 T4MSC 上的 public static 字段群
    /// </summary>
    public class T4MEditorState
    {
        #region Singleton

        private static T4MEditorState _instance;

        /// <summary>
        /// 单例实例
        /// </summary>
        public static T4MEditorState Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new T4MEditorState();
                }
                return _instance;
            }
        }

        /// <summary>
        /// 私有构造函数，初始化笔刷设置和图层数组
        /// </summary>
        private T4MEditorState()
        {
            Brush = new T4MBrushSettings();
            Layers = new T4MTerrainLayer[6];
            for (int i = 0; i < 6; i++)
            {
                Layers[i] = new T4MTerrainLayer(i);
            }
        }

        #endregion

        #region Brush State

        /// <summary>
        /// 笔刷设置
        /// </summary>
        public T4MBrushSettings Brush { get; private set; }

        #endregion

        #region Terrain Layers

        /// <summary>
        /// 地形图层数组 (最多6层)
        /// </summary>
        public T4MTerrainLayer[] Layers { get; private set; }

        /// <summary>
        /// 笔刷层数量 (2/3/4)
        /// </summary>
        public int LayerCount = 4;

        #endregion

        #region Control Map

        /// <summary>
        /// 控制图1
        /// </summary>
        public Texture2D MaskTex;

        /// <summary>
        /// 控制图2（用于5-6层）
        /// </summary>
        public Texture2D MaskTex2;

        /// <summary>
        /// 控制图 UV 坐标缩放
        /// </summary>
        public float MaskUVCoord = 1f;

        /// <summary>
        /// 控制图是否存在未保存到磁盘的修改。
        /// </summary>
        public bool HasUnsavedControlMapChanges { get; private set; }

        #endregion

        #region Selection

        /// <summary>
        /// 当前选中的 T4M 对象
        /// </summary>
        public Transform CurrentSelect;

        /// <summary>
        /// 当前选中对象的材质
        /// </summary>
        public Material CurrentMaterial;

        #endregion

        #region Preview

        /// <summary>
        /// 预览投影器
        /// </summary>
        public Projector Preview;

        /// <summary>
        /// 是否激活绘制
        /// </summary>
        public bool IsActivated = true;

        #endregion

        #region Menu State

        /// <summary>
        /// 当前菜单工具栏索引
        /// </summary>
        public int MenuToolbar;

        /// <summary>
        /// 绘制面板子菜单索引
        /// </summary>
        public int PaintMenuIndex;

        #endregion

        #region Methods

        /// <summary>
        /// 重置状态
        /// </summary>
        public void Reset()
        {
            Brush = new T4MBrushSettings();
            MaskTex = null;
            MaskTex2 = null;
            CurrentSelect = null;
            CurrentMaterial = null;
            Preview = null;
            IsActivated = true;
            MenuToolbar = 0;
            PaintMenuIndex = 0;
            HasUnsavedControlMapChanges = false;
        }

        /// <summary>
        /// 标记控制图已在内存中修改，但尚未保存到磁盘。
        /// </summary>
        public void MarkControlMapsDirty()
        {
            HasUnsavedControlMapChanges = true;
        }

        /// <summary>
        /// 如果控制图存在未保存修改，则保存当前控制图。
        /// </summary>
        /// <returns>无需保存或保存成功时返回 true。</returns>
        public bool SaveDirtyControlMaps()
        {
            if (!HasUnsavedControlMapChanges)
            {
                return true;
            }

            return SaveCurrentControlMaps();
        }

        /// <summary>
        /// 保存当前状态持有的控制图。
        /// </summary>
        /// <returns>保存是否成功。</returns>
        public bool SaveCurrentControlMaps()
        {
            var context = new T4MPaintContext
            {
                ControlMap = MaskTex,
                ControlMap2 = MaskTex2
            };

            bool saved = T4MBrushPainter.SaveControlMaps(context);
            if (saved)
            {
                HasUnsavedControlMapChanges = false;
            }

            return saved;
        }

        /// <summary>
        /// 从材质加载图层数据
        /// </summary>
        /// <param name="mat">目标材质</param>
        public void LoadLayersFromMaterial(Material mat)
        {
            if (mat == null) return;

            for (int i = 0; i < Layers.Length; i++)
            {
                Layers[i] = T4MTerrainLayer.FromMaterial(mat, i);
            }

            CurrentMaterial = mat;
        }

        /// <summary>
        /// 将图层数据保存到材质
        /// </summary>
        /// <param name="mat">目标材质</param>
        public void SaveLayersToMaterial(Material mat)
        {
            if (mat == null) return;

            foreach (var layer in Layers)
            {
                layer.ApplyToMaterial(mat);
            }
        }

        /// <summary>
        /// 更新当前选中对象
        /// </summary>
        /// <param name="transform">选中的 Transform</param>
        public void SetCurrentSelect(Transform transform)
        {
            CurrentSelect = transform;

            if (transform != null)
            {
                var t4mObj = transform.GetComponent<T4MObjSC>();
                Material mat = null;
                if (t4mObj != null && t4mObj.T4MMaterial != null)
                {
                    mat = t4mObj.T4MMaterial;
                }
                else
                {
                    var renderer = transform.GetComponent<Renderer>();
                    if (renderer != null)
                        mat = renderer.sharedMaterial;
                }

                if (mat != null)
                {
                    CurrentMaterial = mat;
                    LoadLayersFromMaterial(CurrentMaterial);

                    if (CurrentMaterial.HasProperty("_Control"))
                    {
                        MaskTex = CurrentMaterial.GetTexture("_Control") as Texture2D;
                    }
                    if (CurrentMaterial.HasProperty("_Control2"))
                    {
                        MaskTex2 = CurrentMaterial.GetTexture("_Control2") as Texture2D;
                    }
                }
                else
                {
                    CurrentMaterial = null;
                    MaskTex = null;
                    MaskTex2 = null;
                }
            }
            else
            {
                CurrentMaterial = null;
                MaskTex = null;
                MaskTex2 = null;
            }
        }

        #endregion
    }
}
