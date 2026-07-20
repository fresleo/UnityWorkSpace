using UnityEditor;
using UnityEngine;

namespace MaterialInspectorExtensionTool.Editor.SelectTextureTool
{
    public class TextureTestTool : TextureTools
    {
        private GUIContent m_toggleContent = new GUIContent()
        {
            image = Resources.Load<Texture2D>("TextureTool_TextureRGBA"), tooltip = "RGBA"
        };

        protected override GUIContent ToggleContent
        {
            get => m_toggleContent;
            set => m_toggleContent = value;
        }

        protected override void OnDispose()
        {
            DrawTextureGroup.IsTextureChange -= SetTextureBox;
            SelectTextureWindow.isChangeMaterialTexture = true;
        }

        private bool[] m_toggleValues = new bool[5] { false, false, false, false, false };
        private Rect[] m_textureRects = new Rect[5];
        private Texture2D[] m_textures = new Texture2D[5];
        private string[] m_RGBAButtonText = new string[7] { "R", "G", "B", "A", "灰", "黑", "白" };
        private string[] m_tips = new string[4] { "+", "+", "+", "=" };

        private GUIContent[] m_buttonContents = new GUIContent[5]
        {
            new GUIContent() { text = "R" },
            new GUIContent() { text = "G" },
            new GUIContent() { text = "B" },
            new GUIContent() { text = "A" },
            new GUIContent() { text = "Result" },
        };

        private int m_tempIndex = -1;
        private int[] m_tempint = new int[] { 0, 0, 0, 0 };
        
        private Texture2D m_RTexture;
        private Texture2D m_GTexture;
        private Texture2D m_BTexture;
        private Texture2D m_ATexture;
        private Texture2D m_ResultTexture;

        private GUIStyle m_gs, m_textGs;
        private int m_sizeInt = 1;

        // 绘制用的着色器
        private static Shader DrawShader => Shader.Find("TextureToolRGBAShader");
        // 绘制用的材质球
        private Material[] m_drawMaterials = new Material[5]
        {
            new Material(DrawShader),
            new Material(DrawShader),
            new Material(DrawShader),
            new Material(DrawShader),
            new Material(DrawShader)
        };

        public override void OnGUI(Rect position)
        {
            float textureSize = Mathf.Clamp(position.height, 40, 200) - 25;
            var textureWidth = GUILayout.Width(textureSize);
            var textureHeight = GUILayout.Height(textureSize);

            using (new GUILayout.HorizontalScope())
            {
                for (int i = 0; i < m_textures.Length; i++)
                {
                    using (new GUILayout.VerticalScope())
                    {
                        m_toggleValues[i] = GUILayout.Toggle(m_toggleValues[i], m_buttonContents[i], m_gs, textureWidth, textureHeight);
                        if (m_toggleValues[i] && m_tempIndex != i)
                        {
                            if (m_tempIndex != -1)
                            {
                                m_toggleValues[m_tempIndex] = false;
                            }

                            m_tempIndex = i;
                        }

                        if (Event.current.type == EventType.Repaint)
                        {
                            m_textureRects[i] = GUILayoutUtility.GetLastRect();
                        }

                        if (m_textures[i] != null)
                        {
                            float tempM = 0f;
                            if (m_textures[i].width > m_textures[i].height)
                            {
                                tempM = (float)m_textures[i].height / m_textures[i].width;
                                m_textureRects[i].y += (1 - tempM) / 2 * m_textureRects[i].height;

                                m_textureRects[i].height *= tempM;
                            }
                            else
                            {
                                tempM = (float)m_textures[i].width / m_textures[i].height;
                                m_textureRects[i].x += (1 - tempM) / 2 * m_textureRects[i].width;
                                m_textureRects[i].width *= tempM;
                            }

                            UnityEngine.Graphics.DrawTexture(m_textureRects[i], m_textures[i], new Rect(0, 0, 1, 1), 0, 0, 0, 0, GUI.color, m_drawMaterials[i]);
                        }

                        // 选择框
                        if (m_toggleValues[i])
                        {
                            DrawTextureGroup.DrawLine(m_textureRects[i], 4, UnityEngine.Color.yellow);
                        }
                        
                        if (i < 4)
                        {
                            EditorGUI.BeginChangeCheck();
                            m_tempint[i] = GUILayout.Toolbar(m_tempint[i], m_RGBAButtonText, textureWidth);

                            m_drawMaterials[i].SetFloat("_CustomValue", m_tempint[i]);
                            if (EditorGUI.EndChangeCheck())
                            {
                                ResetResultTexture(i);
                            }
                        }
                    }

                    if (i < 4)
                    {
                        GUILayout.Label(m_tips[i], m_textGs, textureHeight);
                    }
                }

                if (GUILayout.Button("使用"))
                {
                    SelectTextureWindow.SetTextureInMaterial(m_ResultTexture);
                }

                GUILayout.Label("输出尺寸:");
                m_sizeInt = EditorGUILayout.Popup(m_sizeInt, new[] { "128", "256", "512", "1024" });
                
                GUILayout.Label("输出格式:");
                EditorGUILayout.Popup(0, new[] { "png", "jpg", "tga" });
                
                if (GUILayout.Button("保存"))
                {
                    int size = 0;
                    switch (m_sizeInt)
                    {
                        case 0:
                            size = 128;
                            break;
                        case 1:
                            size = 256;
                            break;
                        case 2:
                            size = 512;
                            break;
                        case 3:
                            size = 1024;
                            break;
                    }

                    string path = EditorUtility.SaveFilePanel("选择保存路径", "", "CustomRGBATexture", "png");
                    if (!string.IsNullOrEmpty(path))
                    {
                        m_ResultTexture = MergeTextureRChannel(
                            m_RTexture != null ? m_RTexture : Texture2D.blackTexture,
                            m_GTexture != null ? m_GTexture : Texture2D.blackTexture,
                            m_BTexture != null ? m_BTexture : Texture2D.blackTexture,
                            m_ATexture != null ? m_ATexture : Texture2D.blackTexture,
                            size, size);
                        SelectTextureWindow.SaveTextureInAssets(m_ResultTexture, path);
                    }
                }
            }
        }

