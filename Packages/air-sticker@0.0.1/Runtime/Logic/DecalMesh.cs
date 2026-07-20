using System;
using System.Collections.Generic;
using AirSticker.Runtime.Core;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace AirSticker.Runtime.Logic
{
    /// <summary>
    /// 贴花网格
    /// 它是由 AirStickerProjector 创建的实例。
    /// </summary>
    public sealed class DecalMesh
    {
        private readonly GameObject _receiverObject;
        private readonly Material _decalMaterial;
        private readonly Component _receiverComponent;
        
        private readonly Matrix4x4[] _bindPoses; // SkinnedMeshRenderer 才有骨骼
        
        // 网格和渲染器
        private Dictionary<long, Mesh> m_meshDict = new();
        private Dictionary<long, IDecalMeshRenderer> m_rendererDict = new();
        
        // 为了避免异步冲突而存在的 buffer key 队列
        private List<long> m_bufferKeyList = new();
        // 分批次存储 mesh 数据
        private SortedDictionary<long, List<Vector3>> _positionBufferSD = new();
        private SortedDictionary<long, List<Vector3>> _normalBufferSD = new();
        private SortedDictionary<long, List<Vector2>> _uvBufferSD = new();
        private SortedDictionary<long, List<int>> _indexBufferSD = new();
        private SortedDictionary<long, List<BoneWeight>> _boneWeightsBufferSD = new();

        public Component ReceiverComponent => _receiverComponent;
        public IReadOnlyDictionary<long, List<Vector3>> PositionBufferSD => _positionBufferSD;
        public IReadOnlyDictionary<long, List<Vector3>> NormalBufferSD => _normalBufferSD;
        
        /// <summary>
        /// 投影矩阵
        /// </summary>
        public Matrix4x4 ProjectionMatrix { get; set; } = Matrix4x4.identity;
        
        private static readonly int _PM = Shader.PropertyToID("_PM"); // 传递投影矩阵的属性
        
        // 以自增的方式产生的唯一键
        private long m_uniqueKeyCounter;

        private long NewDecalUniqueKey()
        {
            m_uniqueKeyCounter++;
            return m_uniqueKeyCounter;
        }

        /// <summary>
        /// 当前的唯一键
        /// </summary>
        public long currentDecalUniqueKey;
        
        // 生命周期配置
        private Dictionary<long, AbsDecalConfig> m_lifeConfigs = new();
        
        
        public DecalMesh(GameObject receiverObject, Material decalMaterial, Component receiverComponent)
        {
            _receiverObject = receiverObject;
            _decalMaterial = decalMaterial;
            _receiverComponent = receiverComponent;

            if (_receiverComponent is SkinnedMeshRenderer smr)
            {
                _bindPoses = smr.sharedMesh.bindposes;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            ClearMeshBuffer();
            
            m_lifeConfigs.Clear();

            foreach (var iter in m_rendererDict)
            {
                iter.Value?.ReleaseRendering();
            }
            m_rendererDict.Clear();
                
            foreach (var iter in m_meshDict)
            {
                if (iter.Value != null) UnityObject.Destroy(iter.Value);
            }
            m_meshDict.Clear();
        }
        
        // 清理 mesh 相关的缓冲区
        private void ClearMeshBuffer()
        {
            m_bufferKeyList.Clear();
            
            _positionBufferSD.ClearDictionaryAndInternalList();
            _normalBufferSD.ClearDictionaryAndInternalList();
            _uvBufferSD.ClearDictionaryAndInternalList();
            _indexBufferSD.ClearDictionaryAndInternalList();
            _boneWeightsBufferSD.ClearDictionaryAndInternalList();
        }
        
        /// <summary>
        /// 检查贴花网格是否可以从池中删除。
        /// 如果此函数返回 true，则会将其从池中删除。
        /// </summary>
        public bool CanRemoveFromPool()
        {
            return !_receiverObject || !_decalMaterial || !_receiverComponent;
        }

        /// <summary>
        /// 将最终要显示的三角面多边形信息添加到 DecalMesh 的缓冲区中等待使用
        /// </summary>
        public void AddTrianglePolygonsToDecalMesh(
            List<ConvexPolygon> convexPolygons,
            Vector3 decalSpaceOriginPosInWorldSpace, Vector3 decalSpaceTangentInWorldSpace, Vector3 decalSpaceBiNormalInWorldSpace,
            float decalSpaceWidth, float decalSpaceHeight, float zOffsetInDecalSpace,
            AbsDecalConfig lifeConfig
        )
        {
            if (!_receiverComponent) return;
            
            // 为新的贴花创建新的唯一键
            currentDecalUniqueKey = NewDecalUniqueKey();
            m_bufferKeyList.Add(currentDecalUniqueKey);
            
            _positionBufferSD.Add(currentDecalUniqueKey, new List<Vector3>());
            _normalBufferSD.Add(currentDecalUniqueKey, new List<Vector3>());
            _uvBufferSD.Add(currentDecalUniqueKey, new List<Vector2>());
            _indexBufferSD.Add(currentDecalUniqueKey, new List<int>());
            _boneWeightsBufferSD.Add(currentDecalUniqueKey, new List<BoneWeight>());
            
            m_lifeConfigs.Add(currentDecalUniqueKey, lifeConfig);
            
            int indexBase = 0;
            foreach (var convexPolygon in convexPolygons)
            {
                if (convexPolygon.ReceiverComponent != _receiverComponent) continue;
                
                int numVertex = convexPolygon.VertexCount;
                for (int localVertNo = 0; localVertNo < numVertex; localVertNo++)
                {
                    int vertNo = convexPolygon.GetRealVertexNo(localVertNo);
                    Vector3 vertPos = convexPolygon.GetVertexPositionInWorldSpace(vertNo);

                    Vector3 decalSpaceToVertPos = vertPos - decalSpaceOriginPosInWorldSpace;

                    Vector2 uv = Vector2.zero;
                    uv.x = Vector3.Dot(decalSpaceTangentInWorldSpace, decalSpaceToVertPos) / decalSpaceWidth + 0.5f;
                    uv.y = Vector3.Dot(decalSpaceBiNormalInWorldSpace, decalSpaceToVertPos) / decalSpaceHeight + 0.5f;
                    _uvBufferSD[currentDecalUniqueKey].Add(uv);

                    // 将位置和旋转转换为父空间。
                    vertPos = convexPolygon.GetVertexPositionInModelSpace(vertNo);
                    Vector3 normal = convexPolygon.GetVertexNormalInModelSpace(vertNo);
                    BoneWeight boneWeight = convexPolygon.GetVertexBoneWeight(vertNo);

                    // 在贴花投影的相反方向上添加轻微的偏移，以避免 Z 冲突。
                    // TODO: 此数字可以稍后调整。
                    vertPos += normal * zOffsetInDecalSpace;
                    
                    _positionBufferSD[currentDecalUniqueKey].Add(vertPos);
                    _normalBufferSD[currentDecalUniqueKey].Add(normal);
                    _boneWeightsBufferSD[currentDecalUniqueKey].Add(boneWeight);
                }

                // 凸多边形由 (顶点数 - 2) 个三角形 构成。
                int numTriangle = numVertex - 2;
                for (int triNo = 0; triNo < numTriangle; triNo++)
                {
                    _indexBufferSD[currentDecalUniqueKey].Add(indexBase);
                    _indexBufferSD[currentDecalUniqueKey].Add(indexBase + triNo + 1);
                    _indexBufferSD[currentDecalUniqueKey].Add(indexBase + triNo + 2);
                }

                indexBase += numVertex;
            }
        }
        
        /// <summary>
        /// 工作线程执行完后，把缓冲区中的三角面多边形数据更新显示出来<br/>
        /// 1. 创建贴花网格。<br/>
        /// 2. 创建贴花网格渲染器。<br/>
        /// </summary>
        public void ExecutePostProcessingAfterWorkerThread(AbsDecalConfig mdConfig, int order)
        {
            UpdateMesh();
            
            foreach (var iter in m_meshDict)
            {
                long decalUniqueKey = iter.Key;
                Mesh mesh = iter.Value;
                if (mesh.vertexCount == 0)
                {
                    continue;
                }

                if (!m_rendererDict.TryGetValue(decalUniqueKey, out IDecalMeshRenderer dmr))
                {
                    // 每个贴花渲染器，有自己的克隆材质
                    Material cloneMaterial = UnityObject.Instantiate(_decalMaterial);
                    // 显示排序
                    cloneMaterial.renderQueue += order;
                    // 设置投影矩阵
                    cloneMaterial.SetMatrix(_PM, ProjectionMatrix);
                    
                    dmr = DecalRendererFactory.Create(_receiverComponent, cloneMaterial, mesh, mdConfig, decalUniqueKey);
                    m_rendererDict.Add(decalUniqueKey, dmr);
                }

                if (m_lifeConfigs.TryGetValue(decalUniqueKey, out AbsDecalConfig lifeConfig))
                {
                    dmr.CreateLifecycle(decalUniqueKey, lifeConfig, RemoveDecal);
                }
            }
            
            m_lifeConfigs.Clear(); // 值传上去了，就没用处了
        }

        private void UpdateMesh()
        {
            for (int i = m_bufferKeyList.Count - 1; i >= 0; i--)
            {
                long decalUniqueKey = m_bufferKeyList[i];

                if (!m_meshDict.TryGetValue(decalUniqueKey, out Mesh mesh))
                {
                    mesh = new Mesh();
                    m_meshDict[decalUniqueKey] = mesh;
                }

                mesh.Clear();

                if (_positionBufferSD.TryGetValue(decalUniqueKey, out var vertices))
                {
                    mesh.SetVertices(vertices);
                }

                if (_indexBufferSD.TryGetValue(decalUniqueKey, out var indices))
                {
                    mesh.SetIndices(indices, MeshTopology.Triangles, 0);
                }

                if (_normalBufferSD.TryGetValue(decalUniqueKey, out var normals))
                {
                    mesh.SetNormals(normals);
                }

                // 蒙皮网格
                if (_bindPoses != null && _bindPoses.Length > 0)
                {
                    if (_boneWeightsBufferSD.TryGetValue(decalUniqueKey, out var boneWeights))
                    {
                        mesh.boneWeights = boneWeights.ToArray();
                    }

                    mesh.bindposes = _bindPoses;
                }
                
                if (_uvBufferSD.TryGetValue(decalUniqueKey, out var uvs))
                {
                    mesh.SetUVs(0, uvs);
                }

                mesh.RecalculateTangents(); // 我们自己没有准备切线，所以让 Unity 自己算1个
                mesh.RecalculateBounds(); // 重新计算边界
                mesh.Optimize(); // 优化网格，提升运行时性能
            }
        }

        private void RemoveDecal(long decalUniqueKey)
        {
            if (m_bufferKeyList == null 
                || _positionBufferSD == null || _normalBufferSD == null || _uvBufferSD == null || _indexBufferSD == null || _boneWeightsBufferSD == null 
                || m_meshDict == null || m_rendererDict == null)
            {
                // TODO: 这些对象有空指针的情况，但暂还没有定位到原因，后续需要更仔细的排查
                Debug.LogError("Mesh 贴花似乎没能正常回收，注意观察，后续有时间再仔细排查。");
                return;
            }
            
            m_bufferKeyList.Remove(decalUniqueKey);
            // 移除 mesh 数据
            _positionBufferSD.ClearListFromDictionary(decalUniqueKey);
            _normalBufferSD.ClearListFromDictionary(decalUniqueKey);
            _uvBufferSD.ClearListFromDictionary(decalUniqueKey);
            _indexBufferSD.ClearListFromDictionary(decalUniqueKey);
            _boneWeightsBufferSD.ClearListFromDictionary(decalUniqueKey);
            
            m_meshDict.Remove(decalUniqueKey);
            m_rendererDict.Remove(decalUniqueKey);
            
            UpdateMesh();
        }

    }
}
