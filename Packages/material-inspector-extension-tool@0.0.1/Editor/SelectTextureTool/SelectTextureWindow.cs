using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MaterialInspectorExtensionTool.Editor.PublicExtension;
using UnityEditor;
using UnityEngine;
using static MaterialInspectorExtensionTool.Editor.PublicExtension.RectExtension;

namespace MaterialInspectorExtensionTool.Editor.SelectTextureTool
{
    public class SelectTextureWindow : EditorWindow
    {
        /// <summary>
        /// 窗口数据
        /// </summary>
        public static SelectTextureWindowData s_windowData;
        
        /// <summary>
        /// 渲染图片的组
        /// </summary>
        public static List<DrawTextureGroup> drawTextures = new();
        
        private static Material s_targetMaterial;
        private static string s_targetPropertyName;
        
        private static string s_searchString;

        /// <summary>
        /// 贴图路径信息
        /// </summary>
        public static string texturePathInfo;

        /// <summary>
        /// 外观皮肤
        /// </summary>
        public static GUISkin skin;

        /// <summary>
        /// 贴图尺寸 - 滑动条
        /// </summary>
        public Rect TextureSizeRect => new(position.width - 150, position.height - 18, 70, 20);

        /// <summary>
        /// 信息栏
        /// </summary>
        public Rect InfoRect => new(0, position.height - 20, position.width, 20);

        // 分屏视图
        private static SplitView s_splitView;

        public static SearchArea mySearchArea;
        public static MainArea mainArea, firstLineArea, textureArea;
        
        private static Filter<int> s_sizeFilter;
        private static ToolbarArea s_myToolbarArea;
        private static Filter<TextureWrapMode> s_wrapModeFilter;
        private static List<TextureTools> s_textureToolsList = new();
        private static int s_selectedGroup;
        
        /// <summary>
        /// 修改了材质纹理
        /// </summary>
        public static bool isChangeMaterialTexture = true;
        
        /// <summary>
        /// 贴图单张预览
        /// </summary>
        public static Texture materialTexture;
        
        
        private static void Open(float width = 500, float height = 500)
        {
            LoadData();

            var win = GetWindow<SelectTextureWindow>("纹理选择工具");
            win.minSize = new Vector2(width, height);
            win.Show();
        }

        /// <summary>
        /// 打开窗口
        /// </summary>
        /// <param name="material">材质球</param>
        /// <param name="propertyName">属性名</param>
        public static void Open(Material material, string propertyName)
        {
            s_targetMaterial = material;
            s_targetPropertyName = propertyName;
            Open();
        }

        public static void DrawQuickSelectTextureButton(MaterialEditor materialEditor, MaterialProperty prop, string displayName)
        {
            if (prop.type != MaterialProperty.PropType.Texture)
            {
                return;
            }
            
            float propertyHeight = materialEditor.GetPropertyHeight(prop, displayName);
            Rect controlRect = EditorGUILayout.GetControlRect(true, propertyHeight, EditorStyles.layerMaskField);
            
            materialEditor.ShaderProperty(controlRect, prop, displayName);
            
            var selectRect = controlRect;
            selectRect.width = 70;
            selectRect.height = 15;
            selectRect.x = controlRect.xMax - 130;
            
            if (GUI.Button(selectRect, "快选贴图"))
            {
                Material targetMaterial = materialEditor.target as Material;
                Open(targetMaterial, prop.name);
            }
        }

