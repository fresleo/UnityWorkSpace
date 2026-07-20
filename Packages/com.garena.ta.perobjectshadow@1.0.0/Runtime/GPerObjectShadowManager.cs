using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Garena.TA
{
    /// <summary>
    /// 逐物体阴影对象和渲染数据管理
    /// </summary>
    public partial class GPerObjectShadowManager
    {
        public static GPerObjectShadowManager Instance { get; private set; } = new GPerObjectShadowManager();

        private GPerObjectShadowManager()
        {

        }

        public Vector2 Offset { get; set; }

        #region Per Object Shadow Target

        /// <summary>使用常驻变量替换new，降低GC，该变量只在CullTarget方法内使用</summary>
        private Plane[] cullPlanes = new Plane[6];

        /// <summary>
        /// 手动注册的对象
        /// </summary>
        private List<GPerObjectShadowTargetData> RegisterTarget = new List<GPerObjectShadowTargetData>();

        /// <summary>
        /// 使用过滤器筛选出的对象
        /// </summary>
        private List<GPerObjectShadowTargetData> FilterFindTarget = new List<GPerObjectShadowTargetData>();

        /// <summary>
        /// 所有对象（手动注册+根据规则筛选）
        /// </summary>
        private List<GPerObjectShadowTargetData> AllTargetList = new List<GPerObjectShadowTargetData>();

        /// <summary>
        /// 最终使用的对象
        /// </summary>
        public List<GPerObjectShadowTargetData> FinalTargetList = new List<GPerObjectShadowTargetData>();

        public Light ShadowLight { get; set; }

        public void Add(GPerObjectShadowTargetData data)
        {
            if (!RegisterTarget.Contains(data))
                RegisterTarget.Add(data);
        }

        public void Remove(GPerObjectShadowTargetData data)
        {
            RegisterTarget.Remove(data);
        }

        public void DisableAll()
        {
            foreach (var item in AllTargetList)
            {
                if (item.go == null)
                    continue;

                item.Disable();
            }
            AllTargetList.Clear();
        }

        bool preSRPBatch = true;

        public void CascadeShadow(bool show = true)
        {
            // 先将阴影状态全部还原
            for (int i = 0; i < AllTargetList.Count; i++)
            {
                if (AllTargetList[i].go == null || !AllTargetList[i].ShadowCastingReady()) continue;

                AllTargetList[i].IsEnablePerObjectShadow(false);
            }
            // 只修改用到的目标的阴影
            for (int i = 0; i < FinalTargetList.Count; i++)
            {
                if (FinalTargetList[i].go == null || !FinalTargetList[i].ShadowCastingReady()) continue;

                FinalTargetList[i].IsEnablePerObjectShadow(!show);
            }
        }

        /// <summary>
        /// 获取最终需要有逐物体阴影的目标
        /// </summary>
        public void UpdateFinalTargetDataList(GPerObjectShadowPassSettings settings, Camera cam)
        {
            UpdateAllTargetList(settings);

            foreach (var item in AllTargetList)
            {
                item.UpdateBounds();
                item.UpdateCameraDistanceAndLOD(cam, settings.LODBias);
            }

            SortTarget(settings, cam);

            CullTarget(settings, cam);

            if (settings.srpBatcher)
            {
                for (int i = 0; i < FinalTargetList.Count; i++)
                {
                    FinalTargetList[i].UpdateRenderingLayerMask(i);
                }
            }
            else if (preSRPBatch)
            {
                for (int i = 0; i < FinalTargetList.Count; i++)
                {
                    FinalTargetList[i].ResetRenderingLayerState();
                }
            }

            preSRPBatch = settings.srpBatcher;
        }

        /// <summary>
        /// 收集所有逐物体阴影目标
        /// </summary>
        void UpdateAllTargetList(GPerObjectShadowPassSettings settings)
        {
            if (!settings.UseFilter)
            {
                AllTargetList.Clear();
                AllTargetList.AddRange(RegisterTarget);
                return;
            }
            else
            {
                AllTargetList.Clear();
                AllTargetList.AddRange(RegisterTarget);
            }

            var arr = GameObject.FindGameObjectsWithTag(settings.filterSettings.Tag);

            for (int i = FilterFindTarget.Count - 1; i >= 0; i--)
            {
                var item = FilterFindTarget[i];
                if (item.go == null)
                {
                    FilterFindTarget.RemoveAt(i);
                    continue;
                }

                //避免编辑器下对资产进行修改
#if UNITY_EDITOR
                if (UnityEditor.EditorUtility.IsPersistent(item.go))
                    continue;

                if (UnityEditor.SceneManagement.EditorSceneManager.IsPreviewSceneObject(item.go))
                    continue;

                if (item.go.scene == null)
                    continue;
#endif

                if (!arr.Contains(item.go))
                {
                    item.Disable();
                    FilterFindTarget.RemoveAt(i);
                    continue;
                }
            }

            foreach (var item in arr)
            {
                if (!FilterFindTarget.Exists(c => c.go == item))
                {
                    var data = new GPerObjectShadowTargetData();
                    data.go = item;

                    if (!data.enabled)
                        data.Enable();

                    FilterFindTarget.Add(data);
                }
            }

            AllTargetList.AddRange(FilterFindTarget);
        }


        void CullTarget(GPerObjectShadowPassSettings settings, Camera cam)
        {
            GeometryUtility.CalculateFrustumPlanes(cam, cullPlanes);

            FinalTargetList.Clear();

            for (int i = AllTargetList.Count - 1; i >= 0; i--)
            {
                var slice = AllTargetList[i];

                if (FinalTargetList.Count >= settings.MaxCount)
                {
                    slice.Disable();
                    continue;
                }

                if (slice.distance > settings.Distance)
                {
                    slice.Disable();
                    continue;
                }

                if (settings.Cull && !GPerObjectShadowUtils.InFrustum(cullPlanes, slice.bounds))
                {
                    slice.Disable();
                    continue;
                }

                if (!slice.enabled)
                    slice.Enable();

                FinalTargetList.Add(slice);
            }
        }

        void SortTarget(GPerObjectShadowPassSettings settings, Camera cam)
        {
            if (!settings.Sort)
                return;

            // 冒泡排序替换Sort，降低GC
            GPerObjectShadowTargetData temp = null;
            int count = AllTargetList.Count - 1;
            for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < count - i; j++)
                {
                    float da = Vector3.Distance(cam.transform.position, AllTargetList[j].go.transform.position);
                    float db = Vector3.Distance(cam.transform.position, AllTargetList[j + 1].go.transform.position);
                    if (da > db)
                    {
                        temp = AllTargetList[j];
                        AllTargetList[j] = AllTargetList[j + 1];
                        AllTargetList[j + 1] = temp;
                    }
                }
            }

            // AllTargetList.Sort((l, r) =>
            // {
            //     float da = Vector3.Distance(cam.transform.position, l.go.transform.position);
            //     float db = Vector3.Distance(cam.transform.position, r.go.transform.position);
            //     return db.CompareTo(da);
            // });
        }

        #endregion

        #region Per Object Shadow Data

        public GPerObjectShadowData data = new GPerObjectShadowData();

        public GPerObjectShadowData selfData = new GPerObjectShadowData();

        /// <summary>
        /// 更新renderer状态，当逐物体对象下的renderer有变化时调用此函数
        /// </summary>
        /// <param name="go"></param>
        public void RefreshTargetRendererState(GameObject go)
        {
            foreach (var target in AllTargetList)
            {
                if (target.go == go)
                {
                    target.SaveCastShadowState(true);
                }
            }
        }

        #endregion
    }
}