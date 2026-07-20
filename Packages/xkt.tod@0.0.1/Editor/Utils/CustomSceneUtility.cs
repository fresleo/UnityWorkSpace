// Created By: WangYu  Date: 2025-04-12

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace XKT.TOD.Utils
{
    public static class CustomSceneUtility
    {
        public static bool SaveModifiedScenesDialog()
        {
            bool hasModifiedScenes = false;
            
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty)
                {
                    hasModifiedScenes = true;
                    break;
                }
            }
            
            if (!hasModifiedScenes)
            {
                return true;
            }

            // 显示对话框
            int choice = EditorUtility.DisplayDialogComplex(
                "场景已修改", 
                "是否要保存对当前场景的修改?",
                "保存",        // 0
                "不保存",      // 1
                "取消"        // 2
            );

            switch (choice)
            {
                case 0: // 保存
                    return EditorSceneManager.SaveOpenScenes();
                case 1: // 不保存
                case 2: // 取消
                default:
                    return false;
            }
        }
        
    }
}