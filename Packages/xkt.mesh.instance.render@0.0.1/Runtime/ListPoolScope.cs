/*******************************************************************************
 * File: ListPoolScope.cs
 * Author: fan.shi
 * Date: 2026-03-18
 * Description: 基于 UnityEngine.Rendering.ListPool 的 using 作用域释放封装。
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

/// <summary> 从 ListPool 取出 List，Dispose 时归还池。 </summary>
/// <typeparam name="T">列表元素类型。</typeparam>
public struct ListPoolScope<T> : IDisposable
{
    readonly List<T> _list;

    /// <summary> 从池中获取 List 并赋给 list 参数。 </summary>
    /// <param name="list">输出的 List 引用。</param>
    public ListPoolScope(out List<T> list)
    {
        list = ListPool<T>.Get();
        _list = list;
    }

    /// <summary> 将 List 归还池中（若非 null）。 </summary>
    public void Dispose()
    {
        if (_list != null)
        {
            ListPool<T>.Release(_list);
        }
    }
}
