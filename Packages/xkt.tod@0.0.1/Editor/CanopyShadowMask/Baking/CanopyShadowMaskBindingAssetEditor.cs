/*******************************************************************************
 * File: CanopyShadowMaskBindingAssetEditor.cs
 * Author: WangYu
 * Date: 2026-07-08
 * Description: 
 * Notice: 
 ******************************************************************************/

using UnityEditor;
using UnityEngine;

namespace XKT.TOD.CanopyShadowMask
{
    [CustomEditor(typeof(CanopyShadowMaskBindingAsset))]
    public sealed class CanopyShadowMaskBindingAssetEditor : Editor
    {
        CanopyShadowMaskBindingAsset CurrentTarget => this.target as CanopyShadowMaskBindingAsset;
        
        public override void OnInspectorGUI()
        {
            if (CurrentTarget == null)
            {
                return;
            }
            
            base.OnInspectorGUI();
            
            EditorGUILayout.Space();
            if (GUILayout.Button("替换为树冠 Shadowmask"))
            {
                int count = CurrentTarget.ApplyCanopyShadowMasks();
                Debug.Log("已替换树冠 shadowmask 数量: " + count);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("还原官方 Shadowmask"))
            {
                int count = CurrentTarget.ApplyOfficialShadowMasks();
                Debug.Log("已还原官方 shadowmask 数量: " + count);
            }
        }
        
    }
}