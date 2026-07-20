/// <summary>
/// 假聚焦灯光效果
/// 作者：Ling mei an
/// 修改日期：2025-9-12
/// 功能：假聚焦灯光效果。
/// </summary>


#if UNITY_EDITOR
using UnityEngine;
namespace  knightTA.FakeSpotLightTool{
    [ExecuteInEditMode]
public class TitleAttribute : PropertyAttribute
{
    public string newTitle { get ; private set; }    
    public TitleAttribute( string title )
    {
        newTitle = title ;
    }
}
}
#endif