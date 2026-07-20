// Created By: WangYu  Date: 2023-12-21

using com.xknight.mt.Lib.Runtime.MT.Serialize;
using com.xknight.mt.Lib.Runtime.MT.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.Utils
{
    public static class MTEditorUtils
    {
        /// <summary>
        /// 判断 GameObject 是否是 Prefab
        /// </summary>
        public static bool IsPrefab(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return false;
            }
            
            // Project中的Prefab是Asset不是Instance
            if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
            {
                return true;
            }

            // Scene中的Prefab Instance是Instance不是Asset
            if (PrefabUtility.IsPartOfPrefabInstance(gameObject))
            {
                return true;
            }

            // PrefabMode中的GameObject既不是Instance也不是Asset
            var prefabStage = PrefabStageUtility.GetPrefabStage(gameObject);
            if (prefabStage != null)
            {
                return true;
            }

            // 不是预制体
            return false;
        }

        /// <summary>
        /// 获取 Prefab 的资源路径
        /// </summary>
        public static string GetPrefabAssetPath(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return string.Empty;
            }
            
            /*
            Unity中一个预制体对象可能处于3种状态：
            位于Project中，是PrefabAsset；
            位于Scene中，是PrefabInstance；
            位于PrefabMode Scene中，既不是PrefabAsset也不是Prefab Instance。
            */

            // Project中的Prefab是Asset不是Instance
            if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
            {
                // 预制体资源就是自身
                return AssetDatabase.GetAssetPath(gameObject);
            }

            // Scene中的Prefab Instance是Instance不是Asset
            if (PrefabUtility.IsPartOfPrefabInstance(gameObject))
            {
                // 获取预制体资源
                var prefabAsset = PrefabUtility.GetCorrespondingObjectFromOriginalSource(gameObject);
                return AssetDatabase.GetAssetPath(prefabAsset);
            }

            // PrefabMode中的GameObject既不是Instance也不是Asset
            var prefabStage = PrefabStageUtility.GetPrefabStage(gameObject);
            if (prefabStage != null)
            {
                // 预制体资源：prefabAsset = prefabStage.prefabContentsRoot
                return prefabStage.assetPath;
            }

            // 不是预制体
            return string.Empty;
        }
        
        public static LightmapConfig GetLightmapData(GameObject go)
        {
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            LightmapConfig ld = new LightmapConfig();
            
            StaticEditorFlags sef = GameObjectUtility.GetStaticEditorFlags(mr.gameObject);
            ld.baked = FlagsUtil<StaticEditorFlags>.Has(sef, StaticEditorFlags.ContributeGI);

            if (ld.baked)
            {
                ld.index = mr.lightmapIndex;
                ld.scaleOffset = mr.lightmapScaleOffset;
            }

            return ld;
        }
        
        public static LightmapConfig[] GetLightmapDatas(GameObject go)
        {
            MeshRenderer[] mrs = go.GetComponentsInChildren<MeshRenderer>();
            
            int mrsLen = mrs.Length;
            LightmapConfig[] lds = new LightmapConfig[mrsLen];

            for (int i = 0; i < mrsLen; i++)
            {
                MeshRenderer mr = mrs[i];
                LightmapConfig ld = lds[i];
                
                StaticEditorFlags sef = GameObjectUtility.GetStaticEditorFlags(mr.gameObject);
                ld.baked = FlagsUtil<StaticEditorFlags>.Has(sef, StaticEditorFlags.ContributeGI);

                if (ld.baked)
                {
                    ld.index = mr.lightmapIndex;
                    ld.scaleOffset = mr.lightmapScaleOffset;
                }
                
                //结构体需要再赋值回去
                lds[i] = ld;
            }
            
            return lds;
        }
        
    }
}