
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif //UNITY_EDITOR

namespace com.xknight.mt.Lib.Runtime.MT.Utils
{
    /// <summary>
    /// LODGroup 实用工具
    /// </summary>
    public static class LODGroupUtility
    {
        /// <summary>
        /// 获取当前 LODGroup 的可见 LOD 级别
        /// </summary>
        /// <param name="lodGroup">LODGroup 对象</param>
        /// <param name="camera">当前摄像机</param>
        /// <returns>可见 LOD 级别</returns>
        public static int GetVisibleLOD(LODGroup lodGroup, Camera camera = null)
        {
            var lods = lodGroup.GetLODs();
            var relativeHeight = GetRelativeHeight(lodGroup, camera);
            
            int lodIndex = GetMaxLOD(lodGroup);
            for (var i = 0; i < lods.Length; i++)
            {
                var lod = lods[i];

                if (relativeHeight >= lod.screenRelativeTransitionHeight)
                {
                    lodIndex = i;
                    break;
                }
            }
            
            return lodIndex;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 获取当前 LODGroup 在 SceneView 中的可见 LOD 级别
        /// </summary>
        public static int GetVisibleLODSceneView(LODGroup lodGroup)
        {
            Camera camera = SceneView.lastActiveSceneView.camera;
            return GetVisibleLOD(lodGroup, camera);
        }
#endif //UNITY_EDITOR

        static float GetRelativeHeight(LODGroup lodGroup, Camera camera)
        {
            float distance = (lodGroup.transform.TransformPoint(lodGroup.localReferencePoint) - camera.transform.position).magnitude;
            float worldSpaceSize = GetWorldSpaceSize(lodGroup);
            return DistanceToRelativeHeight(camera, (distance / QualitySettings.lodBias), worldSpaceSize);
        }

        static float DistanceToRelativeHeight(Camera camera, float distance, float size)
        {
            if (camera.orthographic)
            {
                return size * 0.5F / camera.orthographicSize;
            }

            var halfAngle = Mathf.Tan(Mathf.Deg2Rad * camera.fieldOfView * 0.5F);
            var relativeHeight = size * 0.5F / (distance * halfAngle);
            return relativeHeight;
        }

        public static int GetMaxLOD(LODGroup lodGroup)
        {
            return lodGroup.lodCount - 1;
        }

        public static float GetWorldSpaceSize(LODGroup lodGroup)
        {
            return GetWorldSpaceScale(lodGroup.transform) * lodGroup.size;
        }

        public static float GetWorldSpaceScale(Transform t)
        {
            var scale = t.lossyScale;
            float largestAxis = Mathf.Abs(scale.x);
            largestAxis = Mathf.Max(largestAxis, Mathf.Abs(scale.y));
            largestAxis = Mathf.Max(largestAxis, Mathf.Abs(scale.z));
            return largestAxis;
        }
    }
}