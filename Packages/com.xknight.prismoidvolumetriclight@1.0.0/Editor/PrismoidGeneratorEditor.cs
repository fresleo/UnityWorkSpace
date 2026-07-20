/// <summary>
/// 假聚焦灯光效果
/// 作者：Ling mei an
/// 修改日期：2025-9-12
/// 功能：假聚焦灯光效果。
/// </summary>
#if UNITY_EDITOR
using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using UnityEngine.Rendering.Universal;

namespace  knightTA.FakeSpotLightTool
{
//扩展ToggleTest类在Inspector面板的显示内容
    [CustomEditor(typeof(PrismoidGenerator))]
    [CanEditMultipleObjects]
    public class PrismoidGeneratorEditor : Editor
    {
        private SerializedObject prismoid; //序列化

        //重写OnInspectorGUI类(刷新Inspector面板)
        private SerializedProperty theStyle, sides, heightSegments, bottomRadius, topRadius, height; //定义类型，变量a，变量b
        private SerializedProperty generateBottom, generateTop, smoothNormals, savePath, changeMesh; //

        [MenuItem("GameObject/Light/PrismoidVolumetricLight")]
        private static void GameObject_right_btn1(MenuCommand command)
        {
            // 创建新GameObject
            GameObject newGO = new GameObject("PrismoidVolumetricLight");
            // 确保当前选中对象在与同级对象相比时名称唯一
            GameObjectUtility.EnsureUniqueNameForSibling(newGO);
            newGO.AddComponent<PrismoidGenerator>();
            // 设置父子关系（如果右键时有选中对象）
            if (command.context is GameObject selectedObject)
            {
                newGO.transform.SetParent(selectedObject.transform);
            }

            // 注册Undo操作
            Undo.RegisterCreatedObjectUndo(newGO, "Create " + newGO.name);

            // 选中新对象
            Selection.activeObject = newGO;
        }

        private void OnEnable()
        {
            prismoid = new SerializedObject(target);
            theStyle = prismoid.FindProperty("theStyle");
            sides = prismoid.FindProperty("sides");
            heightSegments = prismoid.FindProperty("heightSegments");
            bottomRadius = prismoid.FindProperty("bottomRadius");
            topRadius = prismoid.FindProperty("topRadius");
            height = prismoid.FindProperty("height");
            generateBottom = prismoid.FindProperty("generateBottom");
            generateTop = prismoid.FindProperty("generateTop");
            smoothNormals = prismoid.FindProperty("smoothNormals");
            savePath = prismoid.FindProperty("savePath");
            changeMesh = prismoid.FindProperty("changeMesh");
        }

        public override void OnInspectorGUI()
        {
            prismoid.Update();
            EditorGUILayout.PropertyField(theStyle);
            if (theStyle.enumValueIndex == 0)
            {
                EditorGUILayout.PropertyField(changeMesh);
                PrismoidGenerator tog = (PrismoidGenerator)target;
                tog.InitializeMesh();
            }
            else if (theStyle.enumValueIndex == 1)
            {
                EditorGUILayout.PropertyField(sides);
                EditorGUILayout.PropertyField(heightSegments);
                EditorGUILayout.PropertyField(bottomRadius);
                EditorGUILayout.PropertyField(topRadius);
                EditorGUILayout.PropertyField(height);
                EditorGUILayout.PropertyField(generateBottom);
                EditorGUILayout.PropertyField(generateTop);
                EditorGUILayout.PropertyField(smoothNormals);
                EditorGUILayout.PropertyField(savePath);
                prismoid.ApplyModifiedProperties();
                // //获取要执行方法的类
                PrismoidGenerator tog = (PrismoidGenerator)target;
                if (GUILayout.Button("生成多棱台"))
                {
                    //执行方法
                    tog.GeneratePrismoid();
                }
                
                if (GUILayout.Button("Bake"))
                {
                    //执行方法
                    tog.BakeGameObject(tog.mesh, tog.gameObject.name);
                }
            }

            
        }
    }


}
#endif