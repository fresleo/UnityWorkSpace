// Created By: WangYu  Date: 2025-03-17

using System;
using UnityEngine;
using UnityEngine.Rendering;
using XKT.TOD.Utils;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XKT.TOD.DataStructure
{
    [Serializable]
    public class ReflectionProbeData
    {
        /// <summary>
        /// 层次结构路径
        /// </summary>
        [Header("层次结构路径")]
        public string hierarchyPath;
        
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        
        public Cubemap reflectionTexture;
        public bool renderDynamicObjects;

        public int importance;
        public float intensity;
        public bool boxProjection;
        public float blendDistance;
        public Vector3 center;
        public Vector3 size;
        
#if UNITY_EDITOR
        public void Collect(TextureImporterSettings tis, string storeFullPath, ReflectionProbe rp)
        {
            this.hierarchyPath = TODUtils.GetHierarchyPath(rp.transform);

            this.position = rp.transform.position;
            this.rotation = rp.transform.rotation;
            this.scale = rp.transform.localScale;
            
            if (rp.mode == ReflectionProbeMode.Baked)
            {
                rp.bakedTexture = null;
                rp.customBakedTexture = null;
                if (Lightmapping.BakeReflectionProbe(rp, storeFullPath))
                {
                    var importer = (TextureImporter)AssetImporter.GetAtPath(storeFullPath);
                    importer.SetTextureSettings(tis);
                    importer.SaveAndReimport();
                }

                rp.mode = ReflectionProbeMode.Custom;
                rp.customBakedTexture = rp.bakedTexture;

                this.reflectionTexture = rp.bakedTexture as Cubemap;
            }
            else if (rp.mode == ReflectionProbeMode.Custom && rp.customBakedTexture != null)
            {
                this.reflectionTexture = rp.customBakedTexture as Cubemap;
            }
            else if (rp.mode == ReflectionProbeMode.Custom && rp.customBakedTexture == null)
            {
                rp.bakedTexture = null;
                rp.customBakedTexture = null;
                if (Lightmapping.BakeReflectionProbe(rp, storeFullPath))
                {
                    var importer = (TextureImporter)AssetImporter.GetAtPath(storeFullPath);
                    importer.SetTextureSettings(tis);
                    importer.SaveAndReimport();
                }

                rp.mode = ReflectionProbeMode.Custom;

                this.reflectionTexture = rp.customBakedTexture as Cubemap;
            }

            this.renderDynamicObjects = rp.renderDynamicObjects;

            this.importance = rp.importance;
            this.intensity = rp.intensity;
            this.boxProjection = rp.boxProjection;
            this.blendDistance = rp.blendDistance;
            this.center = rp.center;
            this.size = rp.size;
        }
#endif // UNITY_EDITOR
        
        public ReflectionProbe Restore()
        {
            Transform parentT = TODUtils.FindHierarchyPath(this.hierarchyPath, 1);
            string goName = TODUtils.GetHierarchyGameObjectName(this.hierarchyPath);
            
            var newGO = new GameObject(goName);
            if (parentT != null)
            {
                newGO.transform.SetParent(parentT);
            }
            var rp = newGO.AddComponent<ReflectionProbe>();

            rp.transform.position = this.position;
            rp.transform.rotation = this.rotation;
            rp.transform.localScale = this.scale;
            
            rp.mode = ReflectionProbeMode.Custom;
            rp.customBakedTexture = this.reflectionTexture;

            rp.renderDynamicObjects = this.renderDynamicObjects;

            rp.importance = this.importance;
            rp.intensity = this.intensity;
            rp.boxProjection = this.boxProjection;
            rp.blendDistance = this.blendDistance;
            rp.center = this.center;
            rp.size = this.size;

            return rp;
        }
        
    }
}