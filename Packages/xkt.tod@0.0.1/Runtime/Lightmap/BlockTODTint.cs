// Created By: WangYu  Date: 2025-07-02

using System;
using UnityEngine;

namespace XKT.TOD.Lightmap
{
    /// <summary>
    /// 屏蔽 TOD 的色调调整
    /// </summary>
    public class BlockTODTint : MonoBehaviour
    {
        #if UNITY_EDITOR
        
        public bool isBlock;
        
        private static readonly int _BakedGITintIntensity = Shader.PropertyToID("_BakedGITintIntensity");
        

        private void OnValidate()
        {
            SetBakedGITintIntensity(isBlock);
        }

        private void SetBakedGITintIntensity(bool blockAllChild)
        {
            var mrs = this.gameObject.GetComponentsInChildren<MeshRenderer>();
            foreach (var mr in mrs)
            {
                Material[] mats = mr.sharedMaterials;
                
                if(mats == null) continue;
                
                foreach (var mat in mats)
                {
                    if(mat == null) continue;
                    
                    if (mat.HasProperty(_BakedGITintIntensity))
                    {
                        mat.SetFloat(_BakedGITintIntensity, blockAllChild ? 0 : 1);
                    }
                }
            }
        }
        
        #endif // UNITY_EDITOR
    }
}