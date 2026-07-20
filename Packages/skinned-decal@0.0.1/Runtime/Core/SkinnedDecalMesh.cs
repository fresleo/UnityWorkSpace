using System;
using System.Collections;
using System.Collections.Generic;
using com.xknight.mt.Lib.Runtime.MT.Utils;
using UnityEngine;

namespace SkinnedDecals
{
    /// <summary>
    /// 蒙皮贴花网格控制器
    /// </summary>
    public class SkinnedDecalMesh : MonoBehaviour
    {
        /// <summary>
        /// 贴花系统
        /// </summary>
        public SkinnedDecalSystem decalSystem;
        /// <summary>
        /// 贴花构建器
        /// </summary>
        public SkinnedDecalBuilder decalBuilder;
        
        /// <summary>
        /// 贴花网格
        /// </summary>
        public Mesh decalMesh;
        /// <summary>
        /// 蒙皮网格渲染器
        /// </summary>
        public SkinnedMeshRenderer decalSmr;
        
        /// <summary>
        /// 屏幕外更新
        /// </summary>
        public bool updateWhenOffscreen;

        /// <summary>
        /// 当前贴花唯一 key
        /// </summary>
        public long currentDecalUniqueKey;
        
        /// <summary>
        /// 贴花类型字典
        /// </summary>
        public Dictionary<long, SkinnedDecal> decalTypes = new();
        /// <summary>
        /// 三角形索引字典
        /// </summary>
        public SortedDictionary<long, List<int>> decalTriangles = new();
        
        // mesh 数据容器
        public SortedDictionary<long, List<Vector3>> decalVertices = new();
        public SortedDictionary<long, List<Vector3>> decalNormals = new();
        public SortedDictionary<long, List<Vector4>> decalTangents = new();
        public SortedDictionary<long, List<Color32>> decalColors = new();
        public SortedDictionary<long, List<Vector2>> decalUvs = new();
        public SortedDictionary<long, List<BoneWeight>> decalBoneWeights = new();
        
        /// <summary>
        /// 可能的贴花中心点
        /// </summary>
        public Dictionary<long, List<Vector3>> possibleDecalCenterPoints = new(); 

        public List<Vector3> allDecalVertices = new();
        public List<Vector3> allDecalNormals = new();
        public List<Vector4> allDecalTangents = new();
        public List<Color32> allDecalColors = new();
        public List<Vector2> allDecalUvs = new();
        public List<BoneWeight> allDecalBoneWeights = new();
        
        /// <summary>
        /// 材质字典
        /// </summary>
        public SortedDictionary<long, Material> decalMaterials = new();
        // 临时材质列表
        private List<Material> m_tempMaterials = new();
        
        // 生命周期协程
        private Coroutine m_destroySelfCoroutine;
        private Dictionary<long, Coroutine> m_decalLifeCoroutines = new();
        
        // 用来在不实例化材质时的参数修改
        private MaterialPropertyBlock m_mpb;

        
        private void OnDestroy()
        {
            Release();
        }

        private void Awake()
        {
            m_mpb = new MaterialPropertyBlock();
        }

        private void Start() {}

        
        /// <summary>
        /// 释放
        /// </summary>
        public void Release()
        {
            // 停止自毁的协程
            StopDestroySelfCoroutine();
            // 结束所有贴花的生命周期
            foreach (var iter in m_decalLifeCoroutines)
            {
                Coroutine item = iter.Value;
                if (item == null) continue;
                StopCoroutine(item);
            }
            m_decalLifeCoroutines.Clear();

            // 释放 Mesh
            if (decalMesh != null)
            {
                decalMesh.Clear();
                Destroy(decalMesh);
                decalMesh = null;
            }
            
            decalTypes.Clear();
            DataUtils.ClearDictionaryAndInternalList(decalTriangles);
            
            DataUtils.ClearDictionaryAndInternalList(decalVertices);
            DataUtils.ClearDictionaryAndInternalList(decalNormals);
            DataUtils.ClearDictionaryAndInternalList(decalTangents);
            DataUtils.ClearDictionaryAndInternalList(decalColors);
            DataUtils.ClearDictionaryAndInternalList(decalUvs);
            DataUtils.ClearDictionaryAndInternalList(decalBoneWeights);
            DataUtils.ClearDictionaryAndInternalList(possibleDecalCenterPoints);
            
            allDecalVertices.Clear();
            allDecalNormals.Clear();
            allDecalTangents.Clear();
            allDecalColors.Clear();
            allDecalUvs.Clear();
            allDecalBoneWeights.Clear();
            
            foreach (var iter in decalMaterials)
            {
                Material item = iter.Value;
                if(item == null) continue;
                Destroy(item);
            }
            decalMaterials.Clear();
            m_tempMaterials.Clear();
        }
        
        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize(SkinnedDecalSystem sds, SkinnedDecalBuilder sdb)
        {
            decalSystem = sds;
            decalBuilder = sdb;
            
            SkinnedMeshRenderer originalSmr = sdb.originalSmr;
            
            decalMesh = new Mesh();
            if (decalSystem.markDynamic)
            {
                decalMesh.MarkDynamic();
            }
            decalMesh.bindposes = originalSmr.sharedMesh.bindposes;
            
            // 创建新的蒙皮网格渲染器
            decalSmr = gameObject.AddComponent<SkinnedMeshRenderer>();
            // 关闭投影
            decalSmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            
            // 同步骨骼
            decalSmr.rootBone = originalSmr.rootBone;
            decalSmr.bones = originalSmr.bones;
            
            // 根据原始渲染器的设置，更新自己的设置
            this.updateWhenOffscreen = originalSmr.updateWhenOffscreen;
            decalSmr.updateWhenOffscreen = this.updateWhenOffscreen;
        }

