// Created By: WangYu  Date: 2024-11-25

using System.Collections.Generic;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace SkinnedDecals
{
    public static class DataUtils
    {
        /// <summary>
        /// 清理字典和 Value 上的 List 对象容器
        /// </summary>
        public static void ClearDictionaryAndInternalList<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict)
        {
            foreach (var iter in dict)
            {
                iter.Value.Clear();
            }
            dict.Clear();
        }
        
        /// <summary>
        /// 把1个字典中的值数据合并到1个列表中
        /// </summary>
        public static void MergeValueFromDictionaryIntoList<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict, List<TValue> list)
        {
            list.Clear();
            foreach (var iter in dict)
            {
                list.AddRange(iter.Value);
            }
        }
        
        public static void ClearListFromDictionary<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict, TKey key)
        {
            if (dict.TryGetValue(key, out var list))
            {
                list.Clear();
                dict.Remove(key);
            }
        }
        
        public static void Destroy(UnityObject obj)
        {
            if(obj == null) return;
            
#if UNITY_EDITOR
            if (Application.isPlaying && !UnityEditor.EditorApplication.isPaused)
                UnityObject.Destroy(obj);
            else
                UnityObject.DestroyImmediate(obj);
#else
                UnityObject.Destroy(obj);
#endif
        }
    }
}