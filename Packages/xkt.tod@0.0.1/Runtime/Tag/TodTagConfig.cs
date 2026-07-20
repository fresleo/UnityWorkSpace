// Created By: WangYu  Date: 2025-04-10

using System;
using System.Collections.Generic;
using UnityEngine;

namespace XKT.TOD.Tag
{
    [Serializable]
    [CreateAssetMenu(fileName = nameof(TodTagConfig), menuName = "TOD/创建 TOD 标记配置", order = 1)]
    public class TodTagConfig : ScriptableObject
    {
        public List<TodTagItem> todTagList = new();
    }

    [Serializable]
    public class TodTagItem
    {
        /// <summary>
        /// hierarchy 结构路径
        /// </summary>
        public string hierarchyPath;

        /// <summary>
        /// 脚本 Id
        /// </summary>
        public string scriptId;

        /// <summary>
        /// 类型全名
        /// </summary>
        public string typeFullName;
        
    }
}