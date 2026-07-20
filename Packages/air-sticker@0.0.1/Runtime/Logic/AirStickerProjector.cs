using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using AirSticker.Runtime.Core;
using UnityEngine;
using UnityEngine.Events;

namespace AirSticker.Runtime.Logic
{
    /// <summary>
    /// 网格贴花投射器
    /// </summary>
    public sealed partial class AirStickerProjector : MonoBehaviour
    {
        /// <summary>
        /// 状态枚举
        /// </summary>
        public enum EState
        {
            NotLaunch = 0,
            Launching,
            LaunchingCompleted,
            LaunchingCanceled
        }

        /// <summary>
        /// 剪切平面枚举
        /// </summary>
        private enum EClipPlane
        {
            Left = 0,
            Right,
            Bottom,
            Top,
            Front,
            Back,
            Num // 总数
        }

        /// <summary>
        /// 工作线程错误枚举
        /// </summary>
        private enum EWorkerThreadError
        {
            None = 0,
            NoVerticesInDecalSpace,
        }

        public const float c_defaultZOffset = 0.005f;

        // 贴花的接收器对象
        [SerializeField] public GameObject receiverGameObject;
        // 已经过滤过的接收器子组件
        [SerializeField] public MeshRenderer[] childMeshRenderers;
        [SerializeField] public MeshFilter[] childMeshFilters;
        [SerializeField] public SkinnedMeshRenderer[] childSkinnedMeshRenderers;
        [SerializeField] public Terrain[] childTerrains;
        
        // 已经预设了子组件
        private bool ChildComponentsHaveBeenPreset => 
            (childMeshRenderers != null && childMeshFilters != null)
            || childSkinnedMeshRenderers != null 
            || childTerrains != null;
        
        /// <summary>
        /// mesh 贴花的配置
        /// </summary>
        [SerializeField] public AbsDecalConfig mdConfig;


        /// <summary>
        /// 贴花的类型哈希值，方便后边对它进行池化
        /// </summary>
        public int DecalTypeHash()
        {
            if (mdConfig == null) return 0;

            int hash = mdConfig.CalculateHash();
            return hash;
        }

        /// <summary>
        /// 贴花启动完成回调
        /// </summary>
        [SerializeField] public UnityEvent<EState> onFinishedLaunch;

        /// <summary>
        /// 顺序
        /// </summary>
        [SerializeField] public int order;

        /// <summary>
        /// 贴花空间与接收器表面的 Z 偏移量
        /// </summary>
        [SerializeField] public float zOffsetInDecalSpace = c_defaultZOffset;

        // 用于剔除的剪切平面
        private readonly Vector4[] m_clipPlanes = new Vector4[(int)EClipPlane.Num];

        private List<ConvexPolygonInfo> m_broadPhaseConvexPolygonInfos = new();
        private List<ConvexPolygonInfo> m_convexPolygonInfos;

        private DecalSpace m_decalSpace;
        private bool m_executeLaunchingOnWorkerThread;
        private EWorkerThreadError m_workerThreadError;

        /// <summary>
        /// 贴花投影仪的状态
        /// </summary>
        public EState NowState { get; private set; } = EState.NotLaunch;

        /// <summary>
        /// 投影器生成的贴花网格的列表
        /// </summary>
        public List<DecalMesh> DecalMeshes { get; } = new();


        private void Start()
        {
            AirStickerSystem.EnsureSystem();
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            OnFinished(EState.LaunchingCanceled);
        }

