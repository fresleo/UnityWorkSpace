// Created By: WangYu  Date: 2025-07-12

#if UNITY_PIE_EDITOR

using System.Collections;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityPie;

namespace XKT.TOD
{
    /// <summary>
    /// TimeOfDayManager 的饼图菜单
    /// </summary>
    [InitializeOnLoad]
    public class TimeOfDayManagerPie
    {
        static TimeOfDayManagerPie()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    CreatePie();
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    RemovePie();
                    break;
            }
        }
        
        private static readonly ScriptableObject CoroutineOwner = ScriptableObject.CreateInstance<ScriptableObject>();

        private static void CreatePie()
        {
            EditorCoroutineUtility.StartCoroutine(DelayedCreatePie(), CoroutineOwner);
        }
        
        private static IEnumerator DelayedCreatePie()
        {
            yield return new EditorWaitForSeconds(1f);

            var todMgr = UnityEngine.Object.FindObjectOfType<TimeOfDayManager>();
            if (todMgr == null)
            {
                Debug.LogWarning("未找到 TimeOfDayManager 实例");
                yield break;
            }

            for (int i = 0, max = todMgr.todDatas.Length; i < max; i++)
            {
                var item = todMgr.todDatas[i];
                string menuFullPath = $"触发 TOD 时态/{item.phaseName}";
                int li = i;
                PieSystem.CreatePie(menuFullPath, () => todMgr.Launch(li));
            }
        }

        private static void RemovePie()
        {
            PieSystem.CleanUpPreviousPie();
        }
    }
}

#endif // UNITY_PIE_EDITOR
