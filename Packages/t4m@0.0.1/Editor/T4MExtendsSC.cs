// using System;
// using UnityEngine;
// using UnityEditor;
// using System.Collections;
//
// [CustomEditor(typeof(T4MObjSC))]
// [CanEditMultipleObjects]
// public class T4MExtendsSC : Editor
// {
//     private int m_layerMask = 0;
//     private bool m_toggleF;
//     private Texture2D[] m_undoObj;
//     private int m_state, m_oldState;
//      
//     private static Color[] s_terrainBay2;
//
//     private const string c_t4mTextureException = "修改控制图发生异常，控制图需要满足以下条件。\nRead/Write 标志被勾选。\n不能使用任何压缩格式，如 DXT, ASTC, ETC 等。";
//
//     private void OnSceneGUI()
//     {
//         if (T4MSC.T4MPreview && T4MSC.T4MMenuToolbar == 2)
//         {
//             Painter();
//         }
//         else
//         {
//             m_state = 3;
//         }
//
//         if (m_oldState != m_state)
//         {
//             MeshRenderer[] prev = FindObjectsOfType(typeof(MeshRenderer)) as MeshRenderer[];
//             foreach (MeshRenderer go in prev)
//             {
//                 if (go.hideFlags == HideFlags.HideInHierarchy || go.name == "previewT4M")
//                 {
//                     go.hideFlags = 0;
//                     DestroyImmediate(go.gameObject);
//                 }
//             }
//
//             m_oldState = m_state;
//         }
//     }
//
//     private void Painter()
//     {
//         if (T4MSC.CurrentSelect == null)
//         {
//             return;
//         }
//
//         m_state = 1;
//         
//         Event eve = Event.current;
//         if (eve.type == EventType.KeyDown && eve.keyCode == KeyCode.T)
//         {
//             T4MSC.T4MActived = T4MSC.T4MActived != "Activated" ? "Activated" : "Deactivated";
//         }
//
//         if (T4MSC.T4MPreview && T4MSC.T4MActived == "Activated" && T4MSC.T4MPreview.enabled == false || T4MSC.T4MPreview.enabled == false)
//         {
//             if (
//                 T4MSC.PaintPrev != T4MSC.PaintHandle.Follow_Normal_Circle &&
//                 T4MSC.PaintPrev != T4MSC.PaintHandle.Follow_Normal_WireCircle &&
//                 T4MSC.PaintPrev != T4MSC.PaintHandle.Hide_preview
//             )
//             {
//                 T4MSC.T4MPreview.enabled = true;
//             }
//         }
//         else if (T4MSC.T4MPreview && T4MSC.T4MActived == "Deactivated" && T4MSC.T4MPreview.enabled == true || T4MSC.T4MPreview.enabled == true)
//         {
//             if (T4MSC.PaintPrev != T4MSC.PaintHandle.Classic)
//             {
//                 T4MSC.T4MPreview.enabled = false;
//             }
//         }
//         
//         if (T4MSC.T4MActived == "Activated")
//         {
//             HandleUtility.AddDefaultControl(0);
//             RaycastHit raycastHit = new RaycastHit();
//             Ray terrain = HandleUtility.GUIPointToWorldRay(eve.mousePosition);
//             if (eve.type == EventType.KeyDown && eve.keyCode == KeyCode.KeypadPlus)
//             {
//                 T4MSC.brushSize += 1;
//             }
//             else if (eve.type == EventType.KeyDown && eve.keyCode == KeyCode.KeypadMinus)
//             {
//                 T4MSC.brushSize -= 1;
//             }
//
//             // 确保射线只检测与当前 Terrain 处于同一层的物体
//             LayerMask layerMask;
//             // 0: Default
//             if (T4MSC.CurrentSelect.gameObject.layer == 0)
//             {
//                 layerMask = ~0;
//             }
//             else
//             {
//                 layerMask = 1 << T4MSC.CurrentSelect.gameObject.layer;
//             }
//
//             if (Physics.Raycast(terrain, out raycastHit, Mathf.Infinity, layerMask))
//             {
//                 // 这里有时候会拿不到 T4MObjSC 组件
//                 var t4mObjSC = T4MSC.CurrentSelect.GetComponent<T4MObjSC>();
//                 if (t4mObjSC != null && t4mObjSC.ConvertType != "UT")
//                 {
//                     T4MSC.T4MPreview.transform.localEulerAngles = new Vector3(90, 180 + T4MSC.CurrentSelect.localEulerAngles.y, 0);
//                 }
//                 else
//                 {
//                     T4MSC.T4MPreview.transform.localEulerAngles = new Vector3(90, -90 + T4MSC.CurrentSelect.localEulerAngles.y, 0);
//                 }
//                 T4MSC.T4MPreview.transform.position = raycastHit.point;
//                 
//                 if (T4MSC.PaintPrev != T4MSC.PaintHandle.Classic && T4MSC.PaintPrev != T4MSC.PaintHandle.Hide_preview && T4MSC.PaintPrev != T4MSC.PaintHandle.Follow_Normal_WireCircle)
//                 {
//                     Handles.color = new Color(1f, 1f, 0f, 0.05f);
//                     Handles.DrawSolidDisc(raycastHit.point, raycastHit.normal, T4MSC.T4MPreview.orthographicSize * 0.9f);
//                 }
//                 else if (T4MSC.PaintPrev != T4MSC.PaintHandle.Classic && T4MSC.PaintPrev != T4MSC.PaintHandle.Hide_preview && T4MSC.PaintPrev != T4MSC.PaintHandle.Follow_Normal_Circle)
//                 {
//                     Handles.color = new Color(1f, 1f, 0f, 1f);
//                     Handles.DrawWireDisc(raycastHit.point, raycastHit.normal, T4MSC.T4MPreview.orthographicSize * 0.9f);
//                 }
//                 
//                 if ((eve.type == EventType.MouseDrag && eve.alt == false && eve.shift == false && eve.button == 0) || (eve.shift == false && eve.alt == false && eve.button == 0 && m_toggleF == false))
//                 {
//                     Vector2 raycastHitTexcoord;
//                     int uvIndex = T4MSC.useUV4 ? 4 : 1;
//                     if (!CalculateRaycastHitTexcoord(ref raycastHit, uvIndex, out raycastHitTexcoord))
//                     {
//                         return;
//                     }
//
//                     Vector2 uv = raycastHitTexcoord * T4MSC.T4MMaskTexUVCoord;
//                     GetTextureColorBlockSize(uv, T4MSC.T4MBrushSizeInPourcent, T4MSC.T4MMaskTex.width, T4MSC.T4MMaskTex.height, out int x, out int y, out int width, out int height);
//                     Color[] terrainControlColorBlock = T4MSC.T4MMaskTex.GetPixels(x, y, width, height, 0);
//
//                     #region Commented Code
//
//                     // Get Layer Base Texture
//                     //Color[] terrainLayerOneBaseTexBlock = GetTerrainLayerBaseTextureBlock(0, uv, T4MSC.T4MBrushSizeInPourcent);
//                     //Color[] terrainLayerTwoBaseTexBlock = GetTerrainLayerBaseTextureBlock(1, uv, T4MSC.T4MBrushSizeInPourcent);
//                     //Color[] terrainLayerThreeBaseTexBlock = GetTerrainLayerBaseTextureBlock(2, uv, T4MSC.T4MBrushSizeInPourcent);
//                     //Color[] terrainLayerFourBaseTexBlock = GetTerrainLayerBaseTextureBlock(3, uv, T4MSC.T4MBrushSizeInPourcent);
//                     //Color[] selectedTerrainLayerBaseTexBlock;
//                     //switch (T4MSC.GetSelectedBrushIndex())
//                     //{
//                     //    default:
//                     //    case 0:
//                     //        selectedTerrainLayerBaseTexBlock = terrainLayerOneBaseTexBlock;
//                     //        break;
//                     //    case 1:
//                     //        selectedTerrainLayerBaseTexBlock = terrainLayerTwoBaseTexBlock;
//                     //        break;
//                     //    case 2:
//                     //        selectedTerrainLayerBaseTexBlock = terrainLayerThreeBaseTexBlock;
//                     //        break;
//                     //    case 3:
//                     //        selectedTerrainLayerBaseTexBlock = terrainLayerFourBaseTexBlock;
//                     //        break;
//                     //}
//
//                     #endregion
//
//                     if (T4MSC.T4MMaskTex2)
//                     {
//                         s_terrainBay2 = T4MSC.T4MMaskTex2.GetPixels(x, y, width, height, 0);
//                     }
//
//                     int texcoordX = Mathf.FloorToInt(uv.x * T4MSC.T4MMaskTex.width);
//                     int texcoordY = Mathf.FloorToInt(uv.y * T4MSC.T4MMaskTex.height);
//                     for (int i = 0; i < height; i++)
//                     {
//                         for (int j = 0; j < width; j++)
//                         {
//                             // 像素在块里面的索引
//                             int index = (i * width) + j;
//
//                             #region Commented Code
//
//                             // Get Top Height
//                             // P.S. 高度在 shader 里面处理了，这里弄过于麻烦
//                             // TODO: 处理每层笔刷的贴图尺寸不一样的情况
//                             //float scaleX = (float)j / (float)width;
//                             //float scaleY = (float)i / (float)height;
//                             ////int index = i / height * 
//                             //float layerOneHeight = terrainLayerOneBaseTexBlock.Length <= 0 ? 0f : terrainLayerOneBaseTexBlock[index].a;
//                             //float layerTwoHeight = terrainLayerTwoBaseTexBlock.Length <= 0 ? 0f : terrainLayerTwoBaseTexBlock[index].a;
//                             //float layerThreeHeight = terrainLayerThreeBaseTexBlock.Length <= 0 ? 0f : terrainLayerThreeBaseTexBlock[index].a;
//                             //float layerFourHeight = terrainLayerFourBaseTexBlock.Length <= 0 ? 0f : terrainLayerFourBaseTexBlock[index].a;
//                             //float pixelTopHeight = GetPixelTopHeight(layerOneHeight, layerTwoHeight, layerThreeHeight, layerFourHeight, terrainBay[index]);                            
//                             //// 1: higher, can draw   0: lower, cant draw
//                             //float heightFactor = selectedTerrainLayerBaseTexBlock[index].a >= pixelTopHeight ? 1 : 0;
//
//                             #endregion
//
//                             float Stronger = T4MSC.T4MBrushAlpha[
//                                 Mathf.Clamp((y + i) - (texcoordY - T4MSC.T4MBrushSizeInPourcent / 2), 0, T4MSC.T4MBrushSizeInPourcent - 1) * T4MSC.T4MBrushSizeInPourcent +
//                                 Mathf.Clamp((x + j) - (texcoordX - T4MSC.T4MBrushSizeInPourcent / 2), 0, T4MSC.T4MBrushSizeInPourcent - 1)] * T4MSC.T4MStronger /* * heightFactor*/;
//
//                             if (T4MSC.T4MselTexture < 3)
//                             {
//                                 terrainControlColorBlock[index] = Color.Lerp(terrainControlColorBlock[index], T4MSC.T4MtargetColor, Stronger);
//                             }
//                             else
//                             {
//                                 terrainControlColorBlock[index] = Color.Lerp(terrainControlColorBlock[index], T4MSC.T4MtargetColor, Stronger); //*0.3f);
//                                 if (T4MSC.T4MMaskTex2)
//                                     s_terrainBay2[index] = Color.Lerp(s_terrainBay2[index], T4MSC.T4MtargetColor2, Stronger); ///0.3f);
//                             }
//                         }
//                     }
//
//                     bool mask_ok = false, mask2_ok = false;
//                     try
//                     {
//                         T4MSC.T4MMaskTex.SetPixels(x, y, width, height, terrainControlColorBlock, 0);
//                         T4MSC.T4MMaskTex.Apply();
//                         
//                         mask_ok = true;
//                     }
//                     catch (System.Exception ex)
//                     {
//                         string title = $"{nameof(T4MSC.T4MMaskTex)} 读写异常";
//                         string message = $"{c_t4mTextureException}\n\n{ex}";
//                         
//                         EditorUtility.DisplayDialog(title, message, "ok");
//                         Debug.LogError(message);
//
//                         T4MSC.T4MMenuToolbar = 0;
//                     }
//                     
//                     if (T4MSC.T4MMaskTex2)
//                     {
//                         try
//                         {
//                             T4MSC.T4MMaskTex2.SetPixels(x, y, width, height, s_terrainBay2, 0);
//                             T4MSC.T4MMaskTex2.Apply();
//                             
//                             mask2_ok = true;
//                         }
//                         catch (System.Exception ex)
//                         {
//                             string title = $"{nameof(T4MSC.T4MMaskTex2)} 读写异常";
//                             string message = $"{c_t4mTextureException}\n\n{ex}";
//                             
//                             EditorUtility.DisplayDialog(title, message, "ok");
//                             Debug.LogError(message);
//                             
//                             T4MSC.T4MMenuToolbar = 0;
//                         }
//
//                         if (mask2_ok)
//                         {
//                             m_undoObj = new Texture2D[2];
//                             m_undoObj[0] = T4MSC.T4MMaskTex;
//                             m_undoObj[1] = T4MSC.T4MMaskTex2;
//                         }
//                     }
//                     else
//                     {
//                         if (mask_ok)
//                         {
//                             m_undoObj = new Texture2D[1];
//                             m_undoObj[0] = T4MSC.T4MMaskTex;
//                         }
//                     }
//
//                     if (mask_ok || mask2_ok)
//                     {
//                         try
//                         {
//                             Undo.RegisterCompleteObjectUndo(m_undoObj, "T4MMask");
//                         }
//                         catch (System.Exception ex)
//                         {
//                             Debug.LogError($"撤销操作发生异常:\n{ex}");
//                         }
//                     }
//
//                     m_toggleF = true;
//                 }
//                 else if (eve.type == EventType.MouseUp && eve.alt == false && eve.button == 0 && m_toggleF == true)
//                 {
//                     T4MSC.SaveTexture();
//                     
//                     m_toggleF = false;
//                 }
//             }
//         }
//     }
//
//     /// <summary>
//     /// 根据射线击中位置在三角形中的重心坐标，计算 UV
//     /// </summary>
//     /// <param name="raycastHit">射线击中的目标</param>
//     /// <param name="uvIndex">第几套 UV</param>
//     /// <param name="texcoord">计算结果</param>
//     /// <returns>计算成功返回 true。</returns>
//     private bool CalculateRaycastHitTexcoord(ref RaycastHit raycastHit, int uvIndex, out Vector2 texcoord)
//     {
//         MeshCollider meshCollider = raycastHit.collider as MeshCollider;
//         if (meshCollider == null || meshCollider.sharedMesh == null)
//         {
//             texcoord = Vector2.zero;
//             return false;
//         }
//
//         Mesh mesh = meshCollider.sharedMesh;
//         Vector2[] uvs;
//         switch (uvIndex)
//         {
//             default:
//             case 0:
//             case 1:
//                 uvs = mesh.uv;
//                 break;
//             case 2:
//                 uvs = mesh.uv2;
//                 break;
//             case 3:
//                 uvs = mesh.uv3;
//                 break;
//             case 4:
//                 uvs = mesh.uv4;
//                 break;
//         }
//
//         if (uvs.Length <= 0)
//         {
//             texcoord = Vector2.zero;
//             return false;
//         }
//
//         int[] triangles = mesh.triangles;
//
//         if (triangles.Length == 0 || triangles.Length == 0)
//         {
//             texcoord = Vector2.zero;
//             return false;
//         }
//
//         Vector2 uv0 = uvs[triangles[raycastHit.triangleIndex * 3 + 0]];
//         Vector2 uv1 = uvs[triangles[raycastHit.triangleIndex * 3 + 1]];
//         Vector2 uv2 = uvs[triangles[raycastHit.triangleIndex * 3 + 2]];
//         Vector3 barycentricCoord = raycastHit.barycentricCoordinate;
//         texcoord = uv0 * barycentricCoord.x + uv1 * barycentricCoord.y + uv2 * barycentricCoord.z;
//         return true;
//     }
//
//     /// <summary>
//     /// 计算色块的大小
//     /// </summary>
//     /// <param name="uv">uv 坐标</param>
//     /// <param name="blockSize">色块的大小</param>
//     /// <param name="textureWidth">贴图的宽</param>
//     /// <param name="textureHeight">贴图的高</param>
//     /// <param name="x">色块的起点 X</param>
//     /// <param name="y">色块的起点 Y</param>
//     /// <param name="blockWidth">色块的最终宽</param>
//     /// <param name="blockHeight">色块的最终高</param>
//     void GetTextureColorBlockSize(Vector2 uv, int blockSize, int textureWidth, int textureHeight, out int x, out int y, out int blockWidth, out int blockHeight)
//     {
//         // 为归一化的纹理坐标
//         int texcoordX = Mathf.FloorToInt(uv.x * textureWidth);
//         int texcoordY = Mathf.FloorToInt(uv.y * textureHeight);
//         // (u, v) 在块的中心，起点 (x, y) 在块的左上角
//         x = Mathf.Clamp(texcoordX - blockSize / 2, 0, textureWidth - 1);
//         y = Mathf.Clamp(texcoordY - blockSize / 2, 0, textureHeight - 1);
//         // 块的右下角 - 块的左上角 = 长宽，同时注意不要超过贴图的边界（ Mathf.Clamp ），否则取值时会数组越界
//         blockWidth = Mathf.Clamp((texcoordX + blockSize / 2), 0, textureWidth) - x;
//         blockHeight = Mathf.Clamp((texcoordY + blockSize / 2), 0, textureHeight) - y;
//     }
//
//     /// <summary>
//     /// 从所有笔刷层中，得到某个像素最高的高度值
//     /// </summary>
//     /// <param name="layerOnePixelHeight"></param>
//     /// <param name="layerTwoPixelHeight"></param>
//     /// <param name="layerThreePixelHeight"></param>
//     /// <param name="layerFourPixelHeight"></param>
//     /// <param name="targetControlMapPixel"></param>
//     /// <returns></returns>
//     float GetPixelTopHeight(float layerOnePixelHeight, float layerTwoPixelHeight, float layerThreePixelHeight, float layerFourPixelHeight, Color targetControlMapPixel)
//     {
//         //T4MSC.GetBrushBaseTexture()
//         float layerOneHeight = targetControlMapPixel.r > 0 ? layerOnePixelHeight : 0f;
//         float layerTwoHeight = targetControlMapPixel.g > 0 ? layerTwoPixelHeight : 0f;
//         float layerThreeHeight = targetControlMapPixel.b > 0 ? layerThreePixelHeight : 0f;
//         float layerFourHeight = targetControlMapPixel.a > 0 ? layerFourPixelHeight : 0f;
//         return Mathf.Max(layerOneHeight, layerTwoHeight, layerThreeHeight, layerFourHeight);
//     }
//
//     /// <summary>
//     /// 由于控制图尺寸和颜色图尺寸可能不一样，因此需要按比例修正颜色块的大小。默认所有的贴图都是正方形，因此不支持长宽不等的贴图。
//     /// </summary>
//     /// <param name="layerIndex"></param>
//     /// <param name="x"></param>
//     /// <param name="y"></param>
//     /// <param name="blockWidth"></param>
//     /// <param name="blockHeight"></param>
//     int FixBaseTextureColorBlockSize(int layerIndex, int controlWidth, int controlBlockSize)
//     {
//         Texture2D terrainLayerBaseTex = T4MSC.GetBrushBaseTexture(layerIndex) as Texture2D;
//         if (terrainLayerBaseTex.width != controlWidth)
//         {
//             float scale = (float)controlWidth / (float)terrainLayerBaseTex.width;
//             return Mathf.FloorToInt(controlBlockSize * scale);
//
//             // TODO：增加长宽不等的支持！如果改的话，GetTextureColorBlockSize 也得改，blockSize 得变成长和宽
//             //bool maskMapHeightLarger = controlHeight > terrainLayerBaseTex.height;
//             //bitOffset = maskMapHeightLarger ? controlHeight / terrainLayerBaseTex.height : terrainLayerBaseTex.height / controlHeight;
//             //bitOffset--;
//             //controlBlockSize = maskMapHeightLarger ? controlBlockSize >> bitOffset : controlBlockSize << bitOffset;
//         }
//         else
//         {
//             return controlBlockSize;
//         }
//     }
//
//     /// <summary>
//     /// 获取某个笔刷层的基础颜色纹理的一块颜色值
//     /// </summary>
//     /// <param name="layerIndex"></param>
//     /// <param name="x"></param>
//     /// <param name="y"></param>
//     /// <param name="blockWidth"></param>
//     /// <param name="blockHeight"></param>
//     /// <returns></returns>
//     Color[] GetTerrainLayerBaseTextureBlock(int layerIndex, Vector2 uv, int blockSize)
//     {
//         Texture2D terrainLayerBaseTex = T4MSC.GetBrushBaseTexture(layerIndex) as Texture2D;
//
//         int finalBlockSize = FixBaseTextureColorBlockSize(layerIndex, T4MSC.T4MMaskTex.width, blockSize);
//         GetTextureColorBlockSize(uv, finalBlockSize, terrainLayerBaseTex.width, terrainLayerBaseTex.height, out int x, out int y, out int blockWidth, out int blockHeight);
//
//         if (terrainLayerBaseTex != null)
//         {
//             return terrainLayerBaseTex.GetPixels(x, y, blockWidth, blockHeight, 0);
//         }
//         else
//         {
//             return new Color[0];
//         }
//     }
// }