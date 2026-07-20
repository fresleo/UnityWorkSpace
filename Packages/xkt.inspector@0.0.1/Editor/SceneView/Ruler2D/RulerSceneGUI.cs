/*******************************************************************************
 * File: RulerSceneGUI.cs
 * Author: WangYu
 * Date: 2026-02-12
 * Description: 类或文件功能描述
 *
 * Notice: 注意事项描述（无可省略）
 *******************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace XKT.Editor.SceneViewRuler2D
{
    /// <summary>
    /// 这个工具是为在 2D 世界中调试而制作的。
    /// 在 Scene 视图的顶部和左侧，显示当前视口范围内的世界坐标刻度
    /// </summary>
    [InitializeOnLoad]
    public static class RulerSceneGUI
    {
        /// <summary>
        /// 显示状态
        /// </summary>
        public static bool show = false;

        private static Camera _sceneViewCamera;
        private static readonly Color _lineColor = new (0.22f, 0.22f, 0.22f);

        // 标尺条厚度（像素）。
        private const int CRULER_THICKNESS = 16;

        // 纵向标尺底部与视口底边的预留间距（像素），避开底部状态栏。
        private const int CRULER_BOTTOM_GAP = 5;

        // 横向刻度标签相对刻度线的左侧内边距（像素）。
        private const float CLABEL_LEFT_PADDING = 2;

        // 小格刻度线长度相对标尺条厚度的比例（大格为满厚度，小格为该比例）。
        private const float CSUBTICK_LENGTH_RATIO = 0.75f;
        
        // 单个大格在屏幕上的目标像素宽度。
        // 每格世界单位数会按此目标自适应，从而保证任意视角下格子总数恒定受限于 视口尺寸 / 该值，不会刷出海量刻度。
        private const float CTARGET_CELL_PIXELS = 64;

        // 1 世界单位在某轴上投影到屏幕的像素数小于该阈值时，认为该轴几乎垂直于屏幕，
        // 无法形成有效刻度，跳过对应方向的标尺（仅跳过该方向，另一方向仍正常绘制）。
        private const float CMIN_PIXELS_PER_UNIT = 0.001f;
        
        // 判定刻度值是否为整数的浮点误差容差。
        private const float CINTEGER_EPSILON = 0.0001f;

        // 单条标尺循环的迭代次数兜底上限，防止任何异常情况下卡死主线程。
        private const int CMAX_RULER_ITERATIONS = 1024;

        // 整数刻度标签字符串缓存，避免每帧重复 ToString 产生 GC 垃圾。
        private static readonly Dictionary<long, string> _labelCache = new ();

        // 标签缓存条目上限，超过则清空，保证长时间平移大范围坐标时内存有界。
        private const int CMAX_LABEL_CACHE = 2048;

        
        static RulerSceneGUI()
        {
            EditorApplication.update += Update;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        static void Update()
        {
            SceneView lastActiveSceneView = SceneView.lastActiveSceneView;
            if (lastActiveSceneView == null)
            {
                _sceneViewCamera = null;
                return;
            }

            _sceneViewCamera = lastActiveSceneView.camera;
        }

        static void OnSceneGUI(SceneView sceneView)
        {
            if (_sceneViewCamera == null)
            {
                return;
            }

            if (show)
            {
                if (_sceneViewCamera.orthographic)
                {
                    DisplayRulerBars(sceneView);
                }
            }
            else
            {
                // 关闭标尺时及时释放缓存，避免长期驻留内存。
                if (_labelCache.Count > 0)
                {
                    _labelCache.Clear();
                }
            }
        }

        private static void DisplayRulerBars(SceneView sceneView)
        {
            int size = CRULER_THICKNESS;
            int viewWidth = (int)sceneView.position.width;
            int viewHeight = (int)sceneView.position.height;
            // 纵向标尺底部边界（屏幕 Y）
            int verticalBottom = viewHeight - size - CRULER_BOTTOM_GAP;

            Handles.BeginGUI();

            Color prevBg = GUI.backgroundColor;

            // 左上角背景
            GUI.backgroundColor = Color.green;
            GUI.Box(new Rect(0, 0, size, size), "", EditorStyles.toolbar);
            GUI.backgroundColor = prevBg;
            // 横向背景
            GUI.Box(new Rect(size, 0, viewWidth - size, size), "", EditorStyles.textArea);
            // 纵向背景
            GUI.Box(new Rect(0, size, size, verticalBottom - size), "", EditorStyles.textArea);

            Vector2 originGui = HandleUtility.WorldToGUIPoint(Vector3.zero);
            Vector2 guiRight = HandleUtility.WorldToGUIPoint(Vector3.right);
            Vector2 guiUp = HandleUtility.WorldToGUIPoint(Vector3.up);

            // 世界 X 轴投影到屏幕横向的像素数（带符号），世界 Y 轴投影到屏幕纵向的像素数
            // （带符号，向上为正；屏幕 Y 向下，所以用 originGui.y - guiUp.y）。
            float pixelsPerUnitX = guiRight.x - originGui.x;
            float pixelsPerUnitY = originGui.y - guiUp.y;

            Handles.color = _lineColor;

            // 仅在该轴投影足够明显时绘制对应方向标尺，避免除零与无意义刻度。
            if (Mathf.Abs(pixelsPerUnitX) >= CMIN_PIXELS_PER_UNIT)
            {
                DrawHorizontalRuler(originGui.x, pixelsPerUnitX, size, viewWidth);
            }

            if (Mathf.Abs(pixelsPerUnitY) >= CMIN_PIXELS_PER_UNIT)
            {
                DrawVerticalRuler(originGui.y, pixelsPerUnitY, size, verticalBottom);
            }

            Handles.EndGUI();
        }

        /// <summary>
        /// 绘制顶部横向标尺。
        /// 每个大格的世界单位数按横向像素密度自适应，因此可见大格数量恒定受限，任意视角都不会产生海量刻度或死循环。
        /// </summary>
        /// <param name="originScreenX">世界原点对应的屏幕 X 像素</param>
        /// <param name="pixelsPerUnitX">1 世界单位在横向的屏幕像素数（带符号）</param>
        /// <param name="size">标尺条厚度（像素）</param>
        /// <param name="viewWidth">视口宽度（像素）</param>
        private static void DrawHorizontalRuler(float originScreenX, float pixelsPerUnitX, int size, int viewWidth)
        {
            float pixelsAbs = Mathf.Abs(pixelsPerUnitX);
            float unitsPerCell = ChooseNiceUnitsPerCell(pixelsAbs);
            int numSubcells = GetSubdivisions(unitsPerCell);
            float minorStep = unitsPerCell / numSubcells;
            float cellPixelWidth = unitsPerCell * pixelsAbs;

            // 屏幕 [size, viewWidth] 区间对应的世界 X 范围。
            float worldAtLeft = (size - originScreenX) / pixelsPerUnitX;
            float worldAtRight = (viewWidth - originScreenX) / pixelsPerUnitX;
            float worldMin = Mathf.Min(worldAtLeft, worldAtRight);
            float worldMax = Mathf.Max(worldAtLeft, worldAtRight);

            float firstMajor = Mathf.Floor(worldMin / unitsPerCell) * unitsPerCell;

            int iteration = 0;
            for (float worldX = firstMajor; worldX <= worldMax; worldX += unitsPerCell)
            {
                if (++iteration > CMAX_RULER_ITERATIONS)
                {
                    break;
                }

                float screenX = originScreenX + worldX * pixelsPerUnitX;
                if (screenX >= size && screenX <= viewWidth)
                {
                    Handles.DrawLine(new Vector2(screenX, 0), new Vector2(screenX, size));
                    GUI.Box(new Rect(screenX + CLABEL_LEFT_PADDING, 0, cellPixelWidth, size),
                        FormatTick(worldX), EditorStyles.label);
                }

                for (int i = 1; i < numSubcells; i++)
                {
                    float screenSubX = originScreenX + (worldX + i * minorStep) * pixelsPerUnitX;
                    if (screenSubX >= size && screenSubX <= viewWidth)
                    {
                        Handles.DrawLine(new Vector2(screenSubX, size),
                            new Vector2(screenSubX, size * CSUBTICK_LENGTH_RATIO));
                    }
                }
            }
        }

        /// <summary>
        /// 绘制左侧纵向标尺。
        /// 每个大格的世界单位数按纵向像素密度自适应，因此可见大格数量恒定受限，任意视角都不会产生海量刻度或死循环。
        /// </summary>
        /// <param name="originScreenY">世界原点对应的屏幕 Y 像素</param>
        /// <param name="pixelsPerUnitY">1 世界单位在纵向的屏幕像素数（带符号，向上为正）</param>
        /// <param name="size">标尺条厚度（像素）</param>
        /// <param name="bottom">纵向标尺底部边界（屏幕 Y）</param>
        private static void DrawVerticalRuler(float originScreenY, float pixelsPerUnitY, int size, int bottom)
        {
            float pixelsAbs = Mathf.Abs(pixelsPerUnitY);
            float unitsPerCell = ChooseNiceUnitsPerCell(pixelsAbs);
            int numSubcells = GetSubdivisions(unitsPerCell);
            float minorStep = unitsPerCell / numSubcells;
            float cellPixelHeight = unitsPerCell * pixelsAbs;

            // 屏幕 [size, bottom] 区间对应的世界 Y 范围（屏幕 Y 向下，世界 Y 向上）。
            float worldAtTop = (originScreenY - size) / pixelsPerUnitY;
            float worldAtBottom = (originScreenY - bottom) / pixelsPerUnitY;
            float worldMin = Mathf.Min(worldAtTop, worldAtBottom);
            float worldMax = Mathf.Max(worldAtTop, worldAtBottom);

            float firstMajor = Mathf.Floor(worldMin / unitsPerCell) * unitsPerCell;

            int iteration = 0;
            for (float worldY = firstMajor; worldY <= worldMax; worldY += unitsPerCell)
            {
                if (++iteration > CMAX_RULER_ITERATIONS)
                {
                    break;
                }

                float screenY = originScreenY - worldY * pixelsPerUnitY;
                if (screenY >= size && screenY <= bottom)
                {
                    Handles.DrawLine(new Vector2(0, screenY), new Vector2(size, screenY));
                    GUI.Box(new Rect(0, screenY, size, cellPixelHeight), FormatTick(worldY), EditorStyles.wordWrappedLabel);
                }

                for (int i = 1; i < numSubcells; i++)
                {
                    float screenSubY = originScreenY - (worldY + i * minorStep) * pixelsPerUnitY;
                    if (screenSubY >= size && screenSubY <= bottom)
                    {
                        Handles.DrawLine(new Vector2(size, screenSubY),
                            new Vector2(size * CSUBTICK_LENGTH_RATIO, screenSubY));
                    }
                }
            }
        }

        /// <summary>
        /// 根据该轴“1 世界单位 = 多少像素”，选取一个漂亮数（1/2/5×10ⁿ）作为每格世界单位数，
        /// 使每个大格的屏幕宽度不小于目标像素，从而把可见格子数量限制在固定范围内。
        /// </summary>
        /// <param name="pixelsPerUnitAbs">1 世界单位在该轴上的屏幕像素数（绝对值）</param>
        /// <returns>每个大格代表的世界单位数</returns>
        private static float ChooseNiceUnitsPerCell(float pixelsPerUnitAbs)
        {
            float rawUnits = CTARGET_CELL_PIXELS / pixelsPerUnitAbs;
            return NiceCeil(rawUnits);
        }

        /// <summary>
        /// 将数值向上取整到最近的漂亮数 1/2/5×10ⁿ。
        /// </summary>
        /// <param name="value">原始数值（应大于 0）</param>
        /// <returns>不小于 value 的漂亮数</returns>
        private static float NiceCeil(float value)
        {
            if (value <= 0)
            {
                return 1;
            }

            float exponent = Mathf.Floor(Mathf.Log10(value));
            float powerOfTen = Mathf.Pow(10, exponent);
            float fraction = value / powerOfTen;

            float niceFraction;
            if (fraction <= 1)
            {
                niceFraction = 1;
            }
            else if (fraction <= 2)
            {
                niceFraction = 2;
            }
            else if (fraction <= 5)
            {
                niceFraction = 5;
            }
            else
            {
                niceFraction = 10;
            }

            return niceFraction * powerOfTen;
        }

        /// <summary>
        /// 根据大格的漂亮数基数（1/2/5）返回合适的小格细分数量。
        /// </summary>
        /// <param name="unitsPerCell">每个大格代表的世界单位数</param>
        /// <returns>小格细分数量</returns>
        private static int GetSubdivisions(float unitsPerCell)
        {
            float exponent = Mathf.Floor(Mathf.Log10(unitsPerCell));
            float powerOfTen = Mathf.Pow(10, exponent);
            int baseFraction = Mathf.RoundToInt(unitsPerCell / powerOfTen);

            switch (baseFraction)
            {
                case 1:
                    return 5;
                case 2:
                    return 4;
                case 5:
                    return 5;
                default:
                    return 5;
            }
        }

        /// <summary>
        /// 格式化刻度标签：整数不显示小数，非整数最多保留 3 位小数。
        /// </summary>
        /// <param name="value">刻度对应的世界坐标值</param>
        /// <returns>标签文本</returns>
        private static string FormatTick(float value)
        {
            float rounded = Mathf.Round(value);
            if (Mathf.Abs(value - rounded) < CINTEGER_EPSILON)
            {
                long key = (long)rounded;
                string label;
                if (_labelCache.TryGetValue(key, out label))
                {
                    return label;
                }

                // 缓存超过上限直接清空，保证内存有界。
                if (_labelCache.Count >= CMAX_LABEL_CACHE)
                {
                    _labelCache.Clear();
                }

                label = key.ToString();
                _labelCache[key] = label;
                return label;
            }

            // 非整数刻度（放大很多时才会出现）直接格式化，不进缓存避免键无界膨胀。
            return value.ToString("0.###");
        }
        
    }
}
