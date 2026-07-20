using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Garena.TA
{
    public class GPerObjectShadowUtils
    {
        
        /// <summary>
        /// 计算物体的包围盒
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        public static Bounds GetBounds(GameObject go)
        {
            Bounds result = new Bounds();
            var arr = go.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < arr.Length; i++)
            {
                if (i == 0)
                    result = arr[i].bounds;
                else
                    result.Encapsulate(arr[i].bounds);
            }
            return result;
        }

        /// <summary>
        /// 包围盒空间变换
        /// </summary>
        /// <param name="b"></param>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static Bounds TransformBounds(Bounds b, Matrix4x4 mat)
        {
            Vector3[] arr = GetBoundsCorner(b);

            Vector3 min = Vector3.zero;
            Vector3 max = Vector3.zero;
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = mat.MultiplyPoint3x4(arr[i]);

                if (i == 0)
                {
                    min = arr[i];
                    max = arr[i];
                }
                else
                {
                    VectorMin(ref min, arr[i]);
                    VectorMax(ref max, arr[i]);
                }
            }

            return new Bounds((min + max) / 2, max - min);
        }
        
        /// <summary>将获取Bound坐标的Array提出来，减少GC，该列表目前只在TransformBounds()方法直接使用</summary>
        private static Vector3[] m_boundsCorner = new Vector3[8];

        /// <summary>
        /// 获取包围盒顶点坐标
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Vector3[] GetBoundsCorner(Bounds b)
        {
            Vector3[] r = m_boundsCorner;
            Vector3 max = b.max, min = b.min;

            r[0] = min;
            r[1] = new Vector3(min.x, min.y, max.z);
            r[2] = new Vector3(min.x, max.y, max.z);
            r[3] = new Vector3(min.x, max.y, min.z);

            r[4] = new Vector3(max.x, min.y, min.z);
            r[5] = new Vector3(max.x, min.y, max.z);
            r[6] = max;
            r[7] = new Vector3(max.x, max.y, min.z);

            return r;
        }


        public static void VectorMin(ref Vector3 src, in Vector3 vec)
        {
            src.x = Mathf.Min(src.x, vec.x);
            src.y = Mathf.Min(src.y, vec.y);
            src.z = Mathf.Min(src.z, vec.z);
        }

        public static void VectorMax(ref Vector3 src, in Vector3 vec)
        {
            src.x = Mathf.Max(src.x, vec.x);
            src.y = Mathf.Max(src.y, vec.y);
            src.z = Mathf.Max(src.z, vec.z);
        }

        /// <summary>
        /// 根据包围盒构建正交投影矩阵
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Matrix4x4 GetOrthoMat(Bounds b)
        {
            Vector3 extents = b.extents;
            Matrix4x4 ortho = Matrix4x4.Ortho(-extents.x, extents.x, -extents.y, extents.y, 0f, extents.z * 2f);
            //自定义视图矩阵没有执行z反转，相应的这里的投影矩阵也不用z反转
            ortho.m02 *= -1;
            ortho.m12 *= -1;
            ortho.m22 *= -1;
            ortho.m32 *= -1;
            return ortho;// GL.GetGPUProjectionMatrix(ortho, true);

            //Vector3 max = b.max, min = b.min;
            //Vector4 row0 = new Vector4(2 / (max.x - min.x), 0, 0, -(max.x + min.x) / (max.x - min.x));
            //Vector4 row1 = new Vector4(0, 2 / (max.y - min.y), 0, -(max.y + min.y) / (max.y - min.y));
            //Vector4 row2 = new Vector4(0, 0, -2 / (max.z - min.z), -(max.z + min.z) / (max.z - min.z));
            //Vector4 row3 = new Vector4(0, 0, 0, 1);

            //Matrix4x4 mat = Matrix4x4.identity;
            //mat.SetRow(0, row0);
            //mat.SetRow(1, row1);
            //mat.SetRow(2, row2);
            //mat.SetRow(3, row3);
            //return mat;
        }

        /// <summary>
        /// 计算世界坐标到阴影坐标矩阵
        /// </summary>
        /// <param name="proj"></param>
        /// <param name="view"></param>
        /// <returns></returns>
        public static Matrix4x4 GetShadowTransform(Matrix4x4 proj, Matrix4x4 view)
        {
            // Currently CullResults ComputeDirectionalShadowMatricesAndCullingPrimitives doesn't
            // apply z reversal to projection matrix. We need to do it manually here.
            if (SystemInfo.usesReversedZBuffer)
            {
                proj.m20 = -proj.m20;
                proj.m21 = -proj.m21;
                proj.m22 = -proj.m22;
                proj.m23 = -proj.m23;
            }

            Matrix4x4 worldToShadow = proj * view;

            //worldToShadow.m00 = (0.5f * (worldToShadow.m00 + worldToShadow.m30)); // 
            //worldToShadow.m01 = (0.5f * (worldToShadow.m01 + worldToShadow.m31)); // 
            //worldToShadow.m02 = (0.5f * (worldToShadow.m02 + worldToShadow.m32)); // 
            //worldToShadow.m03 = (0.5f * (worldToShadow.m03 + worldToShadow.m33)); // [clip->UV]
            //worldToShadow.m10 = (0.5f * (worldToShadow.m10 + worldToShadow.m30)); // 
            //worldToShadow.m11 = (0.5f * (worldToShadow.m11 + worldToShadow.m31)); // 
            //worldToShadow.m12 = (0.5f * (worldToShadow.m12 + worldToShadow.m32)); // 
            //worldToShadow.m13 = (0.5f * (worldToShadow.m13 + worldToShadow.m33)); // 

            var textureScaleAndBias = Matrix4x4.identity;
            textureScaleAndBias.m00 = 0.5f;
            textureScaleAndBias.m11 = 0.5f;
            textureScaleAndBias.m22 = 0.5f;
            textureScaleAndBias.m03 = 0.5f;
            textureScaleAndBias.m23 = 0.5f;
            textureScaleAndBias.m13 = 0.5f;
            // textureScaleAndBias maps texture space coordinates from [-1,1] to [0,1]




            //Matrix4x4.Translate(new Vector3(data.sliceUVOffsetExtend.x, data.sliceUVOffsetExtend.y, 0)) * Matrix4x4.Scale(new Vector3(sliceScale.x, sliceScale.y, 1))

            // Apply texture scale and offset to save a MAD in shader.
            return (textureScaleAndBias * worldToShadow);
        }

        static Bounds GetFrustumBounds(Camera cam, Matrix4x4 mat)
        {
            Vector3[] frustumCornersNear = new Vector3[4];
            cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCornersNear);

            Vector3[] frustumCornersFar = new Vector3[4];
            cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCornersFar);

            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (int i = 0; i < frustumCornersNear.Length; i++)
            {
                var worldSpaceCorner = cam.transform.position + cam.transform.TransformVector(frustumCornersNear[i]);
                Vector3 pos = mat.MultiplyPoint3x4(worldSpaceCorner);

                VectorMin(ref min, pos);
                VectorMax(ref max, pos);
            }

            for (int i = 0; i < frustumCornersFar.Length; i++)
            {

                var worldSpaceCorner = cam.transform.position + cam.transform.TransformVector(frustumCornersFar[i]);
                Vector3 pos = mat.MultiplyPoint3x4(worldSpaceCorner);

                VectorMin(ref min, pos);
                VectorMax(ref max, pos);
            }

            return new Bounds() { min = min, max = max };
        }

        /// <summary>
        /// 计算视图和投影矩阵
        /// </summary>
        public static void GetLightMatrix(Camera cam, Quaternion lightRotation, Bounds boundsWS, bool percentEx, Vector3 boundEx, out Matrix4x4 viewMat, out Matrix4x4 projMat, out Matrix4x4 shadowMeshLocalToWorld)
        {
            Matrix4x4 worldToShadowViewTemp = Matrix4x4.Rotate(Quaternion.Inverse(lightRotation)) * Matrix4x4.Translate(-boundsWS.center);
            //Matrix4x4.TRS(-boundsWS.center, Quaternion.Inverse(lightRotation), Vector3.one);

            Bounds viewBounds = TransformBounds(boundsWS, worldToShadowViewTemp);

            //视锥交集
            var frustumBounds = GetFrustumBounds(cam, worldToShadowViewTemp);
            viewBounds = new Bounds() { min = Vector3.Max(frustumBounds.min, viewBounds.min), max = Vector3.Min(frustumBounds.max, viewBounds.max) };

             if (percentEx)
                 boundEx = Vector3.Scale(viewBounds.size, boundEx);

             viewBounds.center += new Vector3(0, 0, boundEx.z / 2);
             viewBounds.size += boundEx;

            DebugBounds(viewBounds, worldToShadowViewTemp.inverse, Color.red);

            Vector3 shadowCameraPos = worldToShadowViewTemp.inverse.MultiplyPoint3x4(viewBounds.center + viewBounds.extents.z * Vector3.back);
            Vector3 look = worldToShadowViewTemp.inverse.MultiplyPoint3x4(viewBounds.center + viewBounds.extents.z * Vector3.forward);

            Debug.DrawLine(shadowCameraPos, shadowCameraPos + Vector3.up, Color.blue);

            viewMat = Matrix4x4.Rotate(Quaternion.Inverse(lightRotation)) * Matrix4x4.Translate(-shadowCameraPos);

            projMat = GetOrthoMat(viewBounds);

            shadowMeshLocalToWorld = Matrix4x4.TRS(
                worldToShadowViewTemp.inverse.MultiplyPoint3x4(viewBounds.center),
                lightRotation,
                viewBounds.size
                );

            //shadowMeshLocalToWorld = Matrix4x4.TRS(
            //    new Vector3(2, 0, -4),
            //    Quaternion.identity,
            //    Vector3.one * 6
            //    );

            //if (cam == null)
            //{
            //    var go = new GameObject("Shadow Cam Test");
            //    cam = go.AddComponent<Camera>();
            //    cam.orthographic = true;
            //    go.gameObject.SetActive(false);
            //}

            //cam.transform.position = shadowCameraPos;
            //cam.transform.forward = look - shadowCameraPos;
            //cam.orthographicSize = viewBounds.extents.y;
            //cam.nearClipPlane = 0;
            //cam.farClipPlane = viewBounds.size.z;
            //cam.aspect = viewBounds.extents.x / viewBounds.extents.y;

            //var vmat = cam.worldToCameraMatrix;
            //var pmat = GL.GetGPUProjectionMatrix(cam.projectionMatrix, false);

            //viewMat = vmat;
            //projMat = pmat;
        }

        static void DebugBounds(Bounds b, Matrix4x4 mat, Color color)
        {
            var arr = GetBoundsCorner(b);

            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = mat.MultiplyPoint3x4(arr[i]);
            }

            Debug.DrawLine(arr[0], arr[3], color);
            Debug.DrawLine(arr[4], arr[7], color);
            for (int i = 0; i < arr.Length; i++)
            {
                if (i != 0 && i != 4)
                {
                    Debug.DrawLine(arr[i], arr[i - 1], color);
                }

                if (i < 4)
                {
                    Debug.DrawLine(arr[i], arr[i + 4], color);
                }
            }
        }

        public static bool InFrustum(Plane[] planes, Bounds b)
        {
            Vector3[] list = GetBoundsCorner(b);

            foreach (var plane in planes)
            {
                bool cull = true;
                foreach (var point in list)
                {
                    if (plane.GetSide(point))
                        cull = false;
                }

                if (cull)
                    return false;
            }
            return true;
        }


        #region LOD Util
        //https://github.com/Unity-Technologies/AutoLOD/blob/master/Scripts/Extensions/LODGroupExtensions.cs

        public static int GetMaxLOD(LODGroup lodGroup)
        {
            return lodGroup.lodCount - 1;
        }

        public static float GetWorldSpaceSize(LODGroup lodGroup)
        {
            return GetWorldSpaceScale(lodGroup.transform) * lodGroup.size;
        }

        static float GetWorldSpaceScale(Transform t)
        {
            var scale = t.lossyScale;
            float largestAxis = Mathf.Abs(scale.x);
            largestAxis = Mathf.Max(largestAxis, Mathf.Abs(scale.y));
            largestAxis = Mathf.Max(largestAxis, Mathf.Abs(scale.z));
            return largestAxis;
        }

        static float GetRelativeHeight(LODGroup lodGroup, Camera camera)
        {
            var distance = (lodGroup.transform.TransformPoint(lodGroup.localReferencePoint) - camera.transform.position).magnitude;
            return DistanceToRelativeHeight(camera, (distance / QualitySettings.lodBias), GetWorldSpaceSize(lodGroup));
        }

        static float DistanceToRelativeHeight(Camera camera, float distance, float size)
        {
            if (camera.orthographic)
                return size * 0.5F / camera.orthographicSize;

            var halfAngle = Mathf.Tan(Mathf.Deg2Rad * camera.fieldOfView * 0.5F);
            var relativeHeight = size * 0.5F / (distance * halfAngle);
            return relativeHeight;
        }

        public static int GetVisibleLOD(LODGroup lodGroup, Camera camera = null)
        {
            var lods = lodGroup.GetLODs();
            var relativeHeight = GetRelativeHeight(lodGroup, camera ?? Camera.current);


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

        #endregion
    }
}