        private void OnDrawGizmos()
        {
            if (mdConfig != null)
            {
                GizmosTools.DrawDecalBox(transform, Color.green, mdConfig.boxWidth, mdConfig.boxHeight, mdConfig.boxDepth);
            }
        }

        
        private bool CollectDecalMeshes()
        {
            if (ChildComponentsHaveBeenPreset)
            {
                if (childMeshRenderers != null && childMeshFilters != null)
                {
                    foreach (var mr in childMeshRenderers)
                    {
                        AirStickerSystem.CollectDecalMeshes(DecalMeshes, receiverGameObject, mdConfig.material, mr);
                    }
                }
                
                if (childSkinnedMeshRenderers != null)
                {
                    foreach (var smr in childSkinnedMeshRenderers)
                    {
                        AirStickerSystem.CollectDecalMeshes(DecalMeshes, receiverGameObject, mdConfig.material, smr);
                    }
                }
                
                if (childTerrains != null)
                {
                    foreach (var terr in childTerrains)
                    {
                        AirStickerSystem.CollectDecalMeshes(DecalMeshes, receiverGameObject, mdConfig.material, terr);
                    }
                }
            }
            else
            {
                // 每个渲染器会对应1个 DecalMesh 容器
                AirStickerSystem.CollectDecalMeshes(
                    DecalMeshes, receiverGameObject, mdConfig.material,
                    out childMeshRenderers, out childMeshFilters,
                    out childSkinnedMeshRenderers,
                    out childTerrains);
            }

            bool result = DecalMeshes.Count > 0;
            return result;
        }

        /// <summary>
        /// 启动投影器
        /// </summary>
        /// <remarks>
        /// 此处理是异步的，因此投影贴花需要几帧才能完成。
        /// </remarks>
        /// <param name="onFinishedLaunch">启动完成回调</param>
        /// <returns>false: 重复启动失败</returns>
        public bool Launch(UnityAction<EState> onFinishedLaunch)
        {
            if (NowState != EState.NotLaunch)
            {
                Debug.LogError("此函数只能调1次，但现在它被调用了多次。");
                return false;
            }

            NowState = EState.Launching;

            // 完成回调
            if (onFinishedLaunch != null)
            {
                this.onFinishedLaunch.AddListener(onFinishedLaunch);
            }

            // 自动回收投射器
            this.onFinishedLaunch.AddListener(result => { UnityUtils.DestroyUnityObject(this.gameObject); });

            // 请求启动贴花
            AirStickerSystem.DecalProjectorLauncher.Request(this,
                () =>
                {
                    // 接收方对象已失效，因此进程已终止。
                    if (!receiverGameObject)
                    {
                        OnFinished(EState.LaunchingCanceled);
                        return;
                    }

                    StartCoroutine(ExecuteLaunch());
                });
            return true;
        }
        
        /// <summary>
        /// 投影贴花生成到网格
        /// </summary>
        /// <remarks>
        /// 此过程在多个帧上执行。
        /// 可以使用回调函数或通过检查 IsFinishedLaunch 属性来检测投影的完成。
        /// </remarks>
        private IEnumerator ExecuteLaunch()
        {
            // 初始化贴花空间的原点轴
            var trans = transform;
            m_decalSpace = new DecalSpace(trans.right, trans.up, trans.forward * -1.0f);

            // 接收方对象已失效，所以直接就完成了
            if (!receiverGameObject)
            {
                OnFinished(EState.LaunchingCanceled);
                yield break;
            }

            bool haveDecalMeshes = CollectDecalMeshes();
            if (!haveDecalMeshes)
            {
                OnFinished(EState.LaunchingCanceled);
                yield break;
            }
            
            if (!AirStickerSystem.ReceiverObjectTrianglePolygonsPool.Contains(receiverGameObject))
            {
                m_convexPolygonInfos = new List<ConvexPolygonInfo>();

                // 从接收对象构建三角面多边形
                yield return AirStickerSystem.BuildTrianglePolygonsFromReceiverObject(
                    childMeshFilters, childMeshRenderers, childSkinnedMeshRenderers, childTerrains,
                    m_convexPolygonInfos);

                // 在系统里注册多边形对象
                AirStickerSystem.ReceiverObjectTrianglePolygonsPool.RegisterTrianglePolygons(receiverGameObject, m_convexPolygonInfos);
            }

            // 多线程加速逻辑
            PrepareToRunOnWorkerThread(receiverGameObject, childSkinnedMeshRenderers,
                out Matrix4x4[][] boneMatricesPallet, out Vector3 projectorPosition, out Vector3 centerPositionOfDecalBox);
            RunOnWorkerThread(boneMatricesPallet, projectorPosition, centerPositionOfDecalBox);

            // 等待工作线程完成
            while (m_executeLaunchingOnWorkerThread) yield return null;

            if (m_workerThreadError == EWorkerThreadError.NoVerticesInDecalSpace)
            {
                Debug.LogError("在当前贴花的 clip 空间内，没有搜索到顶点。如果你不想增加 mesh 的顶点，或是增大贴花的宽/高，就增大它的深度来增大 clip 空间的范围来解决这个问题。");
            }

            // 更新贴花的显示
            foreach (var decalMesh in DecalMeshes)
            {
                decalMesh.ExecutePostProcessingAfterWorkerThread(mdConfig, order);
            }

            // 计算贴花的表面中心点
            // Vector3 surfaceCenter = CalculateSurfaceCenter();
            
            OnFinished(EState.LaunchingCompleted);
            m_convexPolygonInfos = null;

            yield return null;
        }

