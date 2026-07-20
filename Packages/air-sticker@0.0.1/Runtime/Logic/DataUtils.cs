// Created By: WangYu  Date: 2025-02-18

using System.Collections.Generic;
using UnityEngine;

namespace AirSticker.Runtime.Logic
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
        /// 把字典中对应 key 的 value 合并到 list 中
        /// </summary>
        public static void MergeValueFromDictionary<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict, 
            TKey key, List<TValue> list)
        {
            list.Clear();
            if (dict.TryGetValue(key, out var values))
            {
                foreach (var item in values)
                {
                    list.Add(item);
                }
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

        public static void GetComponentsInChildren(this GameObject go, out MeshRenderer[] meshRenderers, out MeshFilter[] meshFilters)
        {
            meshRenderers = go.GetComponentsInChildren<MeshRenderer>();
            
            int total = meshRenderers.Length;
            meshFilters = new MeshFilter[total];
            for (int i = 0; i < total; i++)
            {
                var mr = meshRenderers[i];
                var mf = mr.GetComponent<MeshFilter>();
                meshFilters[i] = mf;
            }
        }
        
    }
}