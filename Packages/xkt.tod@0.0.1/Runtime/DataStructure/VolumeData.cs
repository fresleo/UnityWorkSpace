// Created By: WangYu  Date: 2025-03-24

using System;
using UnityEngine;
using UnityEngine.Rendering;
using XKT.TOD.Utils;

namespace XKT.TOD.DataStructure
{
    [Serializable]
    public class VolumeData
    {
        /// <summary>
        /// 层次结构路径
        /// </summary>
        [Header("层次结构路径")]
        public string hierarchyPath;
        
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        
        public VolumeProfile profile;

        public void Collect(Volume volume)
        {
            if(volume == null) return;

            this.hierarchyPath = TODUtils.GetHierarchyPath(volume.transform);

            this.position = volume.transform.position;
            this.rotation = volume.transform.rotation;
            this.scale = volume.transform.localScale;

            this.profile = volume.sharedProfile;
        }

        public Volume Restore()
        {
            Transform parentT = TODUtils.FindHierarchyPath(this.hierarchyPath, 1);
            string goName = TODUtils.GetHierarchyGameObjectName(this.hierarchyPath);
            
            var newGO = new GameObject(goName);
            if (parentT != null)
            {
                newGO.transform.SetParent(parentT);
            }
            var volume = newGO.AddComponent<Volume>();
            
            volume.transform.position = this.position;
            volume.transform.rotation = this.rotation;
            volume.transform.localScale = this.scale;

            volume.profile = this.profile;

            return volume;
        }
        
    }
}