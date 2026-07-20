/*******************************************************************************
 * File: TimeOfDayFixedTime.cs
 * Author: WangYu
 * Date: 2025-03-20
 * Description: TOD 管理器，公用同1份 Lightmap 纹理，保存4个时间段的数据，在运行时实现瞬间切换的效果。（是兼顾效果，容量，性能的一种妥协方案）
 *
 * Notice:
 *******************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XKT.TOD.DataStructure;
using XKT.TOD.Utils;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace XKT.TOD
{
    [ExecuteAlways]
    [AddComponentMenu("TOD/TimeOfDayManager")]
    public class TimeOfDayManager : MonoBehaviour
    {
        /// <summary>
        /// tod 数据组
        /// </summary>
        public StoredTimeOfDayData[] todDatas;

        /// <summary>
        /// 开始时启动
        /// </summary>
        public bool startLaunch;
        /// <summary>
        /// 主灯资源
        /// </summary>
        public GameObject mainLightAsset;
        /// <summary>
        /// 当前场景的 Lightmap 计数
        /// </summary>
        public int rawLightmapCount;
        
        
        // 数据恢复器
        private StoredTimeOfDayDataRestorer m_dataRestorer = new();
        // 时钟引用
        private int _startLaunchTimer, _launchTimer = -1;
        // 启动索引
        private int m_launchIndex = -1;
        // 创建的 GO 列表
        private List<GameObject> m_goList = new();

        private const string c_MainLightAssetPath = "Packages/xkt.tod/Prefabs/MainLight.prefab";
        private static readonly int _BakedGITint = Shader.PropertyToID("_BakedGITint");

        private SimpleTimer _simpleTimer;

        private void OnDestroy()
        {
            m_dataRestorer.Exit(); // 在 tod 退出时，为了兼容非 tod 场景，可能需要有一些处理
            
            Shader.SetGlobalColor(_BakedGITint, Color.white);
            
            StopLaunchCoroutines();
            DestroyGoList();
            if (_simpleTimer != null)
            {
                _simpleTimer.Destroy();
                _simpleTimer = null;
            }
        }

        private void Start()
        {
            #if UNITY_EDITOR

            if (mainLightAsset == null)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(c_MainLightAssetPath);
                if (!asset)
                {
                    Debug.LogError($"目标资源未找到：{c_MainLightAssetPath}");
                    return;
                }

                mainLightAsset = asset;
            }
            
            #endif // UNITY_EDITOR
            
            rawLightmapCount = LightmapSettings.lightmaps.Length;
            
            if (Application.isPlaying)
            {
                //初始化简易计时器
                if (_simpleTimer == null)
                {
                    _simpleTimer = new SimpleTimer();
                    _simpleTimer.InitTimer();
                }
                
                if (startLaunch)
                {
                    StopLaunchCoroutines();
                    _startLaunchTimer = _simpleTimer.SetTrigger(0.1f, () =>
                    {
                        LoadTodData(0, null);
                    });
                }
            }
        }
        
        private void Update()
        {
            if (_simpleTimer != null)
            {
                _simpleTimer.Update(Time.deltaTime);    
            }
            
            if (this.LaunchTodData)
            {
                Shader.SetGlobalColor(_BakedGITint, this.LaunchTodData.bakedGITint);
            }
        }


        private void StopLaunchCoroutines()
        {
            if (_simpleTimer == null)
            {
                return;
            }
            
            if (_startLaunchTimer >= 0)
            {
                _simpleTimer.RemoveTrigger(_startLaunchTimer);
            }

            if (_launchTimer >= 0)
            {
                _simpleTimer.RemoveTrigger(_startLaunchTimer);
            }
        }
        
        private void DestroyGoList()
        {
            foreach (var itemGo in m_goList)
            {
                if (itemGo != null)
                {
                    TODUtils.DestroyUnityObject(itemGo);
                }
            }
            m_goList.Clear();
        }
        
        public StoredTimeOfDayData LaunchTodData
        {
            get
            {
                if (todDatas == null)
                {
                    return null;
                }

                if (m_launchIndex < 0 || m_launchIndex >= todDatas.Length)
                {
                    return null;
                }
                
                return todDatas[m_launchIndex];
            }
        }
        
        /// <summary>
        /// 启动
        /// </summary>
        public void Launch(int index, Action callback = null)
        {
            StopLaunchCoroutines();
            if (_simpleTimer != null)
            {
                _launchTimer = _simpleTimer.SetTrigger(0.1f, () =>
                {
                    LoadTodData(index, callback);
                });
            }
        }

        /// <summary>
        /// 获取当前氛围索引
        /// </summary>
        public int GetCurIdx()
        {
            return m_launchIndex;
        }

        private void LoadTodData(int index, Action callback)
        {
            DestroyGoList();
            
            if (this.todDatas == null)
            {
                Debug.LogError("没有 TOD 配置数据");
                return;
            }

            int todTotal = this.todDatas.Length;
            if (index < 0 || index >= todTotal)
            {
                Debug.LogError($"TOD 数据索引超出范围: {index}/{todTotal} ，所以当前自动加载首套数据。");
            }
            m_launchIndex = Mathf.Clamp(index, 0, todTotal);
            
            m_dataRestorer.createdGOL = m_goList;
            m_dataRestorer.mainLightAsset = this.mainLightAsset;
            m_dataRestorer.rawLightmapCount = this.rawLightmapCount;
            
            m_dataRestorer.Execute(this.LaunchTodData, m_launchIndex);

            // 刷新各 LOD 级别的 lightmap 信息
            var lls = TODUtils.FindObjectsOfTypeInActiveScene<LightmappedLOD>();
            foreach (var ll in lls)
            {
                ll.RendererInfoTransfer();
            }
            
            callback?.Invoke();
        }
        
    }
}