        private void OnEnable()
        {
            this.minSize = new Vector2(s_windowData.textureSize * 3, s_windowData.textureSize * 2);
            this.titleContent = new GUIContent("Select Texture", Resources.Load<Texture2D>("SelectTextureWindowIcon"));
            
            skin = Resources.Load<GUISkin>("mySkin");
            if (s_windowData.nowMaterial == null)
            {
                int lastIndex = s_windowData.materials.Count - 1;
                s_windowData.nowMaterial = s_windowData.materials[lastIndex];
            }
            
            if (s_windowData.names.Count != 0 && s_windowData.names.Count != drawTextures.Count)
            {
                drawTextures.Clear();
                for (int i = 0; i < s_windowData.paths.Count; i++)
                {
                    string path = s_windowData.paths[i];
                    var dtg = new DrawTextureGroup(path);
                    drawTextures.Add(dtg);
                }
            }

            s_searchString = "";
            
            DrawTextureGroup.IsTextureChange += SetTextureInMaterial;
            
            s_splitView.FirstArea += FirstArea;
            s_splitView.SecondArea += SecondArea;
            s_splitView.DrawDragAndDropRect += DrawDragAndDrropRect;

            mainArea = new MainArea(MainArea.EArrangement.Vertical, "box");
            firstLineArea = new MainArea(MainArea.EArrangement.Horizontal, "box") { Rect = new Rect(0, 0, this.position.width, 20) };
            firstLineArea.Rect = new Rect(0, 0, 0, 20);
            textureArea = new MainArea(MainArea.EArrangement.Vertical, "box");

            mySearchArea = new SearchArea();
            mySearchArea.SearchHistory = s_windowData.seachString;
            mySearchArea.SearchTextIsChange += (str) =>
            {
                s_searchString = str;
                RefreshFilter();
            };
            firstLineArea.Content.Add(mySearchArea);
            
            s_sizeFilter = new Filter<int>();
            s_sizeFilter.Label = "尺寸";
            s_sizeFilter.toggleTepyList = drawTextures[s_selectedGroup].getTextureList.textureSizeList;
            s_sizeFilter.IsToggleChange += RefreshFilter;
            
            s_wrapModeFilter = new Filter<TextureWrapMode>("循环模式", drawTextures[s_selectedGroup].getTextureList.textureWrapMode);
            s_wrapModeFilter.IsToggleChange += RefreshFilter;
            s_wrapModeFilter.IsToggleChange += RefreshFilter;

            firstLineArea.Content.Add(new FlexibleArea());
            firstLineArea.Content.Add(s_sizeFilter);
            firstLineArea.Content.Add(s_wrapModeFilter);
            
            s_myToolbarArea = new ToolbarArea(s_windowData.names, s_windowData.paths, s_selectedGroup);
            s_myToolbarArea.IsListChange += SetDirtyInData;
            s_myToolbarArea.IsListAdd += AddNewDirector;
            s_myToolbarArea.IsListRemoveIndex += RemoveDataInIndex;
            s_myToolbarArea.IsSelectChange += RefreshFilter;
            s_myToolbarArea.IsSelectChange += SetToggleList;
            
            textureArea.Content.Add(s_myToolbarArea);
            textureArea.Content.Add(s_splitView);
            mainArea.Content.Add(firstLineArea);
            mainArea.Content.Add(textureArea);
        }
        
        private void OnGUI()
        {
            // 图片大小滑动条
            s_windowData.textureSize = (int)GUI.HorizontalSlider(TextureSizeRect, s_windowData.textureSize, 50f, 200f);

            // 第一行
            firstLineArea.OnGUI(new Rect(0, 0, position.width, 20));
            // 贴图区域 包含 Toolbar
            textureArea.OnGUI(new Rect(0, 20, position.width, position.height - 20));
            
            Repaint();
        }

        private void ShowButton(Rect position)
        {
            position.width += 5;
            position.x -= 5;
            
            if (GUI.Button(position, EditorGUIUtility.IconContent("_Popup"), "RL FooterButton"))
            {
                SettingsWindow.Open(new Rect(this.position.xMax - 300, this.position.y + 20, 300, 200));
            }
        }

        private static void SetToggleList()
        {
            s_sizeFilter.toggleTepyList = drawTextures[s_myToolbarArea.selectedIndex].getTextureList.textureSizeList;
            s_wrapModeFilter.toggleTepyList = drawTextures[s_myToolbarArea.selectedIndex].getTextureList.textureWrapMode;
        }

        private void SetDirtyInData()
        {
            EditorUtility.SetDirty(s_windowData);
        }

