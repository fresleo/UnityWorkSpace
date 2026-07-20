using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityPie
{
    /// <summary>
    /// 饼系统
    /// </summary>
    [InitializeOnLoad]
    public class PieSystem
    {
        private const float c_CenterMarkRadius = 10;
        private const int c_Radius = 150;
        private const int c_AnimationDuration = 120;
        
        private const KeyCode c_ShortcutKey = KeyCode.U;

        private static SceneView s_targetSceneView;
        private static int s_lastSceneViewID;
        
        private static VisualElement s_pieRoot;

        private static Vector2 s_size = new(100, 100);

        public class Pie
        {
            public string path;
            
            public Pie(string path)
            {
                this.path = path;
            }

            public Pie parentPie;
            public List<Pie> subPie = new();
            public Action onTrigger;
        }
        
        private static Pie s_rootPie = new("root");
        private static Pie s_currentPie;
        
        public static bool IsVisible => s_pieRoot != null && s_pieRoot.style.display == DisplayStyle.Flex;
        
        
        // [Shortcut("PieSystem.Open", KeyCode.U)]
        private static void TogglePie()
        {
            if (s_pieRoot == null)
            {
                return;
            }

            s_pieRoot.style.display = s_pieRoot.style.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None;
            if (IsVisible)
            {
                Vector2 mousePosition = Event.current.mousePosition; // GUIUtility.ScreenToGUIPoint(Event.current.mousePosition);
                s_pieRoot.transform.position = mousePosition;

                RefreshPie();

                s_targetSceneView.Repaint();
            }
        }

        private static void RefreshPie()
        {
            s_pieRoot.Clear();
            
            // 中心位置标记
            // var centerMark = new VisualElement()
            // {
            //     style = 
            //     {
            //         position = Position.Absolute,
            //         width = c_CenterMarkRadius,
            //         height = c_CenterMarkRadius,
            //         backgroundColor = Color.magenta,
            //         borderTopLeftRadius = c_CenterMarkRadius * 0.5f,
            //         borderTopRightRadius = c_CenterMarkRadius * 0.5f,
            //         borderBottomLeftRadius = c_CenterMarkRadius * 0.5f,
            //         borderBottomRightRadius = c_CenterMarkRadius * 0.5f,
            //         left = c_CenterMarkRadius * -0.5f,
            //         top = c_CenterMarkRadius * -0.5f,
            //     }
            // };
            // s_pieRoot.Add(centerMark);

            // 返回根按钮
            s_pieRoot.Add(NewPieButton("返回根", () => OpenPie(s_rootPie)));

            // 返回按钮
            if (s_currentPie.parentPie is not null)
            {
                s_pieRoot.Add(NewPieButton(" < ", () => OpenPie(s_currentPie.parentPie)));
            }

            // 进入子级按钮
            foreach (var menu in s_currentPie.subPie)
            {
                s_pieRoot.Add(NewPieButton(menu.subPie.Count > 0 ? menu.path + " > " : menu.path, menu.onTrigger));
            }

            // 按钮动画
            int i = 0;
            foreach (var item in s_pieRoot.Children())
            {
                item.transform.position = Vector3.zero;
                if (i == 0)
                {
                    item.RegisterCallback<GeometryChangedEvent>(_ =>
                    {
                        item.transform.position = new Vector3(item.resolvedStyle.width * -0.5f, item.resolvedStyle.height * -0.5f, 0);
                    });
                }
                else
                {
                    Vector2 toPosition = CirclePoint(c_Radius, i, s_pieRoot.childCount);
                    
                    // 延迟执行位置调整，确保 resolvedStyle 可用
                    item.RegisterCallback<GeometryChangedEvent>(_ =>
                    {
                        toPosition.x -= item.resolvedStyle.width * 0.5f;
                        toPosition.y -= item.resolvedStyle.height * 0.5f;

                        item.experimental.animation.Position(toPosition, c_AnimationDuration);
                    });
                }
                i++;
            }
        }
        
        private static void OpenPie(Pie targetPie)
        {
            s_currentPie = targetPie;
            RefreshPie();
        }

        private static Button NewPieButton(string label, Action callback)
        {
            Button newBtn = new Button(callback)
            {
                text = label,
                style =
                {
                    position = Position.Absolute,
                    paddingLeft = 10,
                    paddingRight = 10,
                    paddingTop = 6,
                    paddingBottom = 6,
                },
            };
            return newBtn;
        }
        
        private static Vector2 CirclePoint(float radius, float xIndex, float xCount)
        {
            float angle = xIndex / xCount * 360f;
            Vector2 pos = new Vector2
            {
                x = radius * Mathf.Sin(angle * Mathf.Deg2Rad),
                y = radius * Mathf.Cos(angle * Mathf.Deg2Rad),
            };
            
            return pos;
        }

        
        // 注册引擎事件
        static PieSystem()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
            
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        private static void OnSceneGUI(SceneView view)
        {
            if (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseDown || Event.current.button == 1)
            {
                if (IsVisible)
                {
                    TogglePie();
                }
            }
            else
            {
                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == c_ShortcutKey && !IsVisible)
                {
                    TogglePie();
                }
            }
        }

        private static void OnUpdate()
        {
            // 获取当前 SceneView
            s_targetSceneView = SceneView.currentDrawingSceneView ?? SceneView.lastActiveSceneView;

            // 当 SceneView 有改变时
            if (s_targetSceneView && s_lastSceneViewID != s_targetSceneView.GetInstanceID())
            {
                s_lastSceneViewID = s_targetSceneView.GetInstanceID();
                CleanUpPreviousPie();
            }

            // 确保初始化了饼
            if (s_targetSceneView != null && s_targetSceneView.rootVisualElement != null)
            {
                if (s_pieRoot == null)
                {
                    Init();
                }
            }
        }

        public static void CleanUpPreviousPie()
        {
            if (s_pieRoot != null)
            {
                s_pieRoot.RemoveFromHierarchy();
                s_pieRoot = null;
            }
        }
        
        private static void Init()
        {
            CleanUpPreviousPie();

            // var methods = AppDomain.CurrentDomain.GetAssemblies()
            //                        .SelectMany(x => x.GetTypes())
            //                        .Where(x => x.IsClass)
            //                        .SelectMany(x => x.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            //                        .Where(x => x.GetCustomAttributes(typeof(PieMenuAttribute), false).FirstOrDefault() != null);
            var methods = TypeCache.GetMethodsWithAttribute<PieMenuAttribute>();

            s_pieRoot = new VisualElement()
            {
                style =
                {
                    position = Position.Absolute,
                }
            };
            s_pieRoot.style.width = s_size.x;
            s_pieRoot.style.height = s_size.y;
            s_pieRoot.style.display = DisplayStyle.None;
            // pieRoot.style.backgroundColor = Color.black;
            
            s_targetSceneView.rootVisualElement.Add(s_pieRoot); // 添加到窗口中

            s_rootPie.subPie.Clear();
            foreach (var method in methods)
            {
                var attribute = (PieMenuAttribute)method.GetCustomAttributes(typeof(PieMenuAttribute), false).First();
                CreatePie(attribute.path, () => method.Invoke(null, null));
            }

            s_currentPie = s_rootPie;
        }

        /// <summary>
        /// 创建饼
        /// </summary>
        /// <param name="fullPath">菜单全路径</param>
        /// <param name="method">要触发的菜单方法</param>
        public static void CreatePie(string fullPath, Action method)
        {
            string[] paths = fullPath.Split('/');
            Pie pie = s_rootPie;

            // 为每个路径段创建 sub 饼图
            if (paths.Length > 1)
            {
                for (int i = 0, max = paths.Length - 1; i < max; i++)
                {
                    string path = paths[i];

                    // 查找现有饼图路径
                    Pie tempPie = pie.subPie.Find(x => x.path == path);
                    // 缺少的话就创建1个新的饼
                    if (tempPie == null)
                    {
                        tempPie = new Pie(path);
                        tempPie.parentPie = pie;
                        tempPie.onTrigger = () => OpenPie(tempPie);
                        
                        pie.subPie.Add(tempPie);
                    }

                    pie = tempPie;
                }
            }

            // 创建最后的饼
            var targetPie = new Pie(paths.Last());
            targetPie.onTrigger = () =>
            {
                TogglePie();
                method?.Invoke();
            };
            pie.subPie.Add(targetPie);
        }
        
    }
}