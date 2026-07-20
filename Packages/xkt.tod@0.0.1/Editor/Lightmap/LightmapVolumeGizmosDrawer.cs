// Created By: WangYu  Date: 2025-04-01

using System.IO;
using UnityEditor;
using UnityEngine;

namespace XKT.TOD.Lightmap
{
    class LightmapVolumeGizmosDrawer
    {
        internal static readonly Color s_volumeColor = new(0.2f, 0.8f, 0.1f, 0.125f);
        
        [DrawGizmo(GizmoType.Active | GizmoType.Selected | GizmoType.NonSelected)]
        static void OnDrawGizmos(ILightmapVolume scr, GizmoType gizmoType)
        {
            if (scr is not MonoBehaviour monoBehaviour) return;
            if (!monoBehaviour.enabled) return;

            if (!scr.VolumeCollider || !scr.VolumeCollider.enabled) return;

            Gizmos.color = s_volumeColor;
            
            // 存储 lossyScale 的计算
            var lossyScale = monoBehaviour.transform.lossyScale;
            Gizmos.matrix = Matrix4x4.TRS(monoBehaviour.transform.position, monoBehaviour.transform.rotation, lossyScale);
            
            switch (scr.VolumeCollider)
            {
                case BoxCollider coll:
                {
                    if (LightmapVolumeEditor.s_drawWireFrame)
                    {
                        Gizmos.DrawWireCube(coll.center, coll.size);
                    }

                    if (LightmapVolumeEditor.s_drawSolid)
                    {
                        Gizmos.DrawCube(coll.center, coll.size);
                    }
                }
                    break;

                case SphereCollider coll:
                {
                    Matrix4x4 oldMatrix = Gizmos.matrix;
                    // For sphere the only scale that is used is the transform.x
                    Gizmos.matrix = Matrix4x4.TRS(monoBehaviour.transform.position, monoBehaviour.transform.rotation, Vector3.one * lossyScale.x);

                    if (LightmapVolumeEditor.s_drawWireFrame)
                    {
                        Gizmos.DrawWireSphere(coll.center, coll.radius);
                    }

                    if (LightmapVolumeEditor.s_drawSolid)
                    {
                        Gizmos.DrawSphere(coll.center, coll.radius);
                    }

                    Gizmos.matrix = oldMatrix;
                }
                    break;
            }
            
            // 将 icon 图标移动到 Gizmos 目录下
            string gizmosFolder = Path.Combine(Application.dataPath, "Gizmos");
            if (!Directory.Exists(gizmosFolder))
            {
                Directory.CreateDirectory(gizmosFolder);
            }
            
            string iconName = Path.GetFileName(LightmapVolume.c_iconPath);
            string iconDestPath = Path.Combine(gizmosFolder, iconName);
            if (!File.Exists(iconDestPath))
            {
                File.Copy(LightmapVolume.c_iconPath, iconDestPath);
                AssetDatabase.Refresh();
            }
        }

    }
}