        private void DrawDragAndDrropRect(Rect obj)
        {
            using (new GUILayout.AreaScope(obj))
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("", "RL DragHandle", GUILayout.Width(100));
                    GUILayout.FlexibleSpace();
                }
            }
        }

        // 1区
        private void FirstArea(Rect areaRect)
        {
            using (new GUILayout.AreaScope(areaRect, "", "FrameBox"))
            {
                if (drawTextures.Count != 0 && !drawTextures[s_myToolbarArea.selectedIndex].isLoaded)
                {
                    drawTextures[s_myToolbarArea.selectedIndex].Load();
                }
                else if (drawTextures.Count != 0)
                {
                    drawTextures[s_myToolbarArea.selectedIndex].Draw(new Rect(0, 0, areaRect.width, areaRect.height));
                }
            }
        }
        
        // 2区
        private void SecondArea(Rect areaRect)
        {
            // 通道合图菜单
            float textRectSize = areaRect.height - 20;
            textRectSize = Mathf.Clamp(textRectSize, 50, 200);
            
            var textureButtonRect = new Rect(areaRect.x + 3, areaRect.y, textRectSize, textRectSize);
            var textureToolsRect = new Rect(textureButtonRect.xMax + 3, areaRect.y, 26, areaRect.height - 20);
            var toolsMainRect = new Rect(textureToolsRect.xMax - 2, areaRect.y, areaRect.width - textureToolsRect.xMax - 50, areaRect.height - 20);

            if (GUI.Button(textureButtonRect, materialTexture, skin.customStyles[1]))
            {
                EditorGUIUtility.PingObject(materialTexture);
            }
            
            /*
            // 纹理处理工具
            int tempValue = -1;
            using (new GUILayout.AreaScope(textureToolsRect))
            {
                for (int i = 0; i < s_textureToolsList.Count; i++)
                {
                    s_textureToolsList[i].Rect = toolsMainRect;
                    s_textureToolsList[i].ShowToggle();
                    if (s_textureToolsList[i].ToggleValue)
                    {
                        if (i != tempValue && tempValue != -1)
                        {
                            s_textureToolsList[tempValue].ToggleValue = false;
                        }
                        tempValue = i;
                    }
                }
            }

            if (tempValue != -1)
            {
                using (new GUILayout.AreaScope(toolsMainRect, "", "LODBlackBox"))
                {
                    s_textureToolsList[tempValue].OnGUI(toolsMainRect);
                }
            }
            */

            using (new GUILayout.AreaScope(InfoRect))
            {
                GUILayout.FlexibleSpace();
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(texturePathInfo, "PR DisabledLabel");
                    
                    if (!string.IsNullOrEmpty(texturePathInfo))
                    {
                        if (GUILayout.Button("打开", "AssetLabel Partial"))
                        {
                            AssetDatabase.OpenAsset(materialTexture);
                        }
                    }

                    GUILayout.FlexibleSpace(); // 把后面的 GUI 都往最右边顶

                    if (drawTextures != null && drawTextures[s_selectedGroup].nowTextureBoxs != null)
                    {
                        GUILayout.Label(drawTextures[s_selectedGroup].nowTextureBoxs.Count.ToString(), "MeTimeLabel"); // 显示图片数量
                        GUILayout.Space(5);
                    }
                }
            }
        }

        private void SetTextureInMaterial(TextureBox obj)
        {
            texturePathInfo = obj.texturePath;
            
            // 设置材质球贴图
            if (isChangeMaterialTexture)
            {
                SetTextureInMaterial(obj.t2d);
            }

            // 贴图单张预览
            materialTexture = obj.t2d;
        }

        public static void SetTextureInMaterial(Texture2D texture2D)
        {
            Undo.RecordObject(s_windowData.nowMaterial, "修改材质贴图"); // 注册可撤销
            s_windowData.nowMaterial.SetTexture(s_windowData.nowTextruePropertyName, texture2D);
        }

        private static void AddNewDirector()
        {
            // 只 new 新加的
            int lastIndex = s_windowData.paths.Count - 1;
            string path = s_windowData.paths[lastIndex];
            var dtg = new DrawTextureGroup(path);
            drawTextures.Add(dtg);
        }

        private static void LoadData()
        {
            string dataFilePath = Data.GetDataPath() + "/" + Data.c_dataFileName;
            if (!File.Exists(dataFilePath))
            {
                Data.CreateDefaultData(s_targetMaterial);
            }

            if (s_windowData == null)
            {
                s_windowData = AssetDatabase.LoadAssetAtPath<SelectTextureWindowData>(dataFilePath);
            }

            if (s_splitView == null)
            {
                s_splitView = new SplitView(ESplitType.Vertical, EAutoFillRect.SecondRect);
            }

            if (s_targetMaterial != null)
            {
                s_windowData.nowMaterial = s_targetMaterial;
            }

            if (s_targetPropertyName != null)
            {
                s_windowData.nowTextruePropertyName = s_targetPropertyName;
            }

            SplitView.SplitSize = s_windowData.splitSize;

            materialTexture = s_windowData.nowMaterial.GetTexture(s_windowData.nowTextruePropertyName);
            if (materialTexture == null)
            {
                materialTexture = Texture2D.whiteTexture;
            }
            if (materialTexture != null)
            {
                texturePathInfo = AssetDatabase.GetAssetPath(materialTexture);
            }

            // 加载插件
            var types = TypeCache.GetTypesDerivedFrom<TextureTools>();
            if (types.Count != s_textureToolsList.Count)
            {
                s_textureToolsList.Clear();
                for (int i = 0; i < types.Count; i++)
                {
                    var itemType = types[i];
                    var tt = Activator.CreateInstance(itemType) as TextureTools;
                    s_textureToolsList.Add(tt);
                }
            }
        }

        static void AddMaterialData(Material material)
        {
            // 数量限制
            if (s_windowData.materials.Count >= 5)
            {
                s_windowData.materials.RemoveAt(0);
            }
            
            if (s_windowData.materials.Contains(material))
            {
                // 移除之前一样的
                int index = s_windowData.materials.IndexOf(material);
                s_windowData.materials.RemoveAt(index);
                
                // 再加到最后面
                s_windowData.materials.Add(material);
            }
            else
            {
                s_windowData.materials.Add(material);
            }
        }

        public static void SaveDataInAsset(string path, string name = "null")
        {
            if (name == "null")
            {
                name = path.Substring(path.LastIndexOf('/') + 1);
            }

            EditorUtility.SetDirty(s_windowData);

            s_windowData.names.Add(name);
            s_windowData.paths.Add(path);
            
            drawTextures.Add(new DrawTextureGroup(path));
        }

        public static void RemoveDataInIndex(int index)
        {
            // 限制最少有一个
            if (s_windowData.names.Count == 1)
            {
                return;
            }

            s_windowData.paths.RemoveAt(index);
            s_windowData.names.RemoveAt(index);
            
            EditorUtility.SetDirty(s_windowData);
            
            drawTextures.RemoveAt(index);
            
            // 不是删除第1个，选中就-1
            if (s_myToolbarArea.selectedIndex != 0)
            {
                s_myToolbarArea.selectedIndex--;
            }
        }

        public static void SaveData()
        {
            EditorUtility.SetDirty(s_windowData);
            AssetDatabase.SaveAssets();
        }

        private static void AddSearchList(string searchStr)
        {
            // 数量限制
            if (s_windowData.seachString.Count >= 5)
            {
                s_windowData.seachString.RemoveAt(0);
            }

            if (s_windowData.seachString.Contains(searchStr))
            {
                // 移除之前一样的
                int index = s_windowData.seachString.IndexOf(searchStr);
                s_windowData.seachString.RemoveAt(index);
                
                // 再加到最后面
                s_windowData.seachString.Add(searchStr);
            }
            else
            {
                s_windowData.seachString.Add(searchStr);
            }

            EditorUtility.SetDirty(s_windowData);
        }

        private void OnDisable()
        {
            DrawTextureGroup.IsTextureChange -= SetTextureInMaterial;
            
            s_splitView.FirstArea -= FirstArea;
            s_splitView.SecondArea -= SecondArea;
            s_splitView.DrawDragAndDropRect -= DrawDragAndDrropRect;
            s_sizeFilter.IsToggleChange -= RefreshFilter;
            s_wrapModeFilter.IsToggleChange -= RefreshFilter;
            s_wrapModeFilter.IsToggleChange -= RefreshFilter;
            
            s_myToolbarArea.IsListAdd -= AddNewDirector;
            s_myToolbarArea.IsListChange -= SetDirtyInData;
            s_myToolbarArea.IsListRemoveIndex -= RemoveDataInIndex;
            s_myToolbarArea.IsSelectChange -= RefreshFilter;
            
            s_windowData.splitSize = SplitView.SplitSize;
            EditorUtility.SetDirty(s_windowData);
        }

        public static void RefreshFilter()
        {
            drawTextures[s_myToolbarArea.selectedIndex].nowTextureBoxs = drawTextures[s_myToolbarArea.selectedIndex].getTextureList.textureBoxs;
            
            TextureSizeFilter(drawTextures[s_myToolbarArea.selectedIndex]);
            TextureWrapModesFilter(drawTextures[s_myToolbarArea.selectedIndex]);
            TextureSearchFilter(drawTextures[s_myToolbarArea.selectedIndex]);
        }

        public static void RefreshFilter(ref List<TextureBox> textureBoxs)
        {
            TextureSizeFilter(ref textureBoxs);
        }

        public static void TextureSizeFilter(ref List<TextureBox> textureBoxs)
        {
            textureBoxs.Where(boxs =>
            {
                int size = boxs.t2d.height > boxs.t2d.width
                    ? boxs.t2d.height
                    : boxs.t2d.width;
                bool result = SizeFilterPopupWindow<int>.PropertySelect[size];
                return result;
            }).ToList();
        }

        public static void RefreshFilter(DrawTextureGroup drawTextureGroup)
        {
            drawTextureGroup.nowTextureBoxs = drawTextureGroup.getTextureList.textureBoxs;
            TextureSizeFilter(drawTextureGroup);
            TextureWrapModesFilter(drawTextureGroup);
            TextureSearchFilter(drawTextureGroup);
        }

        public static void TextureSizeFilter(DrawTextureGroup drawTextureGroup)
        {
            drawTextureGroup.nowTextureBoxs = SizeFilterPopupWindow<int>.IsAllIsFalse
                ? drawTextureGroup.nowTextureBoxs
                : drawTextureGroup.nowTextureBoxs.Where(boxs =>
                {
                    bool result = SizeFilterPopupWindow<int>.PropertySelect[boxs.MaxSize];
                    return result;
                }).ToList();
        }

        public static void TextureWrapModesFilter(DrawTextureGroup drawTextureGroup)
        {
            drawTextureGroup.nowTextureBoxs = (SizeFilterPopupWindow<TextureWrapMode>.IsAllIsFalse
                ? drawTextureGroup.nowTextureBoxs
                : drawTextureGroup.nowTextureBoxs.Where(boxs =>
                {
                    bool result = SizeFilterPopupWindow<TextureWrapMode>.PropertySelect[boxs.t2d.wrapMode];
                    return result;
                })).ToList();
        }

        public static void TextureSearchFilter(DrawTextureGroup drawTextureGroup)
        {
            drawTextureGroup.nowTextureBoxs = string.IsNullOrEmpty(s_searchString)
                ? drawTextureGroup.nowTextureBoxs
                : drawTextureGroup.nowTextureBoxs.Where(box =>
                {
                    // 跳过不能成功被加载到的纹理
                    if (box.t2d == null)
                    {
                        //Debug.LogError($"没有加载成功的纹理: {box.texturePath}");
                        return false;
                    }

                    string leftStr = box.t2d.name.ToLower();
                    string rightStr = s_searchString.ToLower();
                    
                    return leftStr.Contains(rightStr);
                }).ToList();
        }

        public static void SaveTextureInAssets(Texture2D texture2D, string path)
        {
            var texture = texture2D.EncodeToPNG();
            File.WriteAllBytes(path, texture);
            AssetDatabase.Refresh();
        }
    }
}