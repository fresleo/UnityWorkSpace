/********************************************************
 * File:    T4MBrushPainter.cs
 * Description: T4M 笔刷绘制服务
 *********************************************************/

using T4MEditor.Data;
using UnityEditor;
using UnityEngine;

namespace T4MEditor.Services
{
    /// <summary>
    /// 绘制上下文
    /// </summary>
    public struct T4MPaintContext
    {
        /// <summary>
        /// 控制图1
        /// </summary>
        public Texture2D ControlMap;

        /// <summary>
        /// 控制图2
        /// </summary>
        public Texture2D ControlMap2;

        /// <summary>
        /// UV 坐标缩放
        /// </summary>
        public float UVCoord;

        /// <summary>
        /// 当前选中的 Transform
        /// </summary>
        public Transform CurrentSelect;
    }

    /// <summary>
    /// 笔刷绘制服务，处理控制图的绘制操作
    /// </summary>
    public static class T4MBrushPainter
    {
        private const string TEXTURE_EXCEPTION_MESSAGE = "修改控制图发生异常，控制图需要满足以下条件。\nRead/Write 标志被勾选。\n不能使用任何压缩格式，如 DXT, ASTC, ETC 等。";

        private static Color[] _terrainBay2;

        /// <summary>
        /// 在控制图上绘制
        /// </summary>
        /// <param name="context">绘制上下文</param>
        /// <param name="uv">UV 坐标</param>
        /// <param name="brush">笔刷设置</param>
        /// <returns>是否成功绘制</returns>
        public static bool Paint(T4MPaintContext context, Vector2 uv, T4MBrushSettings brush)
        {
            if (context.ControlMap == null || brush == null || brush.Alpha == null)
            {
                return false;
            }

            Vector2 scaledUV = uv * context.UVCoord;

            GetTextureColorBlockSize(
                scaledUV,
                brush.SizeInPourcent,
                context.ControlMap.width,
                context.ControlMap.height,
                out int x,
                out int y,
                out int blockWidth,
                out int blockHeight
            );

            Color[] controlColorBlock = context.ControlMap.GetPixels(x, y, blockWidth, blockHeight, 0);

            if (context.ControlMap2 != null)
            {
                _terrainBay2 = context.ControlMap2.GetPixels(x, y, blockWidth, blockHeight, 0);
            }

            int texcoordX = Mathf.FloorToInt(scaledUV.x * context.ControlMap.width);
            int texcoordY = Mathf.FloorToInt(scaledUV.y * context.ControlMap.height);

            for (int i = 0; i < blockHeight; i++)
            {
                for (int j = 0; j < blockWidth; j++)
                {
                    int index = (i * blockWidth) + j;

                    float stronger = brush.Alpha[
                        Mathf.Clamp((y + i) - (texcoordY - brush.SizeInPourcent / 2), 0, brush.SizeInPourcent - 1) * brush.SizeInPourcent +
                        Mathf.Clamp((x + j) - (texcoordX - brush.SizeInPourcent / 2), 0, brush.SizeInPourcent - 1)
                    ] * brush.Strength;

                    if (brush.SelectedTexture < 3)
                    {
                        controlColorBlock[index] = Color.Lerp(controlColorBlock[index], brush.TargetColor, stronger);
                    }
                    else
                    {
                        controlColorBlock[index] = Color.Lerp(controlColorBlock[index], brush.TargetColor, stronger);
                        if (context.ControlMap2 != null)
                        {
                            _terrainBay2[index] = Color.Lerp(_terrainBay2[index], brush.TargetColor2, stronger);
                        }
                    }
                }
            }

            bool mask1Ok = false;
            bool mask2Ok = false;

            try
            {
                context.ControlMap.SetPixels(x, y, blockWidth, blockHeight, controlColorBlock, 0);
                context.ControlMap.Apply();
                mask1Ok = true;
            }
            catch (System.Exception ex)
            {
                string title = "控制图1 读写异常";
                string message = $"{TEXTURE_EXCEPTION_MESSAGE}\n\n{ex}";
                EditorUtility.DisplayDialog(title, message, "ok");
                Debug.LogError(message);
            }

            if (context.ControlMap2 != null)
            {
                try
                {
                    context.ControlMap2.SetPixels(x, y, blockWidth, blockHeight, _terrainBay2, 0);
                    context.ControlMap2.Apply();
                    mask2Ok = true;
                }
                catch (System.Exception ex)
                {
                    string title = "控制图2 读写异常";
                    string message = $"{TEXTURE_EXCEPTION_MESSAGE}\n\n{ex}";
                    EditorUtility.DisplayDialog(title, message, "ok");
                    Debug.LogError(message);
                }
            }

            return mask1Ok;
        }