        // 用材质处理纹理
        private Texture BlitTexture(Texture texture, Material material)
        {
            RenderTexture tmpRT = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            UnityEngine.Graphics.Blit(texture, tmpRT, material);

            RenderTexture previous = RenderTexture.active;

            Texture2D myTexture2D = new Texture2D(texture.width, texture.height);
            myTexture2D.ReadPixels(new Rect(0, 0, tmpRT.width, tmpRT.height), 0, 0);
            myTexture2D.Apply();

            RenderTexture.active = previous;

            RenderTexture.ReleaseTemporary(tmpRT);
            return myTexture2D;
        }

        protected override void OnEnable()
        {
            // 贴图切换不改变材质球贴图
            SelectTextureWindow.isChangeMaterialTexture = false;
            DrawTextureGroup.IsTextureChange += SetTextureBox;
            
            m_gs = new GUIStyle("AppToolbarButtonMid")
            {
                fontSize = 70, 
                fontStyle = UnityEngine.FontStyle.Bold,
            };
            m_gs.normal.textColor = new UnityEngine.Color(0.5f, 0.5f, 0.5f, 0.5f);
            
            m_textGs = new GUIStyle()
            {
                fontSize = 50, 
                fontStyle = UnityEngine.FontStyle.Bold, 
                alignment = TextAnchor.MiddleCenter, 
                margin = new RectOffset(0, 0, 0, 0), 
                padding = new RectOffset(0, 0, 0, 0)
            };
            m_textGs.normal.textColor = new UnityEngine.Color(0.5f, 0.5f, 0.5f, 0.5f);
        }

        /// <summary>
        /// 缩放纹理
        /// </summary>
        public static Texture2D ScaleTexture(Texture2D texture2D, int targetWidth, int targetHeight)
        {
            var result = new Texture2D(targetWidth, targetHeight, TextureFormat.ARGB32, false);

            for (int i = 0; i < targetHeight; i++)
            {
                for (int j = 0; j < targetWidth; j++)
                {
                    var color = texture2D.GetPixelBilinear((float)j / targetWidth, (float)i / targetHeight);
                    result.SetPixel(j, i, color);
                }
            }

            return result;
        }