        /// <summary>
        /// 更新网格
        /// </summary>
        public void UpdateMesh()
        {
            StopDestroySelfCoroutine();
            
            InternalUpdateMesh();
            UpdateMaterial();
            
            StartLife();
        }

        private void InternalUpdateMesh()
        {
            decalMesh.Clear();

            // 顶点
            allDecalVertices.Clear();
            foreach (var iter in decalVertices) allDecalVertices.AddRange(iter.Value);
            decalMesh.SetVertices(allDecalVertices);
            
            // 法线
            allDecalNormals.Clear();
            foreach (var iter in decalNormals) allDecalNormals.AddRange(iter.Value);
            decalMesh.SetNormals(allDecalNormals);
            
            // 切线
            allDecalTangents.Clear();
            foreach (var iter in decalTangents) allDecalTangents.AddRange(iter.Value);
            decalMesh.SetTangents(allDecalTangents);
            
            // 顶点色
            allDecalColors.Clear();
            foreach (var iter in decalColors) allDecalColors.AddRange(iter.Value);
            decalMesh.SetColors(allDecalColors);
            
            // UV
            allDecalUvs.Clear();
            foreach (var iter in decalUvs) allDecalUvs.AddRange(iter.Value);
            decalMesh.SetUVs(0, allDecalUvs);
            
            // 骨骼权重
            allDecalBoneWeights.Clear();
            foreach (var iter in decalBoneWeights) allDecalBoneWeights.AddRange(iter.Value);
            decalMesh.boneWeights = allDecalBoneWeights.ToArray();
            
            // 三角形索引
            decalMesh.subMeshCount = decalTriangles.Count;
            int subCounter = 0;
            foreach (var iter in decalTriangles)
            {
                decalMesh.SetIndices(iter.Value, MeshTopology.Triangles, subCounter);
                subCounter++;
            }
            
            decalMesh.RecalculateBounds(); // 重新计算边界
            decalMesh.Optimize(); // 优化网格，提升运行时性能
            
            if (decalSmr.sharedMesh == null) decalSmr.sharedMesh = decalMesh;
        }
        
        private void UpdateMaterial()
        {
            m_tempMaterials.Clear();
            int counter = 0;
            foreach (var iter in decalMaterials)
            {
                var dk = iter.Key;
                Material mat = iter.Value;

                mat.renderQueue = decalTypes[dk].material.renderQueue + counter;
                m_tempMaterials.Add(mat);

                counter++;
            }
            decalSmr.sharedMaterials = m_tempMaterials.ToArray();
            m_tempMaterials.Clear();
        }
        
        private void StartLife()
        {
            if (!m_decalLifeCoroutines.ContainsKey(currentDecalUniqueKey))
            {
                ChangeDissolve(currentDecalUniqueKey, 0);
                
                // 通过 key 启动相应贴花的生命周期
                var lc = StartCoroutine(OnStartLife(currentDecalUniqueKey));
                m_decalLifeCoroutines.Add(currentDecalUniqueKey, lc);
            }
        }
        
