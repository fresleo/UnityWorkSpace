using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SkinnedDecals
{
    /// <summary>
    /// 蒙皮贴花构建器
    /// </summary>
    public class SkinnedDecalBuilder : MonoBehaviour
    {
        /// <summary>
        /// 归属于的贴花系统
        /// </summary>
        public SkinnedDecalSystem decalSystem;
        
        /// <summary>
        /// 原始蒙皮网格渲染器
        /// </summary>
        public SkinnedMeshRenderer originalSmr;
        
        // debug 标记
        public bool debugBoneBounds;
        public bool debugBoneLine;

        // 原始数据
        private List<int> m_originalTriangles = new();
        private List<Vector3> m_originalVertices = new();
        private List<Vector3> m_originalNormals = new();
        private List<Vector4> m_originalTangents = new();
        private List<BoneWeight> m_originalBoneWeights = new();
        
        private Transform[] m_originalBones;
        private Vector3[] m_bonePositions;

        // 数据对应关系
        private Dictionary<int, List<int>> m_vertexToTriangles = new();
        private Dictionary<int, List<int>> m_verticesByBones = new();
        private Dictionary<int, Bounds> m_boneBounds = new();

        // 判相交的
        private Vector3 m_originalSmrPosition;
        private Quaternion m_originalSmrRotation;
        private Matrix4x4 m_originalSmrMatrix;
        private Bounds m_decalBounds;
        private Matrix4x4 m_inverseMatrix;
        
        // 快照网格
        private Mesh m_snapshotMesh;
        private List<Vector3> m_snapshotVertices = new(), m_snapshotNormals = new();

        /// <summary>
        /// 当前贴花网格的容器
        /// </summary>
        public SkinnedDecalMesh currentSkinnedDecalMesh;
        
        // SkinnedDecalMesh 的数据状态
        private int m_startVerticeCount;
        private int m_startTriangleCount;
        private int m_newTriangleIndices;
        private bool m_newGeometry;
        
        // 新的网格数据
        private List<int> m_newVerticesIndices = new();
        private Dictionary<int, int> m_alreadyAddedTriangles = new();
        private Dictionary<int, int> m_alreadyAddedVertices = new();

        // 唯一 key 计数器
        private long m_uniqueKeyCounter;
        
        
        private void OnDestroy()
        {
            m_originalTriangles.Clear();
            m_originalVertices.Clear();
            m_originalNormals.Clear();
            m_originalTangents.Clear();
            m_originalBoneWeights.Clear();
            m_originalBones = null;
            m_bonePositions = null;

            DataUtils.ClearDictionaryAndInternalList(m_vertexToTriangles);
            DataUtils.ClearDictionaryAndInternalList(m_verticesByBones);
            m_boneBounds.Clear();

            if (m_snapshotMesh != null)
            {
                Destroy(m_snapshotMesh);
                m_snapshotMesh = null;
            }
            m_snapshotVertices.Clear();
            m_snapshotNormals.Clear();
            
            m_newVerticesIndices.Clear();
            m_alreadyAddedTriangles.Clear();
            m_alreadyAddedVertices.Clear();
        }

        private void Start() {}

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize(SkinnedDecalSystem sds)
        {
            decalSystem = sds;
            
            // 获取同级的蒙皮网格渲染器
            originalSmr = GetComponent<SkinnedMeshRenderer>();
            
            // 获取原始数据
            Mesh originalMesh = originalSmr.sharedMesh;
            originalMesh.GetTriangles(m_originalTriangles, 0);
            originalMesh.GetVertices(m_originalVertices);
            originalMesh.GetNormals(m_originalNormals);
            originalMesh.GetTangents(m_originalTangents);
            originalMesh.GetBoneWeights(m_originalBoneWeights);

            m_originalBones = originalSmr.bones;
            m_bonePositions = new Vector3[m_originalBones.Length]; // 我们要的是快照时的位置，这里先完成初始化
            
            // 构建顶点对应信息的字典
            for (int i = 0; i < originalMesh.vertexCount; i++)
            {
                m_vertexToTriangles.Add(i, new List<int>());
                
                // 构建哪些顶点跟随哪些骨骼的字典
                int boneIndex0 = m_originalBoneWeights[i].boneIndex0;
                if (!m_verticesByBones.TryGetValue(boneIndex0, out List<int> boneVertices))
                {
                    boneVertices = new List<int>();
                    m_verticesByBones.Add(boneIndex0, boneVertices);
                }
                boneVertices.Add(i);
            }

            // 顶点与三角形的对应关系
            for (int t = 0; t < m_originalTriangles.Count; t += 3)
            {
                m_vertexToTriangles[m_originalTriangles[t]].Add(t);
                m_vertexToTriangles[m_originalTriangles[t + 1]].Add(t);
                m_vertexToTriangles[m_originalTriangles[t + 2]].Add(t);
            }

            // 计算骨骼的包围盒范围
            foreach (KeyValuePair<int, List<int>> kvp in m_verticesByBones)
            {
                Vector3 center = Vector3.zero;
                float minX = Mathf.Infinity, maxX = Mathf.NegativeInfinity,
                    minY = Mathf.Infinity, maxY = Mathf.NegativeInfinity,
                    minZ = Mathf.Infinity, maxZ = Mathf.NegativeInfinity;
                
                List<int> boneVertices = kvp.Value;
                for (int i = 0; i < boneVertices.Count; i++)
                {
                    if (boneVertices[i] >= m_originalVertices.Count)
                    {
                        continue;
                    }
                    Vector3 vertex = m_originalVertices[boneVertices[i]];
                    if (vertex.x < minX) minX = vertex.x;
                    if (vertex.x > maxX) maxX = vertex.x;
                    if (vertex.y < minY) minY = vertex.y;
                    if (vertex.y > maxY) maxY = vertex.y;
                    if (vertex.z < minZ) minZ = vertex.z;
                    if (vertex.z > maxZ) maxZ = vertex.z;

                    center += originalSmr.transform.TransformPoint(vertex) - m_originalBones[kvp.Key].transform.position;
                }
                center /= boneVertices.Count;

                float sizeX = maxX - minX;
                float sizeY = maxY - minY;
                float sizeZ = maxZ - minZ;
                Vector3 size = new Vector3(sizeX, sizeY, sizeZ);
                //float maxSize = Mathf.Max(sizeX, Mathf.Max(sizeY, sizeZ));
                //Vector3 size = new Vector3(maxSize, maxSize, maxSize);
                
                m_boneBounds.Add(kvp.Key, new Bounds(center, Vector3.Scale(size, originalSmr.transform.localScale)));
            }
            
            m_snapshotMesh = new Mesh();
        }

        private void OnDrawGizmos()
        {
            // 绘制骨骼包围盒
            if (debugBoneBounds && m_boneBounds != null)
            {
                foreach (KeyValuePair<int, Bounds> kvp in m_boneBounds)
                {
                    Gizmos.DrawWireCube(m_originalBones[kvp.Key].transform.position + kvp.Value.center, kvp.Value.size);
                }
            }

            // 绘制骨骼连线
            if (debugBoneLine && m_originalBones != null && originalSmr != null)
            {
                for (int i = 0; i < m_originalBones.Length; i++)
                {
                    if (m_originalBones[i] != originalSmr.rootBone)
                    {
                        Gizmos.DrawLine(m_originalBones[i].position, m_originalBones[i].parent.position);
                    }
                }
            }
        }
        
        
        /// <summary>
        /// 判相交
        /// </summary>
        public bool Intersects(Vector3 origin, Vector3 direction, Vector3 up, float decalSizeX, float decalSizeY)
        {
            // 原始的空间位置关系
            m_originalSmrPosition = originalSmr.transform.position;
            m_originalSmrRotation = originalSmr.transform.rotation;
            m_originalSmrMatrix = new Matrix4x4();
            m_originalSmrMatrix.SetTRS(m_originalSmrPosition, m_originalSmrRotation, Vector3.one);

            // 计算世界空间的碰撞点
            float distanceToPlane = Vector3.Distance(origin, originalSmr.bounds.center);
            Vector3 worldSpaceHitPoint = origin + direction * distanceToPlane;
            
            // 创建矩阵
            Quaternion rotation = up == Vector3.zero ? Quaternion.LookRotation(direction) : Quaternion.LookRotation(direction, up);
            Matrix4x4 matrix = new Matrix4x4();
            matrix.SetTRS(origin, rotation, Vector3.one);
            m_inverseMatrix = matrix.inverse;
            
            // 贴花空间碰撞点
            Vector3 decalSpaceHitPoint = m_inverseMatrix.MultiplyPoint3x4(worldSpaceHitPoint);
            
            m_decalBounds = new Bounds(decalSpaceHitPoint, new Vector3(decalSizeX, decalSizeY, 1) + Vector3.forward * distanceToPlane * 2f);

            // 测试贴花和蒙皮网格重叠
            float maxSize = originalSmr.bounds.size.x;
            if (originalSmr.bounds.size.y > maxSize)
            {
                maxSize = originalSmr.bounds.size.y;
            }
            if (originalSmr.bounds.size.z > maxSize)
            {
                maxSize = originalSmr.bounds.size.z;
            }
            Bounds smrBounds = new Bounds(m_inverseMatrix.MultiplyPoint3x4(originalSmr.bounds.center), new Vector3(maxSize, maxSize, maxSize));
            
            bool result = smrBounds.Intersects(m_decalBounds);
            return result;
        }

        /// <summary>
        /// 拍照当前网格的快照
        /// </summary>
        public void TakeSnapshot()
        {
            // 以当前姿势拍摄网格的快照
            originalSmr.BakeMesh(m_snapshotMesh);
            m_snapshotMesh.GetVertices(m_snapshotVertices);
            m_snapshotMesh.GetNormals(m_snapshotNormals);

            // 记录当前骨骼的位置
            for (int i = 0; i < m_bonePositions.Length; i++)
            {
                m_bonePositions[i] = m_originalBones[i].position;
            }
        }

        /// <summary>
        /// 准备创建
        /// </summary>
        public long ReadyToCreate(SkinnedDecal sd, bool newBase)
        {
            if(currentSkinnedDecalMesh == null || newBase)
            {
                currentSkinnedDecalMesh = CreateNewDecalMesh(this.transform);
                currentSkinnedDecalMesh.Initialize(decalSystem, this);
            }

            long decalUniqueKey = DecalUniqueKey();

            currentSkinnedDecalMesh.currentDecalUniqueKey = decalUniqueKey;
            
            currentSkinnedDecalMesh.decalTypes.Add(decalUniqueKey, sd);

            Material decalMaterial = decalSystem.instantiateMaterial ? new Material(sd.material) : sd.material;
            currentSkinnedDecalMesh.decalMaterials.Add(decalUniqueKey, decalMaterial);
            
            return decalUniqueKey;
        }
        
        // 创建新的贴花网格
        private SkinnedDecalMesh CreateNewDecalMesh(Transform parent)
        {
            var newGo = new GameObject($"SkinnedDecalMesh ({parent.name})");
            newGo.transform.SetParent(parent, false);
            newGo.transform.localPosition = Vector3.zero;
            newGo.transform.rotation = Quaternion.identity;
            
            var sdm = newGo.AddComponent<SkinnedDecalMesh>();
            return sdm;
        }
        
        // 贴花的唯一 key
        private long DecalUniqueKey()
        {
            m_uniqueKeyCounter++;
            return m_uniqueKeyCounter;
        }
        
        /// <summary>
        /// 创建贴花
        /// </summary>
        public void CreateDecal(
            long decalUniqueKey,
            float normalClip, Vector3 origin, Vector3 direction, float decalSizeX, float decalSizeY, 
            byte atlasIndex = 0)
        {
            var currentSdm = currentSkinnedDecalMesh;
            if (currentSdm == null)
            {
                return;
            }

            m_startVerticeCount = currentSdm.allDecalVertices.Count;

            m_startTriangleCount = 0;
            foreach (var iter in currentSdm.decalTriangles)
            {
                m_startTriangleCount += iter.Value.Count;
            }
            
            m_newTriangleIndices = 0;
            
            m_newVerticesIndices.Clear();
            m_alreadyAddedTriangles.Clear();
            m_alreadyAddedVertices.Clear();
            
            var temp_decalTriangles = new List<int>();
            
            // 通过快照测试在贴花范围内的顶点，并传递三角形索引
            for (int i = 0; i < m_snapshotVertices.Count; i++)
            {
                Vector3 vertexWorldPos = m_originalSmrPosition + m_originalSmrRotation * m_snapshotVertices[i];
                Vector3 vertexDecalPos = m_inverseMatrix.MultiplyPoint3x4(vertexWorldPos);
                if (m_decalBounds.Contains(vertexDecalPos) && Vector3.Dot(-direction, m_originalSmrMatrix.MultiplyVector(m_originalNormals[i])) > normalClip)
                {
                    List<int> triangleStarts = m_vertexToTriangles[i];
                    for (int t = 0; t < triangleStarts.Count; t++)
                    {
                        int triangleStart = triangleStarts[t];
                        
                        if (m_alreadyAddedTriangles.ContainsKey(triangleStart)) continue;
                        m_alreadyAddedTriangles.Add(triangleStart, 0);

                        int originalIndex1 = m_originalTriangles[triangleStart];
                        int newIndex1;
                        if (!m_alreadyAddedVertices.TryGetValue(originalIndex1, out newIndex1))
                        {
                            newIndex1 = m_startVerticeCount + m_newVerticesIndices.Count;
                            m_alreadyAddedVertices.Add(originalIndex1, newIndex1);
                            m_newVerticesIndices.Add(originalIndex1);
                        }

                        int originalIndex2 = m_originalTriangles[triangleStart + 1];
                        int newIndex2;
                        if (!m_alreadyAddedVertices.TryGetValue(originalIndex2, out newIndex2))
                        {
                            newIndex2 = m_startVerticeCount + m_newVerticesIndices.Count;
                            m_alreadyAddedVertices.Add(originalIndex2, newIndex2);
                            m_newVerticesIndices.Add(originalIndex2);
                        }

                        int originalIndex3 = m_originalTriangles[triangleStart + 2];
                        int newIndex3;
                        if (!m_alreadyAddedVertices.TryGetValue(originalIndex3, out newIndex3))
                        {
                            newIndex3 = m_startVerticeCount + m_newVerticesIndices.Count;
                            m_alreadyAddedVertices.Add(originalIndex3, newIndex3);
                            m_newVerticesIndices.Add(originalIndex3);
                        }

                        // 添加三角形
                        temp_decalTriangles.Add(newIndex1);
                        temp_decalTriangles.Add(newIndex2);
                        temp_decalTriangles.Add(newIndex3);
                        
                        m_newTriangleIndices += 3;
                    }
                }
            }
            
            currentSdm.decalTriangles.Add(decalUniqueKey, temp_decalTriangles);

            // 如果没有新顶点，则退出
            if (m_newVerticesIndices.Count == 0)
            {
                // 因为不准备创建了，得把之前加的索引再退出来
                if (m_newTriangleIndices > 0)
                {
                    var tempList = currentSdm.decalTriangles[decalUniqueKey];
                    tempList.RemoveRange(tempList.Count - m_newTriangleIndices, m_newTriangleIndices);
                }

                m_newGeometry = false;
                return;
            }
            m_newGeometry = true;

            // 传递新 mesh 的数据
            var temp_decalVertices = new List<Vector3>();
            var temp_decalNormals = new List<Vector3>();
            var temp_decalTangents = new List<Vector4>();
            var temp_decalColors = new List<Color32>();
            var temp_decalUvs = new List<Vector2>();
            var temp_decalBoneWeights = new List<BoneWeight>();

            var temp_possibleDecalCenterPoints = new List<Vector3>();
            
            for (int i = 0; i < m_newVerticesIndices.Count; i++)
            {
                int index = m_newVerticesIndices[i];
                
                Vector3 vertex = m_originalVertices[index];
                temp_decalVertices.Add(vertex);
                
                Vector3 normal = m_originalNormals[index];
                temp_decalNormals.Add(normal);
                
                Vector4 tangent = m_originalTangents[index];
                temp_decalTangents.Add(tangent);

                // 顶点色: r = unused, g = unused, b = unused, a = atlas sheet index
                Color32 color = new Color32(0, 0, 0, atlasIndex);
                temp_decalColors.Add(color);

                // 骨骼权重
                BoneWeight boneWeight = m_originalBoneWeights[index];
                temp_decalBoneWeights.Add(boneWeight);

                // 快照中的位置
                Vector3 vertexWorldPos = m_originalSmrPosition + m_originalSmrRotation * m_snapshotVertices[index];
                Vector3 vertexDecalPos = m_inverseMatrix.MultiplyPoint3x4(vertexWorldPos);

                // 计算 uv
                Vector2 uv = new Vector2(vertexDecalPos.x * (1f / decalSizeX), vertexDecalPos.y * (1f / decalSizeY));
                
                uv.x += 0.5f;
                uv.y += 0.5f;

                if (vertexDecalPos.x < m_decalBounds.min.x)
                {
                    uv.x = 0f;
                }
                else if (vertexDecalPos.x > m_decalBounds.max.x)
                {
                    uv.x = 1f;
                }

                if (vertexDecalPos.y < m_decalBounds.min.y)
                {
                    uv.y = 0f;
                }
                else if (vertexDecalPos.y > m_decalBounds.max.y)
                {
                    uv.y = 1f;
                }
                
                temp_decalUvs.Add(uv);

                // 收集范围内最接近 UV 中心的点
                float approximately = decalSystem.usedToCollectTheApproximatelyOfTheCenterPoint;
                if(Mathf.Abs(uv.x - 0.5f) < approximately && Mathf.Abs(uv.y - 0.5f) < approximately)
                {
                    temp_possibleDecalCenterPoints.Add(vertexWorldPos);
                }
            }
            
            currentSdm.decalVertices.Add(decalUniqueKey, temp_decalVertices);
            currentSdm.decalNormals.Add(decalUniqueKey, temp_decalNormals);
            currentSdm.decalTangents.Add(decalUniqueKey, temp_decalTangents);
            currentSdm.decalColors.Add(decalUniqueKey, temp_decalColors);
            currentSdm.decalUvs.Add(decalUniqueKey, temp_decalUvs);
            currentSdm.decalBoneWeights.Add(decalUniqueKey, temp_decalBoneWeights);
            
            currentSdm.possibleDecalCenterPoints.Add(decalUniqueKey, temp_possibleDecalCenterPoints);
        }

        /// <summary>
        /// 更新网格
        /// </summary>
        public void UpdateMesh(long decalUniqueKey)
        {
            // 没新的多边形，不用更新 mesh
            if (!m_newGeometry) return;
            
            var currentSdm = currentSkinnedDecalMesh;
            if (currentSdm == null)
            {
                return;
            }
            
            // 测试总顶点数是否有超出限制
            // todo m_startVerticeCount 在第1次分家时工作肯定是正常的，但第2次分时可能会出状况，不过暂时应该碰不上那样的状况吧，大概~
            int verticeCount = m_startVerticeCount + m_newVerticesIndices.Count;
            // 65k 的限制来源于 mesh.indexFormat = IndexFormat.UInt16 ，可以改成 32 的，但旧硬件可能会有兼容性问题，所以这里用分家的方式来解决
            bool vertexLimit = verticeCount > 65000;
            
            if (vertexLimit)
            {
                // 当前因为每贴花都被分为了 SubMesh ，所以起始都是 0
                m_startTriangleCount = 0;
                
                var addedTriangles = new List<int>();
                for (int i = 0; i < m_newTriangleIndices; i++)
                {
                    int triangleIndex = currentSdm.decalTriangles[decalUniqueKey][m_startTriangleCount + i] - m_startVerticeCount;
                    addedTriangles.Add(triangleIndex);
                }

                List<Vector3> decalVertices = currentSdm.decalVertices[decalUniqueKey];
                List<Vector3> decalNormals = currentSdm.decalNormals[decalUniqueKey];
                List<Vector4> decalTangents = currentSdm.decalTangents[decalUniqueKey];
                List<Color32> decalColors = currentSdm.decalColors[decalUniqueKey];
                List<Vector2> decalUvs = currentSdm.decalUvs[decalUniqueKey];
                List<BoneWeight> decalBoneWeights = currentSdm.decalBoneWeights[decalUniqueKey];

                // 强制额外创建一个新的
                long newDecalUniqueKey = ReadyToCreate(currentSdm.decalTypes[decalUniqueKey], true);
                var newSdm = currentSkinnedDecalMesh;
                
                Debug.LogWarning($"蒙皮网格贴花，顶点总数超过 65k。 所以创建了新的 mesh 基座: {newSdm.gameObject.name}");
                
                newSdm.decalTriangles.Add(newDecalUniqueKey, addedTriangles);
                
                newSdm.decalVertices.Add(newDecalUniqueKey, decalVertices);
                newSdm.decalNormals.Add(newDecalUniqueKey, decalNormals);
                newSdm.decalTangents.Add(newDecalUniqueKey, decalTangents);
                newSdm.decalColors.Add(newDecalUniqueKey, decalColors);
                newSdm.decalUvs.Add(newDecalUniqueKey, decalUvs);
                newSdm.decalBoneWeights.Add(newDecalUniqueKey, decalBoneWeights);
                
                newSdm.UpdateMesh();
                return;
            }

            currentSdm.UpdateMesh();
        }
        
        /// <summary>
        /// 修改显示进度
        /// </summary>
        public void ChangeShowProgress(float progress)
        {
            var currentSdm = currentSkinnedDecalMesh;
            if (currentSdm == null)
            {
                return;
            }
            
            currentSdm.ChangeShowProgress(progress);
        }

        /// <summary>
        /// 评估当前贴花世界空间命中点
        /// </summary>
        public bool EvaluateCurrentDecalWorldSpaceHitPoint(Vector3 origin, out Vector3 worldSpaceHitPoint)
        {
            var currentSdm = currentSkinnedDecalMesh;
            if (currentSdm == null)
            {
                worldSpaceHitPoint = Vector3.zero;
                return false;
            }
            
            bool result = currentSdm.EvaluateCurrentDecalWorldSpaceHitPoint(origin, out worldSpaceHitPoint);
            return result;
        }
        
    }
}