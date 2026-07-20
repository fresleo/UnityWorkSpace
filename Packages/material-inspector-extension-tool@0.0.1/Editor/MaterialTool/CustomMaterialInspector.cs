using System.IO;
using UnityEditor;
using UnityEngine;

namespace MaterialInspectorExtensionTool.Editor.MaterialTool
{
    /// <summary>
    /// 自定义材质球的检视面板
    /// </summary>
    [CustomEditor(typeof(Material))]
    public class CustomMaterialInspector : MaterialEditor
    {
        private static Material s_tempMaterial;
        private GUIContent m_errorContent;
        private bool m_isGood;
        
        private bool m_isGameobject;
        private Material m_targetMaterial;
        
        // go 的渲染资源
        private Renderer m_goRenderer;
        private Material[] m_goSharedMaterials;
        
        private static int s_index;
        
        private const string c_pref_CopyMaterialKey1 = "CopyMaterialKey1";
        private static string s_lastPath;
        
        public override void OnEnable()
        {
            base.OnEnable();
            
            if (s_tempMaterial == null)
            {
                s_tempMaterial = Resources.Load<Material>("TempCopyMaterialValue");
            }
            
            m_isGood = s_tempMaterial != null;
            if (!m_isGood)
            {
                m_errorContent = new GUIContent(EditorGUIUtility.IconContent("CollabConflict"));
                m_errorContent.tooltip = "找不到原始材质,请在脚本所在文件夹中的 Resources 文件夹创建一个名字叫 TempCopyMaterialValue 的材质球";
            }

            // 判断是不是1个游戏对象
            m_isGameobject = Selection.activeTransform;
            if (!m_isGameobject)
            {
                return;
            }
            
            m_targetMaterial = base.target as Material;

            if (m_targetMaterial != null && m_targetMaterial.hideFlags != HideFlags.NotEditable)
            {
                string assetPath = AssetDatabase.GetAssetPath(m_targetMaterial);
                
                PaseFavoritePathExtension.s_stringName[0] = "*此材质球路径";
                PaseFavoritePathExtension.s_stringPath[0] = assetPath;
            }
            else
            {
                // 当前的不能编辑，就选择上次操作的路径
                s_lastPath = EditorPrefs.GetString(c_pref_CopyMaterialKey1);
                if (!string.IsNullOrEmpty(s_lastPath))
                {
                    string fileName = Path.GetFileName(Path.GetDirectoryName(s_lastPath));
                    PaseFavoritePathExtension.s_stringName[0] = $"*上次的路径: {fileName}";
                    PaseFavoritePathExtension.s_stringPath[0] = s_lastPath;
                }
            }

            // 当选择的是有材质球的 go 时，才能够走到这里
            if (Selection.activeTransform.TryGetComponent(out m_goRenderer))
            {
                m_goSharedMaterials = m_goRenderer.sharedMaterials;
            }
        }


        public override void OnInspectorGUI()
        {
            using (new GUILayout.VerticalScope("box"))
            {
                EditorGUILayout.Space(2);
                
                // 提示错误
                if (!m_isGood)
                {
                    GUILayout.Label(m_errorContent, GUILayout.Width(20));
                }
                GUI.enabled = m_isGood;
                
                var tempMaterial = base.target as Material;
                if (tempMaterial != null)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("复制 Shader引用+材质属性"))
                        {
                            s_tempMaterial.shader = tempMaterial.shader;
                            s_tempMaterial.CopyPropertiesFromMaterial(tempMaterial);
                        }

                        if (GUILayout.Button("粘贴 Shader引用+材质属性"))
                        {
                            Undo.RecordObject(tempMaterial, $"回退[粘贴 Shader引用+材质属性]操作: {tempMaterial.name}");
                            
                            tempMaterial.shader = s_tempMaterial.shader;
                            tempMaterial.CopyPropertiesFromMaterial(s_tempMaterial);
                        }
                    }
                    