        private void PrepareToRunOnWorkerThread(GameObject receiverObject, SkinnedMeshRenderer[] skinnedMeshRenderers,
            out Matrix4x4[][] boneMatricesPallet, out Vector3 projectorPosition, out Vector3 centerPositionOfDecalBox)
        {
            m_convexPolygonInfos = AirStickerSystem.GetTrianglePolygonsFromPool(receiverObject);
            boneMatricesPallet = CalculateMatricesPallet(skinnedMeshRenderers);

            var tf = transform;
            projectorPosition = tf.position;
            // 贴花 box 的中心点
            centerPositionOfDecalBox = projectorPosition + tf.forward * (mdConfig.boxDepth * 0.5f);

            for (int i = 0; i < m_convexPolygonInfos.Count; i++)
            {
                m_convexPolygonInfos[i].ConvexPolygon.PrepareToRunOnWorkerThread();
            }
        }

        // 计算骨骼矩阵的 Pallet
        private static Matrix4x4[][] CalculateMatricesPallet(SkinnedMeshRenderer[] skinnedMeshRenderers)
        {
            // 没有蒙皮网格，就不用计算骨骼矩阵了
            if (skinnedMeshRenderers == null) return null;

            var boneMatricesPallet = new Matrix4x4[skinnedMeshRenderers.Length][];

            int skinnedMeshRendererNo = 0;
            foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
            {
                if (!skinnedMeshRenderer) continue;
                if (skinnedMeshRenderer.rootBone != null)
                {
                    var mesh = skinnedMeshRenderer.sharedMesh;
                    int numBone = skinnedMeshRenderer.bones.Length;

                    var boneMatrices = new Matrix4x4[numBone];
                    for (var boneNo = 0; boneNo < numBone; boneNo++)
                    {
                        boneMatrices[boneNo] = skinnedMeshRenderer.bones[boneNo].localToWorldMatrix * mesh.bindposes[boneNo];
                    }

                    boneMatricesPallet[skinnedMeshRendererNo] = boneMatrices;
                }

                skinnedMeshRendererNo++;
            }

            return boneMatricesPallet;
        }

