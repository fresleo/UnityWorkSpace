using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Garena.TA.VolumetricLightingFog
{
    public static class ShadowUtils
    {
        public static void CalculateCubeViewProjMatrix(Transform lightTransform, List<Vector3> corners, out Matrix4x4 viewMat, out Matrix4x4 projMat, float nearOffset = 0)
        {
            BoundingSphere sphereBound = new BoundingSphere();
            sphereBound.RecalculateSmallestBoundingSphere(corners);
            //
            Transform lt = lightTransform;
            Vector3 axisX = lt.TransformDirection(new Vector3(1, 0, 0));
            Vector3 axisY = lt.TransformDirection(new Vector3(0, 1, 0));
            Vector3 axisZ = lt.TransformDirection(new Vector3(0, 0, 1));
            nearOffset = Mathf.Min(0, nearOffset);
            Vector3 pos = sphereBound.position - axisZ * sphereBound.radius + axisZ * nearOffset;
            //light->world
            Matrix4x4 l2w = new Matrix4x4(
                new Vector4(axisX.x, axisX.y, axisX.z, 0),
                new Vector4(axisY.x, axisY.y, axisY.z, 0),
                new Vector4(axisZ.x, axisZ.y, axisZ.z, 0),
                new Vector4(pos.x, pos.y, pos.z, 1)
                );

            Bounds b = new Bounds();
            //world->light
            Matrix4x4 w2l = l2w.inverse;
            for (int i = 0; i < corners.Count; ++i)
            {
                var p = w2l.MultiplyPoint(corners[i]);
                b.Encapsulate(p);
            }

            //light space的中心点修正为Bounds的中心,z值还需要后退extent.z的距离
            Vector4 col3 = w2l.GetColumn(3);
            col3 = new Vector4(col3.x - b.center.x, col3.y - b.center.y, col3.z - b.center.z + b.extents.z, col3.w);

            w2l.SetColumn(3, col3);
            //Matrix4x4 projM = Matrix4x4.Ortho(-b.extents.x, b.extents.x, -b.extents.y, b.extents.y, b.extents.z * 0.1f, b.extents.z * 2.2f);
            Matrix4x4 projM = Matrix4x4.Ortho(-b.extents.x, b.extents.x, -b.extents.y, b.extents.y, 0, b.extents.z * 2f);

            //left hand->right hand
            Matrix4x4 l2r = new Matrix4x4(
                new Vector4(1, 0, 0, 0),
                new Vector4(0, 1, 0, 0),
                new Vector4(0, 0, -1, 0),
                new Vector4(0, 0, 0, 1)
                );
            Matrix4x4 viewM = l2r * w2l;
            //
            viewMat = viewM;
            projMat = projM;
        }

        /// <summary>
        /// 计算灯光相机的view和proj矩阵
        /// 这里是以opengl为标准
        /// </summary>
        /// <param name="lightTransform">灯光的Transform组件</param>
        /// <param name="shadowDist">阴影距离</param>
        /// <param name="camera">主相机</param>
        /// <param name="viewMat">返回值</param>
        /// <param name="projMat">返回值</param>
        /// <param name="nearOffset">灯光相机的near的向后偏移值</param>
        public static void CalculateShadowCameraViewProjMatrixStable(Transform lightTransform, float shadowDist, Camera camera,
            out Matrix4x4 viewMat, out Matrix4x4 projMat, float nearOffset = 0)
        {
            float dist = shadowDist;
            Vector3[] near = new Vector3[4];
            Vector3[] far = new Vector3[4];
            camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camera.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, near);
            camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), dist, Camera.MonoOrStereoscopicEye.Mono, far);

            for (int i = 0; i < near.Length; ++i)
            {
                near[i] = camera.transform.TransformPoint(near[i]);
                far[i] = camera.transform.TransformPoint(far[i]);
            }
            Vector3 pos = camera.transform.position + camera.transform.forward * dist * 0.5f;
            BoundingSphere sphereBound = new BoundingSphere();
            List<Vector3> list = new List<Vector3>();
            list.AddRange(near);
            list.AddRange(far);
            sphereBound.RecalculateSmallestBoundingSphere(list);
            //
            Transform lt = lightTransform;
            Vector3 axisX = lt.TransformDirection(new Vector3(1, 0, 0));
            Vector3 axisY = lt.TransformDirection(new Vector3(0, 1, 0));
            Vector3 axisZ = lt.TransformDirection(new Vector3(0, 0, 1));
            nearOffset = Mathf.Min(0, nearOffset);
            pos = sphereBound.position - axisZ * sphereBound.radius + axisZ * nearOffset;
            //light->world
            Matrix4x4 l2w = new Matrix4x4(
                new Vector4(axisX.x, axisX.y, axisX.z, 0),
                new Vector4(axisY.x, axisY.y, axisY.z, 0),
                new Vector4(axisZ.x, axisZ.y, axisZ.z, 0),
                new Vector4(pos.x, pos.y, pos.z, 1)
                );

            Bounds b = new Bounds();
            //world->light
            Matrix4x4 w2l = l2w.inverse;
            for (int i = 0; i < list.Count; ++i)
            {
                list[i] = w2l.MultiplyPoint(list[i]);
                b.Encapsulate(list[i]);
            }

            //light space的中心点修正为Bounds的中心,z值还需要后退extent.z的距离
            Vector4 col3 = w2l.GetColumn(3);
            col3 = new Vector4(col3.x - b.center.x, col3.y - b.center.y, col3.z - b.center.z + b.extents.z, col3.w);

            w2l.SetColumn(3, col3);
            //Matrix4x4 projM = Matrix4x4.Ortho(-b.extents.x, b.extents.x, -b.extents.y, b.extents.y, b.extents.z * 0.1f, b.extents.z * 2.2f);
            Matrix4x4 projM = Matrix4x4.Ortho(-b.extents.x, b.extents.x, -b.extents.y, b.extents.y, 0, b.extents.z * 2f);

            //left hand->right hand
            Matrix4x4 l2r = new Matrix4x4(
                new Vector4(1, 0, 0, 0),
                new Vector4(0, 1, 0, 0),
                new Vector4(0, 0, -1, 0),
                new Vector4(0, 0, 0, 1)
                );
            Matrix4x4 viewM = l2r * w2l;
            //
            viewMat = viewM;
            projMat = projM;
        }

    }
}