                    if (MaterialChecker.CheckMaterial(tempMaterial, false, false))
                    {
                        EditorGUILayout.Space(5);
                        
                        EditorGUILayout.HelpBox("可能会因为一些插件的问题，导致这里检查不准，如果反复无法清除，不用死磕", MessageType.Warning);
                        using (new GUILayout.HorizontalScope())
                        {
                            var lastColor = GUI.color;
                            GUI.color = Color.red;
                            if (GUILayout.Button("材质上有无效数据，点我清理"))
                            {
                                MaterialChecker.CheckMaterial(tempMaterial, true, true);
                            }
                            GUI.color = lastColor;

                            var gs = new GUIStyle("CN EntryErrorIconSmall");
                            EditorGUILayout.LabelField("", gs, GUILayout.Width(50));
                        }
                    }
                }
                
                EditorGUILayout.Space(2);
                GUI.enabled = true;
            }
            
            // 是1个go
            if (m_isGameobject && m_goRenderer != null)
            {
                m_goSharedMaterials = m_goRenderer.sharedMaterials; // 将材质球 赋值给新的数组

                for (int i = 0; i < m_goSharedMaterials.Length; i++)
                {
                    if (m_goSharedMaterials[i] == null)
                    {
                        continue;
                    }

                    // 材质球只显示自己的框
                    if (m_targetMaterial.GetInstanceID() != m_goRenderer.sharedMaterials[i].GetInstanceID())
                    {
                        continue;
                    }
                    
                    using (new GUILayout.HorizontalScope())
                    {
                        Undo.RecordObject(m_goRenderer, "CopyAndUseMaterial"); // 设置可撤销
                        
                        // 材质选取框 可被赋值
                        m_goSharedMaterials[i] = (Material)EditorGUILayout.ObjectField(m_goRenderer.sharedMaterials[i], typeof(Material), true, GUILayout.MaxWidth(100));
                        // 下拉选框
                        s_index = EditorGUILayout.Popup(s_index, PaseFavoritePathExtension.s_stringName.ToArray(), GUILayout.MaxWidth(150));
                        
                        var lastColor = GUI.color;
                        GUI.color = Color.green;
                        {
                            if (m_targetMaterial.hideFlags != HideFlags.NotEditable)
                            {
                                if (GUILayout.Button("复制并引用")) // 复制材质并引用
                                {
                                    string copyPath = PaseFavoritePathExtension.s_stringPath[s_index];
                                    EditorPrefs.SetString(c_pref_CopyMaterialKey1, copyPath);
                                    m_goSharedMaterials[i] = CopyAsset(m_goRenderer.sharedMaterials[i], copyPath) as Material;
                                }
                            }
                            else
                            {
                                if (GUILayout.Button("复制临时材质")) // 不可编辑的默认材质球 复制临时 材质并引用
                                {
                                    string copyPath = PaseFavoritePathExtension.s_stringPath[s_index];
                                    EditorPrefs.SetString(c_pref_CopyMaterialKey1, copyPath);
                                    m_goSharedMaterials[i] = CopyAsset(s_tempMaterial, copyPath) as Material;
                                }
                            }
                        }
                        GUI.color = lastColor;
                    }
                }

                m_goRenderer.sharedMaterials = m_goSharedMaterials;
            }

            GUILayout.Space(5); // 和原始的 GUI 之间，留一些空隙
            
            base.OnInspectorGUI();
        }
        
        /// <summary>
        /// 将Asset资产复制保存到指定路径
        /// </summary>
        /// <param name="obj">资源</param>
        /// <param name="copyPath">复制到的路径</param>
        /// <returns>新复制出来的资源</returns>
        public static Object CopyAsset(Object obj, string copyPath)
        {
            // 没有后缀名的话，说明它是一个文件
            if (!Path.HasExtension(copyPath))
            {
                // 永远是资源的名字作为复制资源的名字
                string assetPath = AssetDatabase.GetAssetPath(obj);
                string fileName = Path.GetFileName(assetPath);
                copyPath = $"{copyPath}/{fileName}";
            }

            //copyPath = PathExtension.GetOnlyPath(copyPath);
            copyPath = AssetDatabase.GenerateUniqueAssetPath(copyPath); //获取唯一路径

            Object cloneObj = Object.Instantiate(obj);
            AssetDatabase.CreateAsset(cloneObj, copyPath);

            Object loadedObj = AssetDatabase.LoadAssetAtPath<Object>(copyPath);
            return loadedObj;
        }
        
    }
}