        private void RunOnWorkerThread(Matrix4x4[][] boneMatricesPallet, Vector3 projectorPosition, Vector3 centerPositionOfDecalBox)
        {
            m_executeLaunchingOnWorkerThread = true;
            m_workerThreadError = EWorkerThreadError.None;

            // 注意：不能在工作线程里调用它
            Matrix4x4 projectionMatrix = CalculateProjectionMatrix();
            
            ThreadPool.QueueUserWorkItem(RunActionByWorkerThread, new Action(() =>
            {
                var localToWorldMatrices = new Matrix4x4[3];
                var boneWeights = new BoneWeight[3];

                for (var i = 0; i < m_convexPolygonInfos.Count; i++)
                {
                    m_convexPolygonInfos[i].ConvexPolygon.CalculatePositionsAndNormalsInWorldSpace(
                        boneMatricesPallet, localToWorldMatrices, boneWeights);
                }

                // 筛选出通过了宽相位凸多边形检测的多边形
                bool hasVerticesInDecalSpace = BroadPhaseConvexPolygonsDetection.Execute(
                    projectorPosition, m_decalSpace.Ez,
                    mdConfig.boxWidth, mdConfig.boxHeight, mdConfig.boxDepth,
                    m_convexPolygonInfos, mdConfig.projectionBackside,
                    out m_broadPhaseConvexPolygonInfos);
                if (!hasVerticesInDecalSpace)
                {
                    m_workerThreadError = EWorkerThreadError.NoVerticesInDecalSpace;
                }

                BuildClipPlanes(centerPositionOfDecalBox);
                SplitConvexPolygonsByPlanes();
                AddTrianglePolygonsToDecalMeshFromConvexPolygons(centerPositionOfDecalBox, projectionMatrix);

                m_executeLaunchingOnWorkerThread = false;
            }));
        }
        
        private Matrix4x4 CalculateProjectionMatrix()
        {
            // 缩放矩阵，[-0.5, 0.5] -> [0, 1]
            Matrix4x4 scale = Matrix4x4.Scale(new Vector3(
                2.0f / mdConfig.boxWidth,
                2.0f / mdConfig.boxHeight,
                2.0f / mdConfig.boxDepth
            ));

            // 偏移矩阵，[-0.5, 0.5] -> [0, 1]
            Matrix4x4 offset = Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0.5f));

