// Created By: WangYu  Date: 2025-03-26

using System;
using GameLogic;
using UnityEngine;
using XKT.TOD.Utils;
using UnityObject = UnityEngine.Object;

namespace XKT.TOD.DataStructure
{
    [Serializable]
    public class CharacterOverrideGIData
    {
        /// <summary>
        /// 层次结构路径
        /// </summary>
        [Header("层次结构路径")]
        public string hierarchyPath;
        
        public Vector3 position;
        public Quaternion rotation;

        public float priority;
        public float weight;
        
        public Color characterSkyColor;
        public Color characterEquatorColor;
        public Color characterGroundColor;

        public Vector4[] shConstants;
        
        public bool overrideEnvironmentLighting;
        
        public float unityLightProbeGIIntensity;
        public float characterGIIntensity;

        public Color characterMainLightColor;
        public float characterMainLightIntensity;
        public bool overrideCharacterMainLightDir;
        
        public void Collect()
        {
            var mono = UnityObject.FindObjectOfType<CharacterOverrideGI>();
            if (mono == null)
            {
                return;
            }
            
            this.hierarchyPath = TODUtils.GetHierarchyPath(mono.transform);

            this.position = mono.transform.position;
            this.rotation = mono.transform.rotation;

            this.priority = mono.priority;
            this.weight = mono.weight;
            
            this.characterSkyColor = mono.characterSkyColor;
            this.characterEquatorColor = mono.characterEquatorColor;
            this.characterGroundColor = mono.characterGroundColor;

            this.shConstants = mono.shConstants;

            this.overrideEnvironmentLighting = mono.overrideEnvironmentLighting;

            this.unityLightProbeGIIntensity = mono.unityLightProbeGIIntensity;
            this.characterGIIntensity = mono.characterGIIntensity;
            
            this.characterMainLightColor = mono.characterMainLightColor;
            this.characterMainLightIntensity = mono.characterMainLightIntensity;
            this.overrideCharacterMainLightDir = mono.overrideCharacterMainLightDir;
        }

        public CharacterOverrideGI Restore()
        {
            Transform parentT = TODUtils.FindHierarchyPath(this.hierarchyPath, 1);
            string goName = "CharacterOverrideGI";
            
            var newGO = new GameObject(goName);
            if (parentT != null)
            {
                newGO.transform.SetParent(parentT);
            }
            var mono = newGO.AddComponent<CharacterOverrideGI>();

            mono.transform.position = this.position;
            mono.transform.rotation = this.rotation;
            mono.transform.localScale = Vector3.one;

            mono.priority = this.priority;
            mono.weight = this.weight;
            
            mono.characterSkyColor = this.characterSkyColor;
            mono.characterEquatorColor = this.characterEquatorColor;
            mono.characterGroundColor = this.characterGroundColor;

            mono.shConstants = this.shConstants;

            mono.overrideEnvironmentLighting = this.overrideEnvironmentLighting;

            mono.unityLightProbeGIIntensity = this.unityLightProbeGIIntensity;
            mono.characterGIIntensity = this.characterGIIntensity;
            
            mono.characterMainLightColor = this.characterMainLightColor;
            mono.characterMainLightIntensity = this.characterMainLightIntensity;
            mono.overrideCharacterMainLightDir = this.overrideCharacterMainLightDir;
            
            return mono;
        }
        
    }
}
