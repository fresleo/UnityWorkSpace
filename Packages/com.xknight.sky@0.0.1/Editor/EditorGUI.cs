using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    public class SwatchesDrawParams
    {
        public bool swatches_Folder = false;
        public object dicKeyObject = null;
    }

    public class CommonEditorGUI
    {
        //public static readonly GUIStyle boxStyle = "LODSliderRange";
        public static void BeginBoxContent()
        {
            GUILayout.BeginHorizontal();
            //EditorGUILayout.BeginHorizontal(boxStyle, GUILayout.MinHeight(10f));
            GUILayout.BeginVertical();
            GUILayout.Space(2f);
        }

        public static void EndBoxContent()
        {
            GUILayout.Space(3f);
            GUILayout.EndVertical();
            //EditorGUILayout.EndHorizontal();
            GUILayout.Space(3f);
            GUILayout.EndHorizontal();
            GUILayout.Space(3f);
        }

        
        public static void DrawScriptableDictionarySwatches<T,U>( string swatchesname,SwatchesDrawParams drawParams,ScriptableDictionary<T,U> target,Action<T> addSwatchesCall,Action<bool,T,U> DicOption)
        {
            if(target == null)
            {
                Debug.LogError("Draw Folder TargetData Is Null!");
                return;
            }

            drawParams.swatches_Folder = CommonEditorGUI.DrawSubFolder(new GUIContent(swatchesname), drawParams.swatches_Folder);
            CoreEditorUtils.DrawSplitter(false);
            if(drawParams.swatches_Folder)
            {

                CommonEditorGUI.BeginBoxContent();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Key:");
                GUILayout.FlexibleSpace();
               
                //需要更具type 区分不同的类型
                switch (target.GetTypeKey())
                {
                    case DicKeyType.String:
                        if (drawParams.dicKeyObject == null)
                        {
                            drawParams.dicKeyObject = string.Empty;
                        }
                        drawParams.dicKeyObject =  EditorGUILayout.TextField(drawParams.dicKeyObject.ToString());
                        break;
                    case DicKeyType.Int:
                        if (drawParams.dicKeyObject == null)
                        {
                            drawParams.dicKeyObject = 0;
                        }
                        drawParams.dicKeyObject = EditorGUILayout.IntField((int)drawParams.dicKeyObject);
                        break;
                }
                
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Save Swatches"))
                {
                    string key = drawParams.dicKeyObject.ToString();
                    if (string.IsNullOrEmpty(key))
                    {
                        EditorUtility.DisplayDialog("Tip", "需要Key值!", "OK", "Cancle");
                    }
                    else
                    {
                        /*
                            要对泛型支持 
                            1、需要实现不同的key值类型的判定 绘制
                            2、要给每个类型的T 实现实例的Key映射，做逻辑判断.eg:target 必须维护一个T对应的key值，最简单的是用string类型维护
                                否则无法更具具体的T类型 先进性存储后 ，在转回T做判断
                        */
                        if (target.ContainsKey((T)drawParams.dicKeyObject))
                        {
                            EditorUtility.DisplayDialog("Tip", "Key值重复!", "OK", "Cancle");
                        }
                        else
                        {
                            addSwatchesCall?.Invoke((T)drawParams.dicKeyObject);
                            drawParams.dicKeyObject = null ;
                        }
                    }
                }
                GUILayout.EndHorizontal();

                ScriptableDictionaryEditor.Drawer<T, U>(target, DicOption);

                CommonEditorGUI.EndBoxContent();

            }
            CommonEditorGUI.EndSubFolder();
        }

        public static Color MakeColorDraker(Color originColor)
        {
            const float mutiplier = 0.8f;
            var modif = originColor * mutiplier;
            modif.a *= mutiplier;
            return modif;
        }

        public static bool DrawSubFolder(GUIContent title, bool folderValue)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(12);
            bool value = CoreEditorUtils.DrawHeaderFoldout(title, folderValue);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(26);
            EditorGUILayout.BeginVertical();
            return value;
        }

        public static void EndSubFolder()
        {
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }
    }

    public class EditorGUILabelWidthScope : IDisposable
    {
        private float m_OriginalLabel;
        public EditorGUILabelWidthScope(float target)
        {
            m_OriginalLabel = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = target;
        }
        public void Dispose()
        {
            EditorGUIUtility.labelWidth = m_OriginalLabel;
        }
    }
}