        /// <summary>
        /// 合图纹理的 R 通道
        /// </summary>
        public static Texture2D MergeTextureRChannel(Texture2D rTexture, Texture2D gTexture, Texture2D bTexture, Texture2D aTexture, int targetWidth, int targetHeight)
        {
            var result = new Texture2D(targetWidth, targetHeight, TextureFormat.ARGB32, false);
            var resultColors = new UnityEngine.Color[targetWidth * targetHeight];
            
            for (int i = 0; i < targetHeight; i++)
            {
                for (int j = 0; j < targetWidth; j++)
                {
                    float u = (float)j / targetWidth;
                    float v = (float)i / targetHeight;
                    
                    var rColor = rTexture.GetPixelBilinear(u, v);
                    var gColor = gTexture.GetPixelBilinear(u, v);
                    var bColor = bTexture.GetPixelBilinear(u, v);
                    var aColor = aTexture.GetPixelBilinear(u, v);
                    
                    resultColors[i * targetWidth + j] = new UnityEngine.Color(rColor.r, gColor.r, bColor.r, aColor.r);
                }
            }

            result.SetPixels(resultColors);
            result.Apply(); // 不调用这个函数，set和get不会生效

            return result;
        }

        private void SetTextureBox(TextureBox obj)
        {
            if (m_tempIndex != -1 && m_toggleValues[m_tempIndex])
            {
                if (m_tempIndex != 4)
                {
                    m_textures[m_tempIndex] = obj.t2d;
                    m_drawMaterials[m_tempIndex].SetTexture("_MainTexture", obj.t2d);
                    
                    ResetResultTexture(m_tempIndex);
                }
                else
                {
                    //设置贴图
                    for (int i = 0; i < 4; i++)
                    {
                        m_textures[i] = obj.t2d;
                        m_drawMaterials[i].SetTexture("_MainTexture", obj.t2d);
                        m_tempint[i] = i;
                        m_drawMaterials[i].SetFloat("_CustomValue", i);
                    }

                    ResetResultTexture();
                }
            }
        }

        private void ResetResultTexture(int tempIndex)
        {
            switch (tempIndex)
            {
                case 0:
                {
                    if (m_textures[0] != null)
                    {
                        m_RTexture = BlitTexture(m_textures[0], m_drawMaterials[0]) as Texture2D;
                    }
                }
                    break;

                case 1:
                {
                    if (m_textures[1] != null)
                    {
                        m_GTexture = BlitTexture(m_textures[1], m_drawMaterials[1]) as Texture2D;
                    }
                }
                    break;

                case 2:
                {
                    if (m_textures[2] != null)
                    {
                        m_BTexture = BlitTexture(m_textures[2], m_drawMaterials[2]) as Texture2D;
                    }
                }
                    break;

                case 3:
                {
                    if (m_textures[3] != null)
                    {
                        m_ATexture = BlitTexture(m_textures[3], m_drawMaterials[3]) as Texture2D;
                    }
                }
                    break;
            }

            m_ResultTexture = MergeTextureRChannel(
                m_RTexture != null ? m_RTexture : Texture2D.blackTexture,
                m_GTexture != null ? m_GTexture : Texture2D.blackTexture,
                m_BTexture != null ? m_BTexture : Texture2D.blackTexture,
                m_ATexture != null ? m_ATexture : Texture2D.blackTexture,
                256, 256);

            m_buttonContents[4].text = string.Empty;
            m_buttonContents[4].image = m_ResultTexture;
        }

        private void ResetResultTexture()
        {
            if (m_textures[0] != null)
            {
                m_RTexture = BlitTexture(m_textures[0], m_drawMaterials[0]) as Texture2D;
            }

            if (m_textures[1] != null)
            {
                m_GTexture = BlitTexture(m_textures[1], m_drawMaterials[1]) as Texture2D;
            }

            if (m_textures[2] != null)
            {
                m_BTexture = BlitTexture(m_textures[2], m_drawMaterials[2]) as Texture2D;
            }

            if (m_textures[3] != null)
            {
                m_ATexture = BlitTexture(m_textures[3], m_drawMaterials[3]) as Texture2D;
            }
            
            m_ResultTexture = MergeTextureRChannel(
                m_RTexture != null ? m_RTexture : Texture2D.blackTexture,
                m_GTexture != null ? m_GTexture : Texture2D.blackTexture,
                m_BTexture != null ? m_BTexture : Texture2D.blackTexture,
                m_ATexture != null ? m_ATexture : Texture2D.blackTexture,
                256, 256);

            m_buttonContents[4].text = string.Empty;
            m_buttonContents[4].image = m_ResultTexture;
        }
        
    }
}