using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MaterialInspectorExtensionTool.Editor.SelectTextureTool
{
    /// <summary>
    /// 单个贴图数据
    /// </summary>
    public class TextureBox
    {
        public Texture2D t2d;
        public DateTime timeInfo;
        public string texturePath;
        public bool isSelect;

        public int MaxSize
        {
            get
            {
                if (t2d != null)
                {
                    return Mathf.Max(t2d.width, t2d.height);
                }

                return 0;
            }
        }

        public TextureBox(Texture2D texture2D, DateTime dateTime, String texturePath, bool isSelect = false)
        {
            this.t2d = texture2D;
            this.timeInfo = dateTime;
            this.texturePath = texturePath;
            this.isSelect = isSelect;
        }
    }

    public class GetTextureList
    {
        public List<TextureWrapMode> textureWrapMode = new(); // 模式表不添加重复模式
        public List<int> textureSizeList = new();
        public List<TextureBox> textureBoxs = new();

        private int m_textureArrayLength;
        public int TextureArrayLength => m_textureArrayLength;

        private int m_lodIndex;


        public IEnumerator GetAssetTextureInPath(string path)
        {
            m_lodIndex = 0;

            string[] guidOrPath;
            bool isAssets = path.StartsWith("Assets"); // 判断是否是工程内的路径
            if (isAssets)
            {
                guidOrPath = AssetDatabase.FindAssets("t:Texture", new[] { path });
            }
            else
            {
                guidOrPath = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).Where(s => 
                    s.EndsWith(".png") || s.EndsWith(".jpg") || s.EndsWith(".tga")).ToArray();
            }

            m_textureArrayLength = guidOrPath.Length; // 确定长度

            string tempPath;
            Texture2D texture;
            for (int i = 0; i < guidOrPath.Length; i++)
            {
                if (isAssets)
                {
                    tempPath = AssetDatabase.GUIDToAssetPath(guidOrPath[i]);
                    texture = AssetDatabase.LoadAssetAtPath<Texture2D>(tempPath);
                }
                else
                {
                    tempPath = guidOrPath[i];
                    using (FileStream fileStream = new FileStream(tempPath, FileMode.Open, FileAccess.Read))
                    {
                        var length = fileStream.Length;
                        m_lodIndex = (int)length;

                        byte[] bytes = new byte[length];
                        fileStream.Read(bytes, 0, (int)length);

                        texture = new Texture2D(512, 512);
                        texture.LoadImage(bytes);
                    }
                }

                var tempTextureBox = new TextureBox(texture, new FileInfo(tempPath).LastWriteTime, tempPath);
                textureBoxs.Add(tempTextureBox);

                if (texture != null && !textureWrapMode.Contains(texture.wrapMode))
                {
                    textureWrapMode.Add(texture.wrapMode); // 收集贴图组的模式表
                }

                if (!textureSizeList.Contains(tempTextureBox.MaxSize)) // 收集的贴图组的 Size 表
                {
                    bool result = false;

                    for (int j = 0; j < textureSizeList.Count; j++) // 排序小到大
                    {
                        if (tempTextureBox.MaxSize < textureSizeList[j])
                        {
                            textureSizeList.Insert(j, tempTextureBox.MaxSize);
                            result = true;
                            break;
                        }
                    }

                    if (!result)
                    {
                        textureSizeList.Add(tempTextureBox.MaxSize);
                    }
                }

                if ((i % 100 == 0) || m_lodIndex >= 800000 && i != 0) // 加载100个每帧
                {
                    SelectTextureWindow.RefreshFilter(); // 刷新筛选数据，跑一下筛选 
                    yield return null;
                }
            }

            SelectTextureWindow.RefreshFilter();
        }
    }
}