        /// <summary>
        /// 保存控制图
        /// </summary>
        /// <param name="context">绘制上下文</param>
        /// <returns>是否成功保存</returns>
        public static bool SaveControlMaps(T4MPaintContext context)
        {
            bool savedAny = false;
            bool success = true;

            if (context.ControlMap != null)
            {
                savedAny = true;
                success &= T4MControlMapService.SaveExistingControlMap(context.ControlMap);
            }

            if (context.ControlMap2 != null)
            {
                savedAny = true;
                success &= T4MControlMapService.SaveExistingControlMap(context.ControlMap2);
            }

            return savedAny && success;
        }

        /// <summary>
        /// 根据射线击中位置计算 UV 坐标
        /// </summary>
        /// <param name="raycastHit">射线击中信息</param>
        /// <param name="uvIndex">UV 索引 (1 或 4)</param>
        /// <param name="texcoord">输出的 UV 坐标</param>
        /// <returns>是否成功计算</returns>
        public static bool CalculateRaycastHitTexcoord(ref RaycastHit raycastHit, int uvIndex, out Vector2 texcoord)
        {
            MeshCollider meshCollider = raycastHit.collider as MeshCollider;
            if (meshCollider == null || meshCollider.sharedMesh == null)
            {
                texcoord = Vector2.zero;
                return false;
            }

            Mesh mesh = meshCollider.sharedMesh;
            Vector2[] uvs;

            switch (uvIndex)
            {
                default:
                case 0:
                case 1:
                    uvs = mesh.uv;
                    break;
                case 2:
                    uvs = mesh.uv2;
                    break;
                case 3:
                    uvs = mesh.uv3;
                    break;
                case 4:
                    uvs = mesh.uv4;
                    break;
            }

            if (uvs == null || uvs.Length <= 0)
            {
                texcoord = Vector2.zero;
                return false;
            }

            int[] triangles = mesh.triangles;

            if (triangles == null || triangles.Length == 0)
            {
                texcoord = Vector2.zero;
                return false;
            }

            int triIndex = raycastHit.triangleIndex * 3;
            if (triIndex + 2 >= triangles.Length)
            {
                texcoord = Vector2.zero;
                return false;
            }

            Vector2 uv0 = uvs[triangles[triIndex + 0]];
            Vector2 uv1 = uvs[triangles[triIndex + 1]];
            Vector2 uv2 = uvs[triangles[triIndex + 2]];
            Vector3 barycentricCoord = raycastHit.barycentricCoordinate;

            texcoord = uv0 * barycentricCoord.x + uv1 * barycentricCoord.y + uv2 * barycentricCoord.z;
            return true;
        }

        /// <summary>
        /// 计算纹理颜色块的大小和位置
        /// </summary>
        /// <param name="uv">UV 坐标</param>
        /// <param name="blockSize">块大小</param>
        /// <param name="textureWidth">纹理宽度</param>
        /// <param name="textureHeight">纹理高度</param>
        /// <param name="x">输出：块起点 X</param>
        /// <param name="y">输出：块起点 Y</param>
        /// <param name="blockWidth">输出：块宽度</param>
        /// <param name="blockHeight">输出：块高度</param>
        public static void GetTextureColorBlockSize(
            Vector2 uv,
            int blockSize,
            int textureWidth,
            int textureHeight,
            out int x,
            out int y,
            out int blockWidth,
            out int blockHeight)
        {
            int texcoordX = Mathf.FloorToInt(uv.x * textureWidth);
            int texcoordY = Mathf.FloorToInt(uv.y * textureHeight);

            x = Mathf.Clamp(texcoordX - blockSize / 2, 0, textureWidth - 1);
            y = Mathf.Clamp(texcoordY - blockSize / 2, 0, textureHeight - 1);

            blockWidth = Mathf.Clamp((texcoordX + blockSize / 2), 0, textureWidth) - x;
            blockHeight = Mathf.Clamp((texcoordY + blockSize / 2), 0, textureHeight) - y;
        }

        /// <summary>
        /// 获取用于绘制预览的 Layer Mask
        /// </summary>
        /// <param name="currentSelect">当前选中对象</param>
        /// <returns>Layer Mask</returns>
        public static int GetPaintLayerMask(Transform currentSelect)
        {
            if (currentSelect == null) return ~0;

            if (currentSelect.gameObject.layer == 0)
            {
                return ~0;
            }
            else
            {
                return 1 << currentSelect.gameObject.layer;
            }
        }
    }
}