            return offset * scale * transform.worldToLocalMatrix;
        }

        private void RunActionByWorkerThread(object obj)
        {
            Action action = obj as Action;
            action?.Invoke();
        }

        /// <summary>
        /// 构建用于剔除的剪切平面
        /// </summary>
        private void BuildClipPlanes(Vector3 basePoint)
        {
            float basePointToNearClipDistance = mdConfig.boxDepth * 0.5f;
            float basePointToFarClipDistance = mdConfig.boxDepth * 0.5f;

            var decalSpaceTangentWs = m_decalSpace.Ex;
            var decalSpaceBiNormalWs = m_decalSpace.Ey;
            var decalSpaceNormalWs = m_decalSpace.Ez;

            // Build left plane.
            m_clipPlanes[(int)EClipPlane.Left] = new Vector4
            {
                x = decalSpaceTangentWs.x,
                y = decalSpaceTangentWs.y,
                z = decalSpaceTangentWs.z,
                w = mdConfig.boxWidth / 2.0f - Vector3.Dot(decalSpaceTangentWs, basePoint)
            };
            // Build right plane.
            m_clipPlanes[(int)EClipPlane.Right] = new Vector4
            {
                x = -decalSpaceTangentWs.x,
                y = -decalSpaceTangentWs.y,
                z = -decalSpaceTangentWs.z,
                w = mdConfig.boxWidth / 2.0f + Vector3.Dot(decalSpaceTangentWs, basePoint)
            };
            // Build bottom plane.
            m_clipPlanes[(int)EClipPlane.Bottom] = new Vector4
            {
                x = decalSpaceBiNormalWs.x,
                y = decalSpaceBiNormalWs.y,
                z = decalSpaceBiNormalWs.z,
                w = mdConfig.boxHeight / 2.0f - Vector3.Dot(decalSpaceBiNormalWs, basePoint)
            };
            // Build top plane.
            m_clipPlanes[(int)EClipPlane.Top] = new Vector4
            {
                x = -decalSpaceBiNormalWs.x,
                y = -decalSpaceBiNormalWs.y,
                z = -decalSpaceBiNormalWs.z,
                w = mdConfig.boxHeight / 2.0f + Vector3.Dot(decalSpaceBiNormalWs, basePoint)
            };
            // Build front plane.
            m_clipPlanes[(int)EClipPlane.Front] = new Vector4
            {
                x = -decalSpaceNormalWs.x,
                y = -decalSpaceNormalWs.y,
                z = -decalSpaceNormalWs.z,
                w = basePointToNearClipDistance + Vector3.Dot(decalSpaceNormalWs, basePoint)
            };
            // Build back plane.
            m_clipPlanes[(int)EClipPlane.Back] = new Vector4
            {
                x = decalSpaceNormalWs.x,
                y = decalSpaceNormalWs.y,
                z = decalSpaceNormalWs.z,
                w = basePointToFarClipDistance - Vector3.Dot(decalSpaceNormalWs, basePoint)
            };
        }

        /// <summary>
        /// 凸多边形将按剪辑平面进行分割
        /// </summary>
        private void SplitConvexPolygonsByPlanes()
        {
            foreach (var clipPlane in m_clipPlanes)
            foreach (var convexPolyInfo in m_broadPhaseConvexPolygonInfos)
            {
                if (convexPolyInfo.IsOutsideClipSpace) continue; // 在 Clip 空间外

                convexPolyInfo.ConvexPolygon.SplitAndRemoveByPlane(clipPlane, out bool isOutsideClipSpace);
                convexPolyInfo.IsOutsideClipSpace = isOutsideClipSpace;
            }
        }

        /// <summary>
        /// 把最终筛出来的多边形添加到 DecalMesh 对象里
        /// </summary>
        private void AddTrianglePolygonsToDecalMeshFromConvexPolygons(Vector3 originPosInDecalSpace, Matrix4x4 projectionMatrix)
        {
            var convexPolygons = new List<ConvexPolygon>();
            foreach (var convexPolyInfo in m_broadPhaseConvexPolygonInfos)
            {
                if (convexPolyInfo.IsOutsideClipSpace) continue; // 在 Clip 空间外

                convexPolygons.Add(convexPolyInfo.ConvexPolygon);
            }

            foreach (DecalMesh decalMesh in DecalMeshes)
            {
                decalMesh.ProjectionMatrix = projectionMatrix;
                
                decalMesh.AddTrianglePolygonsToDecalMesh(
                    convexPolygons,
                    originPosInDecalSpace, m_decalSpace.Ex, m_decalSpace.Ey,
                    mdConfig.boxWidth, mdConfig.boxHeight, zOffsetInDecalSpace,
                    mdConfig);
            }
        }

        private void OnFinished(EState finishedState)
        {
            NowState = finishedState;

            onFinishedLaunch?.Invoke(finishedState);
            onFinishedLaunch = null;
        }
        
        private Vector3 CalculateSurfaceCenter()
        {
            Vector3 center = Vector3.zero;
            int vertexCount = 0;

            // 获取投影方向
            Vector3 projectionDir = transform.forward;

            foreach (var decalMesh in DecalMeshes)
            {
                // 遍历所有贴花网格的顶点
                foreach (var key in decalMesh.PositionBufferSD.Keys)
                {
                    var positionList = decalMesh.PositionBufferSD[key];
                    var normalList = decalMesh.NormalBufferSD[key];
                    if (positionList.Count == 0) continue;

                    for (int i = 0; i < positionList.Count; i++)
                    {
                        Vector3 localPos = positionList[i];
                        Vector3 localNormal = normalList[i];
                        
                        Vector3 worldPos = decalMesh.ReceiverComponent.transform.TransformPoint(localPos);
                        Vector3 worldNormal = decalMesh.ReceiverComponent.transform.TransformDirection(localNormal).normalized;

                        // 排除与投影方向夹角超过90度的顶点
                        float dotProduct = Vector3.Dot(projectionDir, worldNormal);
                        if (dotProduct >= 0)
                        {
                            center += worldPos;
                            vertexCount++;
                        }
                    }
                }
            }

            // 计算平均位置
            if (vertexCount > 0)
            {
                center /= vertexCount;
            }
            else
            {
                // 回退到估计的中心点
                center = transform.position + transform.forward * (mdConfig.boxDepth * 0.5f);
            }

            return center;
        }

    }
}