        private IEnumerator OnStartLife(long decalUniqueKey)
        {
            var decalType = decalTypes[decalUniqueKey];
            
            // 无线持续时间
            if (decalType.duration <= 0) yield break;

            // 淡入过程
            float fadeinTimer = 0;
            while (fadeinTimer < decalType.fadeinTime)
            {
                float progress = decalType.fadeinCurve.Evaluate(fadeinTimer / decalType.fadeinTime);
                ChangeShowProgress(decalUniqueKey, progress);
                
                fadeinTimer += Time.deltaTime;
                yield return null; // 等待下一帧
            }
            
            // 活着的时间
            yield return YieldInstructionPool.GetWaitForSeconds(decalType.duration);
            
            // 淡出过程
            float fadeoutTimer = 0;
            while (fadeoutTimer < decalType.fadeoutTime)
            {
                // 改透明度
                float progress = decalType.fadeoutCurve.Evaluate(fadeoutTimer / decalType.fadeoutTime);
                ChangeDissolve(decalUniqueKey, progress);
                
                fadeoutTimer += Time.deltaTime;
                yield return null; // 等待下一帧
            }
            
            // 移出协程字典
            m_decalLifeCoroutines.Remove(decalUniqueKey);
            
            // 找到整理三角形索引的起点
            int limitIndex = 0;
            if (decalVertices.TryGetValue(decalUniqueKey, out var list))
            {
                limitIndex = list.Count;
            }
            
            // 移除废数据
            decalTriangles.Remove(decalUniqueKey);
            
            decalVertices.Remove(decalUniqueKey);
            decalNormals.Remove(decalUniqueKey);
            decalTangents.Remove(decalUniqueKey);
            decalColors.Remove(decalUniqueKey);
            decalUvs.Remove(decalUniqueKey);
            decalBoneWeights.Remove(decalUniqueKey);
            
            // 重新整理剩余的三角形索引
            foreach (var iter in decalTriangles)
            {
                for (int i = 0; i < iter.Value.Count; i++)
                {
                    if (iter.Value[i] >= limitIndex)
                    {
                        iter.Value[i] -= limitIndex;
                    }
                }
            }

            // 移除一些资源向的字典
            decalTypes.Remove(decalUniqueKey);
            
            if (decalSystem.instantiateMaterial)
            {
                var mat = decalMaterials[decalUniqueKey];
                if(mat != null) Destroy(mat);
            }
            decalMaterials.Remove(decalUniqueKey);
            
            // 刷新显示
            InternalUpdateMesh();
            UpdateMaterial();
        }

        
        #region 修改材质参数
        private static readonly int _ShowProgressY = Shader.PropertyToID("_ShowProgressY");
        private static readonly int _DissolveCutoff = Shader.PropertyToID("_DissolveCutoff");

        /// <summary>
        /// 修改显示进度
        /// </summary>
        public void ChangeShowProgress(long decalUniqueKey, float progress)
        {
            if (decalSystem.instantiateMaterial)
            {
                if (decalMaterials.TryGetValue(decalUniqueKey, out var mat))
                {
                    mat.SetFloat(_ShowProgressY, progress);
                }
            }
            else
            {
                decalSmr.GetPropertyBlock(m_mpb);
                m_mpb.SetFloat(_ShowProgressY, progress);
                decalSmr.SetPropertyBlock(m_mpb);
            }
        }

        public void ChangeShowProgress(float progress)
        {
            ChangeShowProgress(currentDecalUniqueKey, progress);
        }

        // 改变溶解度
        private void ChangeDissolve(long decalUniqueKey, float dissolve)
        {
            if (decalSystem.instantiateMaterial)
            {
                if (decalMaterials.TryGetValue(decalUniqueKey, out var mat))
                {
                    mat.SetFloat(_DissolveCutoff, dissolve);
                }
            }
            else
            {
                decalSmr.GetPropertyBlock(m_mpb);
                m_mpb.SetFloat(_DissolveCutoff, dissolve);
                decalSmr.SetPropertyBlock(m_mpb);
            }
        }
        #endregion // 修改材质参数
        
        
        #region 贴花的生命周期都结束后，开始尝试回收自己
        private void LateUpdate()
        {
            if (m_decalLifeCoroutines.Count == 0 && m_destroySelfCoroutine == null)
            {
                m_destroySelfCoroutine = StartCoroutine(OnDestroySelf());
            }
        }

        private IEnumerator OnDestroySelf()
        {
            yield return YieldInstructionPool.GetWaitForSeconds(decalSystem.waitTimeout);
            Destroy(this.gameObject);
        }

        private void StopDestroySelfCoroutine()
        {
            if (m_destroySelfCoroutine != null)
            {
                StopCoroutine(m_destroySelfCoroutine);
                m_destroySelfCoroutine = null;
            }
        }
        #endregion // 贴花的生命周期都结束后，开始尝试回收自己
        
        
        /// <summary>
        /// 评估当前贴花世界空间命中点
        /// </summary>
        public bool EvaluateCurrentDecalWorldSpaceHitPoint(Vector3 origin, out Vector3 worldSpaceHitPoint)
        {
            if (!possibleDecalCenterPoints.TryGetValue(currentDecalUniqueKey, out List<Vector3> pointList))
            {
                worldSpaceHitPoint = Vector3.zero;
                return false;
            }
            
            //Debug.LogError($"可能的点: {pointList.Count}");
            
            // 找到最近和最远的点
            float minDis = float.MaxValue;
            float maxDis = float.MinValue;
            foreach (var itemPoint in pointList)
            {
                float dis = Vector3.Distance(origin, itemPoint);
                if (minDis > dis) minDis = dis;
                if (maxDis < dis) maxDis = dis;
            }

            // 排除那些距离过远的点
            float discardDis = (maxDis - minDis) * 0.5f;
            for (int i = pointList.Count - 1; i >= 0; i--)
            {
                var itemPoint = pointList[i];
                float dis = Vector3.Distance(origin, itemPoint);
                if (dis - minDis > discardDis)
                {
                    pointList.RemoveAt(i);
                }
            }
            
            // 在剩余的点里求质心
            Vector3 sumPoint = Vector3.zero;
            foreach (var itemPoint in pointList)
            {
                sumPoint += itemPoint;
            }
            Vector3 centroidPoint = sumPoint / pointList.Count;

            worldSpaceHitPoint = centroidPoint;
            return true;
        }
        
    }
}