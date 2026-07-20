// using UnityEngine;
// using UnityEditor;
// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.IO;
// using System.Text;
//
// public class SubstanceImporter : AssetImporter
// {
//     public Material[] GetMaterials()
//     {
//         return null;
//     }
// }
//
// public class T4MSC : EditorWindow
// {
//     public static Transform CurrentSelect;
//
//     public enum BrushCount
//     {
//         Two = 0,
//         Three,
//         Four
//     }
//
//     private GUIContent[] MenuIcon = new GUIContent[3];
//
//     public static int T4MMenuToolbar = 0;
//
//     private enum CreaType
//     {
//         Classic_T4M,
//         Custom
//     }
//
//     private CreaType CreationBB = CreaType.Classic_T4M;
//
//     private enum MaterialType
//     {
//         Classic,
//         Substances
//     }
//
//     public static string T4MActived = "Activated";
//     string terrainName = "";
//     string[] MyT4MPaintMenu = { "绘制", "材质设置" };
//     string[] LODMenu = { "LOD Manager", "LOD Composer" };
//     string[] BillMenu = { "Billboard Manager", "Billboard Creator" };
//     string[] brushCountNumStr = { "两层", "三层", "四层" };
//     string PrefabName = "Name";
//     string CheckStatus;
//     
//     GUIContent keepTextureGUIContent = new("保留原始贴图", "Can keep the first 4 splats and first Blend.");
//
//     int tCount;
//     int counter;
//     int totalCount;
//     int X;
//     int Y;
//
//     /// <summary>
//     /// Equals to vertices count
//     /// </summary>
//     int T4MResolution = 90;
//
//     /// <summary>
//     /// Scale between unity terrain an T4M. terrain.heightmapResolusion / T4MResolution
//     /// </summary>
//     float tScale = 4.1f;
//
//     float HeightmapWidth = 0;
//     float HeightmapHeight = 0;
//     TerrainData terrainDat;
//     float progressUpdateInterval = 10000;
//     int vertexInfo;
//     int trisInfo;
//     int LODM = 0;
//     int BillM;
//     int MyT4MV = 0;
//     float MaximunView = 60.0f;
//     float StartLOD2 = 20.0f;
//     float StartLOD3 = 40.0f;
//     float UpdateInterval = 1f;
//     static int selBrush = 0;
//     int selProcedural = 0;
//     public static int brushSize = 16;
//     int oldSelBrush;
//     int layerMask = 1 << 30;
//     int oldBrushSizeInPourcent;
//     int oldselTexture;
//     public static int T4MPlantSel = 0;
//     public static float T4MObjSize = 1;
//
//     T4MObjSC[] T4MObjCounter;
//
//     Texture Layer1;
//     Texture Layer2;
//     Texture Layer3;
//     Texture Layer4;
//     Texture Layer5;
//     Texture Layer6;
//     Texture LMMan;
//     Texture Layer1Bump;
//     Texture Layer2Bump;
//     Texture Layer3Bump;
//     Texture Layer4Bump;
//     Texture Layer1Mask;
//     Texture Layer2Mask;
//     Texture Layer3Mask;
//     Texture Layer4Mask;
//     public static Texture[] TexBrush;
//     Texture[] TexTexture;
//     Texture[] TexObject = new Texture[6];
//     public static bool[] T4MBoolObj = new bool[6];
//
//     bool joinTiles = true;
//     bool intialized = false;
//     bool deleteOriginUnityTerrain = false;
//     bool hideOriginUnityTerrain = true;
//     int targetT4MLayerIndex = 0;
//
//     Vector2 Layer1Tile;
//     Vector2 Layer2Tile;
//     Vector2 Layer3Tile;
//     Vector2 Layer4Tile;
//     Vector2 Layer5Tile;
//     Vector2 Layer6Tile;
//     Vector2 scrollPos;
//
//     GameObject Child;
//     GameObject UnityTerrain;
//     GameObject AddObject;
//     Transform PlayerCam;
//     Material PreceduralAdd;
//     Material Precedural;
//
//     TerrainData terrainData;
//
//     //LightModel lightModel = LightModel.Lit;
//     int brushCountNumIdx = 2;
//     BrushCount brushCount = BrushCount.Four;
//     public static bool useUV4 = false;
//
//     MaterialType MaterialTyp = MaterialType.Classic;
//
//     public static int T4MBrushSizeInPourcent;
//     public static Color T4MtargetColor;
//     public static Color T4MtargetColor2;
//     public static Texture2D T4MMaskTex2;
//     public static Texture2D T4MMaskTex;
//     public static float[] T4MBrushAlpha;
//     public static float T4MStronger = 0.5f;
//     public static Projector T4MPreview;
//     static int T4MSelectID;
//     public static int T4MselTexture = 0;
//
//     string T4MEditorFolder = "Packages/T4M/Editor/";
//
//     string T4MPrefabFolderEditorKey
//     {
//         get { return "T4MPrefabFolderEditorKey" + Application.dataPath; }
//     }
//
//     string T4MPrefabFolderDefaultValue = "Assets/ArtRes/T4MOBJ";
//
//     string FinalExpName;
//     public static float T4MMaskTexUVCoord = 1f;
//
//     string defaultShaderName = "XKnight/Scene/Terrain";
//     float shiness0;
//     float shiness1;
//     float shiness2;
//     float shiness3;
//     Color ShinessColor;
//     Texture MaterialAdd;
//     public static float T4MDistanceMax = 15.0f;
//     public static float T4MDistanceMin = 15.0f;
//     public static float T4MrandX = 0.0f;
//     public static float T4MrandY = 1.0f;
//     public static float T4MrandZ = 0.0f;
//     public static bool T4MRandomRot = true;
//     public static bool T4MRandomSpa;
//     public static float T4MYOrigin = 0.02f;
//     public static float T4MSizeVar;
//     public static string T4MGroupName = "Group1";
//     public static bool T4MCreateColl;
//     public static bool T4MStaticObj = true;
//
//     public static int T4MselectObj;
//
//     //bool NewPref = false;
//     bool needCreateNewMat = true;
//     string newMatName = string.Empty;
//     string newMatPath = "Assets/ArtRes/T4MOBJ/Materials/";
//     Material oldModelMaterial;
//     int partofT4MObj = 0;
//     bool keepTexture = true;
//
//     public enum PaintHandle
//     {
//         Classic = 0,
//         Follow_Normal_Circle,
//         Follow_Normal_WireCircle,
//         Hide_preview
//     }
//
//     int paintPrevInt = 0;
//     public static PaintHandle PaintPrev = PaintHandle.Classic;
//     string[] paintPrevNames = { "常规", "圆面", "圆圈", "不显示" };
//
//     Vector4 UpSideTile = new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
//     float UpSideF = 2.5f;
//     float BlendFac = 4;
//
//     Renderer[] NbrPartObj;
//
//     void OnDestroy()
//     {
//         T4MMenuToolbar = 0;
//         terrainDat = null;
//         vertexInfo = 0;
//         partofT4MObj = 0;
//         trisInfo = 0;
//         TexTexture = null;
//         T4MSelectID = 0;
//     }
//
//     [MenuItem("Window/T4M Terrain Tool %t")]
//     static void Initialize()
//     {
//         T4MSC window = (T4MSC)EditorWindow.GetWindowWithRect(typeof(T4MSC), new Rect(0, 0, 386, 585), false, "T4M SC");
//         window.Show();
//     }
//
//     void OnInspectorUpdate()
//     {
//         Repaint();
//     }
//
//     void OnGUI()
//     {
//         CurrentSelect = Selection.activeTransform;
//
//         MenuIcon[0] = new GUIContent(AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Icons/conv.png", typeof(Texture2D)) as Texture);
//         //MenuIcon[1] = new GUIContent(AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Icons/optimize.png", typeof(Texture2D)) as Texture);
//         MenuIcon[1] = new GUIContent(AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Icons/myt4m.png", typeof(Texture2D)) as Texture);
//         MenuIcon[2] = new GUIContent(AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Icons/paint.png", typeof(Texture2D)) as Texture);
//         //MenuIcon[4] = new GUIContent(AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Icons/plant.png", typeof(Texture2D)) as Texture);
//         //MenuIcon[5] = new GUIContent(AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Icons/lod.png", typeof(Texture2D)) as Texture);
//         //MenuIcon[6] = new GUIContent(AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Icons/bill.png", typeof(Texture2D)) as Texture);
//
//         if (CurrentSelect && Selection.activeInstanceID != T4MSelectID || UnityTerrain && T4MMenuToolbar != 0 || T4MMenuToolbar != 3)
//         {
//             IniNewSelect();
//         }
//
//         GUILayout.BeginHorizontal();
//         GUILayout.BeginArea(new Rect(0, 0, 90, 620));
//         GUILayout.Label(AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/T4MBAN.jpg", typeof(Texture2D)) as Texture2D, GUILayout.Width(24), GUILayout.Height(582));
//         GUILayout.EndArea();
//         GUILayout.BeginArea(new Rect(25, 0, 363, 620));
//         EditorGUILayout.Space();
//         GUILayout.BeginHorizontal("box");
//         T4MMenuToolbar = (int)GUILayout.Toolbar(T4MMenuToolbar, MenuIcon, "gridlist", GUILayout.Width(66 /*172*/), GUILayout.Height(18));
//         GUILayout.FlexibleSpace();
//
//         GUILayout.Label("Controls", GUILayout.Width(52));
//         if (GUILayout.Button(T4MActived, GUILayout.Width(80)))
//         {
//             if (T4MActived == "Activated")
//             {
//                 T4MActived = "Deactivated";
//             }
//             else
//             {
//                 T4MActived = "Activated";
//             }
//         }
//
//         GUILayout.EndHorizontal();
//         GUILayout.Label(AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/separator.png", typeof(Texture)) as Texture);
//
//         if (CurrentSelect != null && T4MActived == "Activated")
//         {
//             if (CurrentSelect.GetComponent<T4MPartSC>())
//             {
//                 Selection.activeTransform = CurrentSelect.parent;
//             }
//
//             Renderer[] rendererPart = CurrentSelect.GetComponentsInChildren<Renderer>();
//
//
//             if (CurrentSelect.GetComponent<T4MObjSC>() && (!CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial || !CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMesh))
//             {
//                 if (rendererPart.Length == 0)
//                 {
//                     CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial = CurrentSelect.GetComponent<Renderer>().sharedMaterial;
//                     CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMesh = CurrentSelect.gameObject.GetComponent<MeshFilter>();
//                 }
//                 else
//                 {
//                     for (int i = 0; i < rendererPart.Length; i++)
//                     {
//                         CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial = rendererPart[0].sharedMaterial;
//                         CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMesh = rendererPart[0].gameObject.GetComponent<MeshFilter>();
//                     }
//                 }
//             }
//             else if (CurrentSelect.GetComponent<T4MObjSC>() && CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial)
//             {
//                 if (rendererPart.Length == 0)
//                 {
//                     if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial != CurrentSelect.GetComponent<Renderer>().sharedMaterial)
//                         CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial = CurrentSelect.GetComponent<Renderer>().sharedMaterial;
//                     EditorUtility.SetSelectedRenderState(CurrentSelect.GetComponent<Renderer>(), EditorSelectedRenderState.Wireframe);
//                 }
//                 else
//                 {
//                     if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial != rendererPart[0].sharedMaterial)
//                     {
//                         CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial = rendererPart[0].sharedMaterial;
//                     }
//
//                     for (int i = 0; i < rendererPart.Length; i++)
//                     {
//                         if (rendererPart[i].sharedMaterial != rendererPart[0].sharedMaterial)
//                         {
//                             rendererPart[i].sharedMaterial = rendererPart[0].sharedMaterial;
//                         }
//
//                         EditorUtility.SetSelectedRenderState(rendererPart[i], EditorSelectedRenderState.Wireframe);
//                     }
//                 }
//             }
//
//             if (CurrentSelect && !CurrentSelect.GetComponent<T4MObjSC>())
//             {
//                 int countchild = CurrentSelect.transform.childCount;
//                 if (countchild > 0)
//                 {
//                     NbrPartObj = CurrentSelect.transform.GetComponentsInChildren<Renderer>();
//                 }
//             }
//
//             switch (T4MMenuToolbar)
//             {
//                 case 0:
//                     ConverterMenu();
//                     break;
//
//                 case 1:
//                     MyT4M();
//                     break;
//                 //Optimize();
//                 //break;
//
//                 case 2:
//                     PainterMenu();
//                     break;
//                 //MyT4M();
//                 //break;
//
//                 case 3:
//                     PainterMenu();
//                     break;
//
//                 //case 4:
//                 //    Planting();
//                 //    break;
//
//                 //case 5:
//                 //    afLOD();
//                 //    break;
//
//                 //case 6:
//                 //    BillboardMenu();
//                 //    break;
//             }
//         }
//         else
//         {
//             if (CurrentSelect && CurrentSelect.GetComponent<T4MObjSC>() && CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial)
//             {
//                 Renderer[] rendererPart = CurrentSelect.GetComponentsInChildren<Renderer>();
//                 if (rendererPart.Length == 0)
//                 {
//                     EditorUtility.SetSelectedRenderState(CurrentSelect.GetComponent<Renderer>(), EditorSelectedRenderState.Wireframe);
//                 }
//                 else
//                 {
//                     for (int i = 0; i < rendererPart.Length; i++)
//                     {
//                         EditorUtility.SetSelectedRenderState(rendererPart[i], EditorSelectedRenderState.Wireframe);
//                     }
//                 }
//             }
//
//             GUILayout.FlexibleSpace();
//             GUILayout.BeginHorizontal();
//             GUILayout.FlexibleSpace();
//             GUILayout.Label(AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/waiting.png", typeof(Texture)) as Texture);
//             GUILayout.FlexibleSpace();
//             GUILayout.EndHorizontal();
//             GUILayout.FlexibleSpace();
//         }
//
//         GUILayout.EndArea();
//         GUILayout.EndHorizontal();
//     }
//
//     /// <summary>
//     /// 绘制菜单
//     /// </summary>
//     void PixelPainterMenu()
//     {
//         if (CurrentSelect.GetComponent<T4MObjSC>())
//         {
//             if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial && CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat0") &&
//                 CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat1") && CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Control"))
//             {
//                 IniBrush();
//                 InitPincil();
//
//                 if (!T4MPreview)
//                 {
//                     InitPreview();
//                 }
//
//                 if (intialized)
//                 {
//                     GUILayout.BeginHorizontal();
//                     GUILayout.FlexibleSpace();
//                     GUILayout.Label(AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/brushes.jpg", typeof(Texture)) as Texture, "label");
//                     GUILayout.BeginHorizontal("box", GUILayout.Width(318));
//                     GUILayout.FlexibleSpace();
//                     selBrush = GUILayout.SelectionGrid(selBrush, TexBrush, 9, "gridlist", GUILayout.Width(290), GUILayout.Height(70));
//                     GUILayout.FlexibleSpace();
//                     GUILayout.EndHorizontal();
//                     GUILayout.FlexibleSpace();
//                     GUILayout.EndHorizontal();
//
//                     GUILayout.BeginHorizontal();
//                     GUILayout.FlexibleSpace();
//                     GUILayout.BeginHorizontal("box", GUILayout.Width(340));
//                     GUILayout.FlexibleSpace();
//                     if (TexTexture.Length > 4)
//                         T4MselTexture = GUILayout.SelectionGrid(T4MselTexture, TexTexture, 6, "gridlist", GUILayout.Width(340), GUILayout.Height(58));
//                     else
//                         T4MselTexture = GUILayout.SelectionGrid(T4MselTexture, TexTexture, 4, "gridlist", GUILayout.Width(340), GUILayout.Height(86));
//                     GUILayout.FlexibleSpace();
//                     GUILayout.EndHorizontal();
//                     GUILayout.FlexibleSpace();
//                     GUILayout.EndHorizontal();
//
//                     EditorGUILayout.Space();
//
//
//                     GUILayout.BeginHorizontal();
//                     GUILayout.FlexibleSpace();
//                     GUILayout.BeginVertical("box", GUILayout.Width(347));
//                     GUILayout.Label("笔刷设置（显示笔刷需要打开场景视图的 Gizmos 开关）", EditorStyles.boldLabel);
//                     EditorGUILayout.Space();
//                     GUILayout.BeginHorizontal();
//                     GUILayout.Label("笔刷预览", GUILayout.Width(145));
//                     paintPrevInt = EditorGUILayout.Popup(paintPrevInt, paintPrevNames, GUILayout.Width(160));
//                     PaintPrev = (PaintHandle)paintPrevInt;
//                     GUILayout.EndHorizontal();
//                     brushSize = (int)EditorGUILayout.Slider("笔刷大小", brushSize, 1, 36);
//                     T4MStronger = EditorGUILayout.Slider("笔刷强度", T4MStronger, 0.05f, 1f);
//                     GUILayout.EndVertical();
//                     GUILayout.FlexibleSpace();
//                     GUILayout.EndHorizontal();
//
//
//                     if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_SpecColor"))
//                     {
//                         EditorGUILayout.Space();
//                         GUILayout.BeginHorizontal();
//                         GUILayout.FlexibleSpace();
//                         GUILayout.BeginVertical("box", GUILayout.Width(347), GUILayout.Height(96));
//                         ShinessColor = EditorGUILayout.ColorField("Shininess Color", ShinessColor);
//                         CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetColor("_SpecColor", ShinessColor);
//                         EditorGUILayout.Space();
//                         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_ShininessL0"))
//                         {
//                             shiness0 = EditorGUILayout.Slider("Shininess Layer 1", shiness0, 0.00f, 1.0f);
//                             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetFloat("_ShininessL0", shiness0);
//                         }
//
//                         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_ShininessL1"))
//                         {
//                             shiness1 = EditorGUILayout.Slider("Shininess Layer 2", shiness1, 0.00f, 1.0f);
//                             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetFloat("_ShininessL1", shiness1);
//                         }
//
//                         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_ShininessL2"))
//                         {
//                             shiness2 = EditorGUILayout.Slider("Shininess Layer 3", shiness2, 0.00f, 1.0f);
//                             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetFloat("_ShininessL2", shiness2);
//                         }
//
//                         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_ShininessL3"))
//                         {
//                             shiness3 = EditorGUILayout.Slider("Shininess Layer 4", shiness3, 0.00f, 1.0f);
//                             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetFloat("_ShininessL3", shiness3);
//                         }
//
//                         GUILayout.EndVertical();
//                         GUILayout.FlexibleSpace();
//                         GUILayout.EndHorizontal();
//                     }
//
//                     EditorGUILayout.Space();
//
//                     GUILayout.BeginHorizontal();
//                     GUILayout.FlexibleSpace();
//                     if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_SpecColor"))
//                     {
//                         scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(350), GUILayout.Height(140));
//                         GUILayout.BeginVertical("box", GUILayout.Width(320));
//                     }
//                     else
//                     {
//                         GUILayout.BeginVertical("box", GUILayout.Width(320));
//                         if (TexTexture.Length > 4)
//                             scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(340), GUILayout.Height(215));
//                         else scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(340), GUILayout.Height(180));
//                     }
//
//                     GUILayout.Label("纹理缩放：", EditorStyles.boldLabel);
//                     EditorGUILayout.Space();
//                     joinTiles = EditorGUILayout.Toggle("X、Y 同步缩放", joinTiles);
//                     EditorGUILayout.Space();
//                     if (joinTiles)
//                     {
//                         Layer1Tile.x = Layer1Tile.y = EditorGUILayout.Slider("第一层笔刷缩放：", Layer1Tile.x, 1, 500 * T4MMaskTexUVCoord);
//                         CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTextureScale("_Splat0", new Vector2(Layer1Tile.x, Layer1Tile.x));
//                         EditorGUILayout.Space();
//                         Layer2Tile.x = Layer2Tile.y = EditorGUILayout.Slider("第二层笔刷缩放：", Layer2Tile.x, 1, 500 * T4MMaskTexUVCoord);
//                         CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTextureScale("_Splat1", new Vector2(Layer2Tile.x, Layer2Tile.x));
//                         EditorGUILayout.Space();
//                         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat2"))
//                         {
//                             Layer3Tile.x = Layer3Tile.y = EditorGUILayout.Slider("第三层笔刷缩放：", Layer3Tile.x, 1, 500 * T4MMaskTexUVCoord);
//                             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTextureScale("_Splat2", new Vector2(Layer3Tile.x, Layer3Tile.x));
//                         }
//
//                         EditorGUILayout.Space();
//                         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat3"))
//                         {
//                             Layer4Tile.x = Layer4Tile.y = EditorGUILayout.Slider("第四层笔刷缩放：", Layer4Tile.x, 1, 500 * T4MMaskTexUVCoord);
//                             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTextureScale("_Splat3", new Vector2(Layer4Tile.x, Layer4Tile.x));
//                         }
//
//                         EditorGUILayout.Space();
//                         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat4"))
//                         {
//                             Layer5Tile.x = Layer5Tile.y = EditorGUILayout.Slider("第五层笔刷缩放：", Layer5Tile.x, 1, 500 * T4MMaskTexUVCoord);
//                             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTextureScale("_Splat4", new Vector2(Layer5Tile.x, Layer5Tile.x));
//                         }
//
//                         EditorGUILayout.Space();
//                         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat5"))
//                         {
//                             Layer6Tile.x = Layer6Tile.y = EditorGUILayout.Slider("第六层笔刷缩放：", Layer6Tile.x, 1, 500 * T4MMaskTexUVCoord);
//                             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTextureScale("_Splat5", new Vector2(Layer6Tile.x, Layer6Tile.x));
//                         }
//                     }
//                     else
//                     {
//                         Layer1Tile.x = EditorGUILayout.Slider("第一层笔刷缩放 X：", Layer1Tile.x, 1, 500 * T4MMaskTexUVCoord);
//                         Layer1Tile.y = EditorGUILayout.Slider("第一层笔刷缩放 Y：", Layer1Tile.y, 1, 500 * T4MMaskTexUVCoord);
//                         CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTextureScale("_Splat0", new Vector2(Layer1Tile.x, Layer1Tile.y));
//                         EditorGUILayout.Space();
//                         Layer2Tile.x = EditorGUILayout.Slider("第二层笔刷缩放 X：", Layer2Tile.x, 1, 500 * T4MMaskTexUVCoord);
//                         Layer2Tile.y = EditorGUILayout.Slider("第二层笔刷缩放 Y：", Layer2Tile.y, 1, 500 * T4MMaskTexUVCoord);
//                         CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTextureScale("_Splat1", new Vector2(Layer2Tile.x, Layer2Tile.y));
//                         EditorGUILayout.Space();
//                         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat2"))
//                         {
//                             Layer3Tile.x = EditorGUILayout.Slider("第三层笔刷缩放 X：", Layer3Tile.x, 1, 500 * T4MMaskTexUVCoord);
//                             Layer3Tile.y = EditorGUILayout.Slider("第三层笔刷缩放 Y：", Layer3Tile.y, 1, 500 * T4MMaskTexUVCoord);
//                             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTextureScale("_Splat2", new Vector2(Layer3Tile.x, Layer3Tile.y));
//                         }
//
//                         EditorGUILayout.Space();
//                         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat3"))
//                         {
//                             Layer4Tile.x = EditorGUILayout.Slider("第四层笔刷缩放 X：", Layer4Tile.x, 1, 500 * T4MMaskTexUVCoord);
//                             Layer4Tile.y = EditorGUILayout.Slider("第四层笔刷缩放 Y：", Layer4Tile.y, 1, 500 * T4MMaskTexUVCoord);
//                             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTextureScale("_Splat3", new Vector2(Layer4Tile.x, Layer4Tile.y));
//                         }
//
//                         EditorGUILayout.Space();
//                         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat4"))
//                         {
//                             Layer5Tile.x = EditorGUILayout.Slider("第五层笔刷缩放 X：", Layer5Tile.x, 1, 500 * T4MMaskTexUVCoord);
//                             Layer5Tile.y = EditorGUILayout.Slider("第五层笔刷缩放 Y：", Layer5Tile.y, 1, 500 * T4MMaskTexUVCoord);
//                             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTextureScale("_Splat4", new Vector2(Layer5Tile.x, Layer5Tile.y));
//                         }
//
//                         EditorGUILayout.Space();
//                         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat5"))
//                         {
//                             Layer6Tile.x = EditorGUILayout.Slider("第六层笔刷缩放 X：", Layer6Tile.x, 1, 500 * T4MMaskTexUVCoord);
//                             Layer6Tile.y = EditorGUILayout.Slider("第六层笔刷缩放 Y：", Layer6Tile.y, 1, 500 * T4MMaskTexUVCoord);
//                             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTextureScale("_Splat5", new Vector2(Layer6Tile.x, Layer6Tile.y));
//                         }
//                     }
//
//                     EditorGUILayout.EndScrollView();
//                     GUILayout.EndVertical();
//                     GUILayout.FlexibleSpace();
//                     GUILayout.EndHorizontal();
//                     
//                     if (TexBrush.Length > 0)
//                     {
//                         T4MPreview.material.SetTexture("_MaskTex", TexBrush[selBrush]);
//                         MeshFilter temp = CurrentSelect.GetComponent<MeshFilter>();
//                         if (temp == null)
//                         {
//                             temp = CurrentSelect.GetComponent<T4MObjSC>().T4MMesh;
//                         }
//                         T4MPreview.orthographicSize = (brushSize * CurrentSelect.localScale.x) * (temp.sharedMesh.bounds.size.x / 200);
//                     }
//                     
//                     float test = T4MStronger * 200 / 100;
//                     T4MPreview.material.SetFloat("_Transp", Mathf.Clamp(test, 0.4f, 1));
//
//                     T4MBrushSizeInPourcent = (int)Mathf.Round((brushSize * T4MMaskTex.width) / 100);
//
//                     if (T4MselTexture == 0)
//                     {
//                         T4MPreview.material.SetTextureScale("_MainTex", Layer1Tile);
//                     }
//                     else if (T4MselTexture == 1)
//                     {
//                         T4MPreview.material.SetTextureScale("_MainTex", Layer2Tile);
//                     }
//                     else if (T4MselTexture == 2)
//                     {
//                         T4MPreview.material.SetTextureScale("_MainTex", Layer3Tile);
//                     }
//                     else if (T4MselTexture == 3)
//                     {
//                         T4MPreview.material.SetTextureScale("_MainTex", Layer4Tile);
//                     }
//                     else if (T4MselTexture == 4)
//                     {
//                         T4MPreview.material.SetTextureScale("_MainTex", Layer5Tile);
//                     }
//                     else if (T4MselTexture == 5)
//                     {
//                         T4MPreview.material.SetTextureScale("_MainTex", Layer6Tile);
//                     }
//
//                     if (selBrush != oldSelBrush || T4MBrushSizeInPourcent != oldBrushSizeInPourcent || T4MBrushAlpha == null || T4MselTexture != oldselTexture)
//                     {
//                         if (T4MselTexture == 0)
//                         {
//                             T4MPreview.material.SetTexture("_MainTex", CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat0") as Texture);
//                             
//                             T4MtargetColor = new Color(1f, 0f, 0f, 0f);
//                             if (T4MMaskTex2)
//                             {
//                                 T4MtargetColor2 = new Color(0, 0, 0, 0);
//                             }
//                         }
//                         else if (T4MselTexture == 1)
//                         {
//                             T4MPreview.material.SetTexture("_MainTex", CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat1") as Texture);
//
//                             if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.shader == Shader.Find("T4MShaders/ShaderModel1/T4M 2 Textures Auto BeastLM 2DrawCall") ||
//                                 CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.shader == Shader.Find("T4MShaders/ShaderModel1/T4M 2 Textures ManualAdd BeastLM_1DC") ||
//                                 CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.shader == Shader.Find("T4MShaders/ShaderModel1/T4M 2 Textures ManualAdd CustoLM 1DC"))
//                             {
//                                 T4MtargetColor = new Color(0, 0, 0, 1);
//                             }
//                             else
//                             {
//                                 T4MtargetColor = new Color(0, 1, 0, 0);
//                                 if (T4MMaskTex2)
//                                 {
//                                     T4MtargetColor2 = new Color(0, 0, 0, 0);
//                                 }
//                             }
//                         }
//                         else if (T4MselTexture == 2)
//                         {
//                             T4MPreview.material.SetTexture("_MainTex", CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat2") as Texture);
//                             
//                             T4MtargetColor = new Color(0, 0, 1, 0);
//                             if (T4MMaskTex2)
//                             {
//                                 T4MtargetColor2 = new Color(0, 0, 0, 0);
//                             }
//                         }
//                         else if (T4MselTexture == 3)
//                         {
//                             T4MPreview.material.SetTexture("_MainTex", CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat3") as Texture);
//                             
//                             T4MtargetColor = new Color(0, 0, 0, 1);
//                             if (T4MMaskTex2)
//                             {
//                                 T4MtargetColor2 = new Color(1, 0, 0, 0);
//                             }
//                         }
//                         else if (T4MselTexture == 4)
//                         {
//                             T4MPreview.material.SetTexture("_MainTex", CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat4") as Texture);
//                             
//                             T4MtargetColor = new Color(0, 0, 0, 1);
//                             if (T4MMaskTex2)
//                             {
//                                 T4MtargetColor2 = new Color(0, 1, 0, 0);
//                             }
//                         }
//                         else if (T4MselTexture == 5)
//                         {
//                             T4MPreview.material.SetTexture("_MainTex", CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat5") as Texture);
//                             
//                             T4MtargetColor = new Color(0, 0, 0, 1);
//                             if (T4MMaskTex2)
//                             {
//                                 T4MtargetColor2 = new Color(0, 0, 1, 0);
//                             }
//                         }
//
//                         Texture2D TBrush = TexBrush[selBrush] as Texture2D;
//                         T4MBrushAlpha = new float[T4MBrushSizeInPourcent * T4MBrushSizeInPourcent];
//                         for (int i = 0; i < T4MBrushSizeInPourcent; i++)
//                         {
//                             for (int j = 0; j < T4MBrushSizeInPourcent; j++)
//                             {
//                                 T4MBrushAlpha[j * T4MBrushSizeInPourcent + i] = TBrush.GetPixelBilinear(((float)i) / T4MBrushSizeInPourcent, ((float)j) / T4MBrushSizeInPourcent).a;
//                             }
//                         }
//
//                         oldselTexture = T4MselTexture;
//                         oldSelBrush = selBrush;
//                         oldBrushSizeInPourcent = T4MBrushSizeInPourcent;
//                     }
//                 }
//             }
//         }
//         else
//         {
//             GUILayout.BeginHorizontal();
//             GUILayout.FlexibleSpace();
//             GUILayout.Label("请选择转换过的对象进行修改.", EditorStyles.boldLabel);
//             GUILayout.FlexibleSpace();
//             GUILayout.EndHorizontal();
//         }
//     }
//     
//     private void InitPreview()
//     {
//         var ProjectorB = new GameObject("PreviewT4M");
//         ProjectorB.AddComponent(typeof(Projector));
//         ProjectorB.hideFlags = HideFlags.HideInHierarchy;
//         
//         T4MPreview = ProjectorB.GetComponent(typeof(Projector)) as Projector;
//         
//         MeshFilter SizeOfGeo = CurrentSelect.GetComponent<MeshFilter>();
//         if (SizeOfGeo == null)
//         {
//             SizeOfGeo = CurrentSelect.GetComponent<T4MObjSC>().T4MMesh;
//         }
//         Vector2 MeshSize = new Vector2(SizeOfGeo.sharedMesh.bounds.size.x, SizeOfGeo.sharedMesh.bounds.size.z);
//         
//         T4MPreview.nearClipPlane = -20;
//         T4MPreview.farClipPlane = 20;
//         T4MPreview.orthographic = true;
//         T4MPreview.orthographicSize = (brushSize * CurrentSelect.localScale.x) * (MeshSize.x / 100);
//         T4MPreview.ignoreLayers = ~layerMask;
//         T4MPreview.transform.Rotate(90, -90, 0);
//         
//         Material NewPMat = new Material(Shader.Find("Hidden/PreviewT4M")); //\" { \n	Properties {\n _Transp (\"Transparency\", Range(0,1)) = 1 \n  _MainTex (\"Texture\", 2D) = \"\" { }\n	_MaskTex (\"Mask (RGB) Trans (A)\", 2D) = \"\" { TexGen ObjectLinear }\n	}\nSubShader {\n Pass {\nBlend SrcAlpha OneMinusSrcAlpha  \n SetTexture [_MainTex]  \n SetTexture [_MaskTex] {\n constantColor (1,1,1,[_Transp]) \n	combine previous , texture* constant\n	Matrix [_Projector]\n	}\n}\n}\n}");
//         T4MPreview.material = NewPMat;
//         T4MPreview.material.SetTexture("_MainTex", TexTexture[T4MselTexture]);
//         T4MPreview.material.SetTexture("_MaskTex", TexBrush[selBrush]);
//         
//         if (T4MselTexture == 0)
//         {
//             T4MPreview.material.SetTextureScale("_MainTex", Layer1Tile);
//             T4MPreview.material.SetTexture("_MainTex", CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat0") as Texture);
//         }
//         else if (T4MselTexture == 1)
//         {
//             T4MPreview.material.SetTextureScale("_MainTex", Layer2Tile);
//             T4MPreview.material.SetTexture("_MainTex", CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat1") as Texture);
//         }
//         else if (T4MselTexture == 2)
//         {
//             T4MPreview.material.SetTextureScale("_MainTex", Layer3Tile);
//             T4MPreview.material.SetTexture("_MainTex", CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat2") as Texture);
//         }
//         else if (T4MselTexture == 3)
//         {
//             T4MPreview.material.SetTextureScale("_MainTex", Layer4Tile);
//             T4MPreview.material.SetTexture("_MainTex", CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat3") as Texture);
//         }
//         else if (T4MselTexture == 4)
//         {
//             T4MPreview.material.SetTextureScale("_MainTex", Layer5Tile);
//             T4MPreview.material.SetTexture("_MainTex", CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat4") as Texture);
//         }
//         else if (T4MselTexture == 5)
//         {
//             T4MPreview.material.SetTextureScale("_MainTex", Layer6Tile);
//             T4MPreview.material.SetTexture("_MainTex", CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat5") as Texture);
//         }
//     }
//
//     public static Texture GetBrushBaseTexture(int brushIndex)
//     {
//         if (TexBrush != null && TexBrush.Length > 0 && brushIndex >= 0 && brushIndex < TexBrush.Length)
//         {
//             return TexBrush[brushIndex];
//             //T4MPreview.orthographicSize = (brushSize * CurrentSelect.localScale.x) * (temp.sharedMesh.bounds.size.x / 200);
//         }
//         else
//         {
//             return null;
//         }
//     }
//
//     public static Texture GetSelectedBrushBaseTexture()
//     {
//         if (TexBrush != null && TexBrush.Length > 0 && selBrush < TexBrush.Length)
//         {
//             return TexBrush[selBrush];
//         }
//         else
//         {
//             return null;
//         }
//     }
//
//     public static int GetSelectedBrushIndex()
//     {
//         return selBrush;
//     }
//
//     public static void SaveTexture()
//     {
//         string assetPath = AssetDatabase.GetAssetPath(T4MMaskTex);
//         
//         byte[] bytes = null;
//         try
//         {
//             bytes = T4MMaskTex.EncodeToPNG();
//         }
//         catch (System.Exception ex)
//         {
//             Debug.LogError($"将 {nameof(T4MMaskTex)} 遮罩编码为 PNG 时发生异常:\n{ex}");
//         }
//         
//         if (bytes == null)
//         {
//             return;
//         }
//
//         try
//         {
//             File.WriteAllBytes(assetPath, bytes);
//         }
//         catch (System.Exception ex)
//         {
//             Debug.LogError($"{nameof(T4MMaskTex)} 遮罩写入文件异常，优先检查是不是没有读写权限:\n{ex}");
//         }
//         
//         if (T4MMaskTex2)
//         {
//             string assetPath2 = AssetDatabase.GetAssetPath(T4MMaskTex2);
//             
//             byte[] bytes2 = null;
//             try
//             {
//                 bytes2 = T4MMaskTex2.EncodeToPNG();
//             }
//             catch (System.Exception ex)
//             {
//                 Debug.LogError($"将 {nameof(T4MMaskTex2)} 遮罩编码为 PNG 时发生异常:\n{ex}");
//             }
//
//             if (bytes2 == null)
//             {
//                 return;
//             }
//             
//             try
//             {
//                 File.WriteAllBytes(assetPath2, bytes2);
//             }
//             catch (System.Exception ex)
//             {
//                 Debug.LogError($"{nameof(T4MMaskTex2)} 遮罩写入文件异常，优先检查是不是没有读写权限:\n{ex}");
//             }
//         }
//         
//         // AssetDatabase.Refresh();
//     }
//
//     private void InitPincil()
//     {
//         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat5"))
//         {
//             TexTexture = new Texture[6];
//             TexTexture[0] = AssetPreview.GetAssetPreview(CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat0")) as Texture;
//             TexTexture[1] = AssetPreview.GetAssetPreview(CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat1")) as Texture;
//             TexTexture[2] = AssetPreview.GetAssetPreview(CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat2")) as Texture;
//             TexTexture[3] = AssetPreview.GetAssetPreview(CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat3")) as Texture;
//             TexTexture[4] = AssetPreview.GetAssetPreview(CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat4")) as Texture;
//             TexTexture[5] = AssetPreview.GetAssetPreview(CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat5")) as Texture;
//         }
//         else if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat4"))
//         {
//             TexTexture = new Texture[5];
//             TexTexture[0] = AssetPreview.GetAssetPreview(CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat0")) as Texture;
//             TexTexture[1] = AssetPreview.GetAssetPreview(CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat1")) as Texture;
//             TexTexture[2] = AssetPreview.GetAssetPreview(CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat2")) as Texture;
//             TexTexture[3] = AssetPreview.GetAssetPreview(CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat3")) as Texture;
//             TexTexture[4] = AssetPreview.GetAssetPreview(CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat4")) as Texture;
//         }
//         else if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat3"))
//         {
//             TexTexture = new Texture[4];
//             TexTexture[0] = AssetPreview.GetAssetPreview(CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat0")) as Texture;
//             TexTexture[1] = AssetPreview.GetAssetPreview(CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat1")) as Texture;
//             TexTexture[2] = AssetPreview.GetAssetPreview(CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat2")) as Texture;
//             TexTexture[3] = AssetPreview.GetAssetPreview(CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat3")) as Texture;
//         }
//         else if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat2"))
//         {
//             TexTexture = new Texture[3];
//             TexTexture[0] = AssetPreview.GetAssetPreview(CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat0")) as Texture;
//             TexTexture[1] = AssetPreview.GetAssetPreview(CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat1")) as Texture;
//             TexTexture[2] = AssetPreview.GetAssetPreview(CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat2")) as Texture;
//         }
//         else
//         {
//             TexTexture = new Texture[2];
//             TexTexture[0] = AssetPreview.GetAssetPreview(CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat0")) as Texture;
//             TexTexture[1] = AssetPreview.GetAssetPreview(CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat1")) as Texture;
//         }
//     }
//
//     void IniBrush()
//     {
//         List<Texture> BrushList = new List<Texture>();
//         Texture BrushesTL;
//         int BrushNum = 0;
//         do
//         {
//             BrushesTL = (Texture)AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Brushes/Brush" + BrushNum + ".png", typeof(Texture));
//             if (BrushesTL)
//             {
//                 BrushList.Add(BrushesTL);
//             }
//
//             BrushNum++;
//         } while (BrushesTL);
//
//         TexBrush = BrushList.ToArray();
//     }
//
//     void ConverterMenu()
//     {
//         if (vertexInfo == 0 && trisInfo == 0 && partofT4MObj == 0)
//         {
//             if ((CurrentSelect.GetComponent<Renderer>() || CurrentSelect.GetComponent<Terrain>() || NbrPartObj != null && NbrPartObj.Length != 0) && !CurrentSelect.GetComponent<T4MObjSC>() &&
//                 !CurrentSelect.GetComponent<T4MPartSC>())
//             {
//                 GUILayout.BeginHorizontal();
//                 GUILayout.FlexibleSpace();
//                 if (CurrentSelect.GetComponent<Renderer>() && !CurrentSelect.GetComponent<Terrain>() || NbrPartObj != null && NbrPartObj.Length != 0 && !CurrentSelect.GetComponent<Terrain>())
//                 {
//                     if (terrainDat)
//                         terrainDat = null;
//                     GUILayout.Label("模型兼容多层材质", EditorStyles.boldLabel);
//                 }
//                 else
//                 {
//                     if (!terrainDat && CurrentSelect.GetComponent<Terrain>())
//                         GetHeightmap();
//                     GUILayout.Label("Unity Terrain 转换为模型", EditorStyles.boldLabel);
//                 }
//
//                 GUILayout.FlexibleSpace();
//                 GUILayout.EndHorizontal();
//                 EditorGUILayout.Space();
//
//                 if (CurrentSelect.GetComponent<Renderer>() && !CurrentSelect.GetComponent<Terrain>() || NbrPartObj != null && NbrPartObj.Length != 0 && !CurrentSelect.GetComponent<Terrain>())
//                 {
//                     Renderer renderer = CurrentSelect.GetComponent<Renderer>();
//
//                     if (renderer == null)
//                     {
//                         return;
//                     }
//
//                     Material[] materials = renderer.sharedMaterials;
//                     bool isSupport = materials.Length <= 1;
//                     oldModelMaterial = materials[0];
//                     string oldMatPath = AssetDatabase.GetAssetPath(oldModelMaterial);
//                     bool isBuiltIn = oldMatPath.Contains("Packages/") || oldMatPath.Contains("Resources/unity_builtin");
//                     bool forceSelectNewPath = oldModelMaterial == null || isBuiltIn;
//
//                     if (isBuiltIn)
//                     {
//                         needCreateNewMat = true;
//                     }
//                     else
//                     {
//                         GUILayout.BeginHorizontal();
//                         GUILayout.Label("创建新材质", EditorStyles.boldLabel, GUILayout.Width(90));
//                         needCreateNewMat = EditorGUILayout.Toggle(needCreateNewMat, GUILayout.Width(53));
//                         GUILayout.EndHorizontal();
//                     }
//
//                     // 需要创建新材质
//                     if (needCreateNewMat)
//                     {
//                         GUILayout.BeginHorizontal();
//                         GUILayout.Label("材质名称：", EditorStyles.boldLabel);
//                         GUILayout.EndHorizontal();
//
//                         GUILayout.BeginHorizontal("box");
//                         GUILayout.Label("(不填将自动起名)");
//                         newMatName = GUILayout.TextField(newMatName, 50, GUILayout.Width(155));
//                         GUILayout.EndHorizontal();
//
//                         if (forceSelectNewPath)
//                         {
//                             // 找不到原材质，需要找个（选择）一个路径创建一个
//                             GUILayout.BeginHorizontal();
//                             GUILayout.Label("新材质路径：", EditorStyles.boldLabel, GUILayout.Width(330));
//                             GUILayout.EndHorizontal();
//
//                             GUILayout.BeginHorizontal();
//                             GUILayout.Label(newMatPath, EditorStyles.label, GUILayout.Width(305));
//                             if (GUILayout.Button("修改", GUILayout.Width(40)))
//                             {
//                                 newMatPath = EditorUtility.OpenFolderPanel("选择输出路径（只可以在 Assets 下）", Application.dataPath, "") + "/";
//                                 if (!string.IsNullOrEmpty(newMatPath))
//                                 {
//                                     int startIndex = Application.dataPath.Length - "Assets".Length;
//                                     int length = newMatPath.Length - startIndex;
//                                     newMatPath = newMatPath.Substring(startIndex, length);
//                                 }
//                             }
//
//                             GUILayout.EndHorizontal();
//                         }
//                         else
//                         {
//                             // 能找到，就在原材质所在路径创建一个
//                             GUILayout.BeginHorizontal();
//                             GUILayout.Label("新材质路径：", EditorStyles.boldLabel, GUILayout.Width(330));
//                             GUILayout.EndHorizontal();
//
//                             string oldMatName = oldModelMaterial.name + ".mat";
//
//                             if (string.IsNullOrEmpty(oldMatPath))
//                             {
//                                 // 能找到原材质，但找不到原材质的路径
//                                 GUILayout.BeginHorizontal();
//                                 GUILayout.Label(newMatPath, EditorStyles.label, GUILayout.Width(305));
//                                 if (GUILayout.Button("修改", GUILayout.Width(40)))
//                                 {
//                                     newMatPath = EditorUtility.OpenFolderPanel("选择新材质路径（只可以在 Assets 下）", Application.dataPath, "") + "/";
//                                     if (!string.IsNullOrEmpty(newMatPath))
//                                     {
//                                         int startIndex = Application.dataPath.Length - "Assets".Length;
//                                         int length = newMatPath.Length - startIndex;
//                                         newMatPath = newMatPath.Substring(startIndex, length);
//                                     }
//                                 }
//
//                                 GUILayout.EndHorizontal();
//                             }
//                             else
//                             {
//                                 // 能找到原材质、原材质的路径都可以找到
//                                 newMatPath = oldMatPath.Substring(0, oldMatPath.Length - oldMatName.Length);
//
//                                 GUILayout.BeginHorizontal();
//                                 GUILayout.Label(newMatPath, EditorStyles.label, GUILayout.Width(330));
//                                 GUILayout.EndHorizontal();
//                             }
//                         }
//                     }
//                 }
//                 else
//                 {
//                     GUILayout.Label("名字", EditorStyles.boldLabel);
//                     GUILayout.BeginHorizontal("box");
//
//                     GUILayout.Label("(不填默认使用原物体名)");
//                     terrainName = GUILayout.TextField(terrainName, 25, GUILayout.Width(155));
//                     GUILayout.EndHorizontal();
//
//                     GUILayout.BeginHorizontal();
//                     GUILayout.Label(keepTextureGUIContent, EditorStyles.boldLabel, GUILayout.Width(330));
//                     keepTexture = EditorGUILayout.Toggle(keepTexture, GUILayout.Width(53));
//                     GUILayout.EndHorizontal();
//
//                     GUILayout.BeginHorizontal();
//                     GUILayout.Label("输出路径：", EditorStyles.boldLabel, GUILayout.Width(330));
//                     GUILayout.EndHorizontal();
//
//                     GUILayout.BeginHorizontal();
//                     string pathLocal = EditorPrefs.GetString(T4MPrefabFolderEditorKey, T4MPrefabFolderDefaultValue);
//                     GUILayout.Label(pathLocal, EditorStyles.label, GUILayout.Width(305));
//                     if (GUILayout.Button("修改", GUILayout.Width(40)))
//                     {
//                         pathLocal = EditorUtility.OpenFolderPanel("选择 T4M 的输出路径（只可以在 Assets 下）", Application.dataPath, "T4MOBJ");
//                         if (!string.IsNullOrEmpty(pathLocal))
//                         {
//                             int startIndex = Application.dataPath.Length - "Assets".Length;
//                             int length = pathLocal.Length - startIndex;
//                             pathLocal = pathLocal.Substring(startIndex, length);
//                             EditorPrefs.SetString(T4MPrefabFolderEditorKey, pathLocal + "/");
//                         }
//                     }
//
//                     GUILayout.EndHorizontal();
//
//                     GUILayout.BeginHorizontal();
//                     GUILayout.Label("完成后删除场景中的 Unity Terrain", EditorStyles.boldLabel, GUILayout.Width(330));
//                     deleteOriginUnityTerrain = EditorGUILayout.Toggle(deleteOriginUnityTerrain, GUILayout.Width(53));
//                     GUILayout.EndHorizontal();
//
//                     if (!deleteOriginUnityTerrain)
//                     {
//                         GUILayout.BeginHorizontal();
//                         GUILayout.Label("完成后隐藏 Unity Terrain", EditorStyles.boldLabel, GUILayout.Width(330));
//                         hideOriginUnityTerrain = EditorGUILayout.Toggle(hideOriginUnityTerrain, GUILayout.Width(53));
//                         GUILayout.EndHorizontal();
//                     }
//
//                     GUILayout.BeginHorizontal();
//                     GUILayout.Label("层级", EditorStyles.boldLabel, GUILayout.Width(225));
//                     List<string> allLayers = new List<string>();
//                     for (int i = 0; i < 31; i++)
//                     {
//                         string layer = LayerMask.LayerToName(i);
//                         if (!string.IsNullOrEmpty(layer))
//                         {
//                             allLayers.Add(layer);
//                         }
//                     }
//
//                     targetT4MLayerIndex = EditorGUILayout.Popup(targetT4MLayerIndex, allLayers.ToArray(), GUILayout.Width(120));
//                     GUILayout.EndHorizontal();
//                 }
//
//                 if (CurrentSelect.GetComponent<Terrain>())
//                 {
//                     EditorGUILayout.Space();
//                     EditorGUILayout.Space();
//                     GUILayout.Label("转换模型质量", EditorStyles.boldLabel);
//                     GUILayout.BeginHorizontal();
//                     GUILayout.Label(" <");
//                     GUILayout.FlexibleSpace();
//                     T4MResolution = EditorGUILayout.IntField(T4MResolution, GUILayout.Width(30));
//                     GUILayout.Label("x " + T4MResolution + " : " + (T4MResolution * T4MResolution).ToString() + " 个顶点");
//                     GUILayout.FlexibleSpace();
//                     GUILayout.Label(" >");
//                     GUILayout.EndHorizontal();
//                     GUILayout.BeginHorizontal();
//                     GUILayout.FlexibleSpace();
//                     T4MResolution = (int)GUILayout.HorizontalScrollbar(T4MResolution, 0, 32, 350, GUILayout.Width(350));
//                     GUILayout.FlexibleSpace();
//                     GUILayout.EndHorizontal();
//                     EditorGUILayout.Space();
//                     EditorGUILayout.Space();
//                     tScale = (HeightmapWidth - 1f) / (T4MResolution - 1f);
//                     X = (int)((HeightmapWidth - 1) / tScale + 1);
//                     Y = (int)((HeightmapHeight - 1) / tScale + 1);
//                     EditorGUILayout.Space();
//                     EditorGUILayout.Space();
//                 }
//
//                 GUILayout.BeginVertical();
//                 GUILayout.FlexibleSpace();
//                 GUILayout.BeginHorizontal();
//                 GUILayout.FlexibleSpace();
//                 if (GUILayout.Button(new GUIContent("开始处理", "可能需要花费一定时间"), GUILayout.Width(100), GUILayout.Height(30)))
//                 {
//                     if (CurrentSelect.GetComponent<Renderer>() && !CurrentSelect.GetComponent<Terrain>() || NbrPartObj != null && NbrPartObj.Length != 0 && !CurrentSelect.GetComponent<Terrain>())
//                     {
//                         Obj2T4M();
//                     }
//                     else
//                     {
//                         ConvertUTerrain();
//                     }
//                 }
//
//                 GUILayout.FlexibleSpace();
//                 GUILayout.EndHorizontal();
//                 GUILayout.Space(50);
//                 GUILayout.EndVertical();
//             }
//             else
//             {
//                 terrainDat = null;
//                 GUILayout.BeginHorizontal();
//                 GUILayout.FlexibleSpace();
//                 if (CurrentSelect.GetComponent<T4MObjSC>())
//                     GUILayout.Label("当前模型已进行过转换！", EditorStyles.boldLabel);
//                 else GUILayout.Label("错误，当前模型无法转换！", EditorStyles.boldLabel);
//                 GUILayout.FlexibleSpace();
//                 GUILayout.EndHorizontal();
//             }
//         }
//         else
//         {
//             GUILayout.Label("T4M Final Resolution : ", EditorStyles.boldLabel);
//             if (partofT4MObj > 1)
//                 GUILayout.Label("Vertex : ~" + vertexInfo + " in " + partofT4MObj + " Parts");
//             else GUILayout.Label("Vertex : " + vertexInfo + " in " + partofT4MObj + " Part");
//             GUILayout.Label("Triangle : " + trisInfo);
//             EditorGUILayout.Space();
//             EditorGUILayout.Space();
//             GUILayout.BeginVertical("Box");
//             GUILayout.Label("Since Unity 3.5, some converted objects can be ", EditorStyles.boldLabel);
//             GUILayout.Label("no smooth : ", EditorStyles.boldLabel);
//             EditorGUILayout.Space();
//             GUILayout.Label("Select the New Mesh in the Project window :");
//             GUILayout.Label("in T4MOBJ/Meshes/\"yourobject\"");
//             EditorGUILayout.Space();
//             GUILayout.Label("In Inspector window :");
//             GUILayout.Label("Descrease \"Smoothing Angle\", Increase again to 180");
//             GUILayout.Label("And \"Apply\"");
//             EditorGUILayout.Space();
//             GUILayout.Label("Now Select your Object on the scene :");
//             GUILayout.Label("Uncheck/check the box the \"Mesh Collider\" in ");
//             GUILayout.Label("Inspector window");
//             GUILayout.EndVertical();
//             EditorGUILayout.Space();
//             EditorGUILayout.Space();
//             if (GUILayout.Button("Keep my Conversion and Destroy Original"))
//             {
//                 DestroyImmediate(CurrentSelect.gameObject);
//                 Selection.activeTransform = Child.transform;
//                 vertexInfo = 0;
//                 trisInfo = 0;
//                 partofT4MObj = 0;
//                 T4MMenuToolbar = 1;
//             }
//
//             if (GUILayout.Button("Modify Options and Start a New Conversion"))
//             {
//                 DestroyImmediate(Child);
//
//                 string T4MPrefabFolder = EditorPrefs.GetString(T4MPrefabFolderEditorKey, T4MPrefabFolderDefaultValue);
//                 AssetDatabase.DeleteAsset(T4MPrefabFolder + "Terrains/Meshes/" + FinalExpName + ".obj");
//                 AssetDatabase.DeleteAsset(T4MPrefabFolder + "Terrains/" + FinalExpName + ".prefab");
//                 AssetDatabase.DeleteAsset(T4MPrefabFolder + "Terrains/Texture/" + FinalExpName + ".png");
//                 AssetDatabase.DeleteAsset(T4MPrefabFolder + "Terrains/Material/" + FinalExpName + ".mat");
//                 CurrentSelect.GetComponent<Terrain>().enabled = true;
//                 vertexInfo = 0;
//                 trisInfo = 0;
//                 partofT4MObj = 0;
//                 UnityTerrain = null;
//                 terrainDat = null;
//             }
//
//             if (GUILayout.Button("Keep Both and Continue"))
//             {
//                 UnityTerrain.SetActive(false);
//                 UnityTerrain = null;
//                 Selection.activeTransform = Child.transform;
//                 vertexInfo = 0;
//                 trisInfo = 0;
//                 partofT4MObj = 0;
//                 T4MMenuToolbar = 1;
//             }
//         }
//     }
//
//     void PainterMenu()
//     {
//         if (CurrentSelect.GetComponent<T4MObjSC>() != null)
//         {
//             GUILayout.BeginHorizontal();
//             GUILayout.FlexibleSpace();
//             MyT4MV = GUILayout.Toolbar(MyT4MV, MyT4MPaintMenu, GUILayout.Width(290), GUILayout.Height(20));
//             GUILayout.FlexibleSpace();
//             GUILayout.EndHorizontal();
//             switch (MyT4MV)
//             {
//                 case 0:
//                     if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.shader != Shader.Find("T4MShaders/ShaderModel2/Unlit/T4M World Projection Shader + LM") &&
//                         CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.shader != Shader.Find("T4MShaders/ShaderModel2/Diffuse/T4M World Projection Shader") &&
//                         CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.shader != Shader.Find("T4MShaders/ShaderModel2/MobileLM/T4M World Projection Shader_Mobile") &&
//                         !CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Tiling"))
//                     {
//                         PixelPainterMenu();
//                     }
//                     else ProjectionWorldConfig();
//
//                     break;
//                 case 1:
//                     if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial && CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat0") &&
//                         CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat1") && CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Control"))
//                     {
//                         EditorGUILayout.Space();
//                         InitPincil();
//                         GUILayout.BeginVertical("box", GUILayout.Width(310));
//                         GUILayout.Label("添加、替换材质层", EditorStyles.boldLabel);
//                         selProcedural = GUILayout.SelectionGrid(selProcedural, TexTexture, 6, "gridlist", GUILayout.Width(340), GUILayout.Height(58));
//
//                         EditorGUILayout.BeginHorizontal();
//                         if (GUILayout.Button((Texture)AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/up.png", typeof(Texture)), GUILayout.Width(54)))
//                         {
//                             if (!PreceduralAdd && !MaterialAdd && Precedural)
//                                 PreceduralAdd = Precedural;
//
//                             if (PreceduralAdd)
//                             {
//                                 CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Splat0", PreceduralAdd.GetTexture("_MainTex"));
//                                 if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_BumpSplat0"))
//                                     CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_BumpSplat0", PreceduralAdd.GetTexture("_BumpMap"));
//                             }
//                             else if (MaterialAdd)
//                             {
//                                 CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Splat0", MaterialAdd);
//                             }
//
//                             selProcedural = 0;
//                             PreceduralAdd = null;
//                             MaterialAdd = null;
//                             IniNewSelect();
//                         }
//
//                         if (GUILayout.Button((Texture)AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/up.png", typeof(Texture)), GUILayout.Width(54)))
//                         {
//                             if (!PreceduralAdd && !MaterialAdd && Precedural)
//                                 PreceduralAdd = Precedural;
//                             if (PreceduralAdd)
//                             {
//                                 CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Splat1", PreceduralAdd.GetTexture("_MainTex"));
//                                 if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_BumpSplat1"))
//                                     CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_BumpSplat1", PreceduralAdd.GetTexture("_BumpMap"));
//                             }
//                             else if (MaterialAdd)
//                             {
//                                 CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Splat1", MaterialAdd);
//                             }
//
//                             selProcedural = 1;
//                             PreceduralAdd = null;
//                             MaterialAdd = null;
//                             IniNewSelect();
//                         }
//
//                         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat2"))
//                             if (GUILayout.Button((Texture)AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/up.png", typeof(Texture)), GUILayout.Width(54)))
//                             {
//                                 if (!PreceduralAdd && !MaterialAdd && Precedural)
//                                     PreceduralAdd = Precedural;
//                                 if (PreceduralAdd)
//                                 {
//                                     CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Splat2", PreceduralAdd.GetTexture("_MainTex"));
//                                     if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_BumpSplat2"))
//                                         CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_BumpSplat2", PreceduralAdd.GetTexture("_BumpMap"));
//                                 }
//                                 else if (MaterialAdd)
//                                 {
//                                     CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Splat2", MaterialAdd);
//                                 }
//
//                                 selProcedural = 2;
//                                 PreceduralAdd = null;
//                                 MaterialAdd = null;
//                                 IniNewSelect();
//                             }
//
//                         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat3"))
//                             if (GUILayout.Button((Texture)AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/up.png", typeof(Texture)), GUILayout.Width(54)))
//                             {
//                                 if (!PreceduralAdd && !MaterialAdd && Precedural)
//                                     PreceduralAdd = Precedural;
//                                 if (PreceduralAdd)
//                                 {
//                                     CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Splat3", PreceduralAdd.GetTexture("_MainTex"));
//                                     if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_BumpSplat3"))
//                                         CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_BumpSplat3", PreceduralAdd.GetTexture("_BumpMap"));
//                                 }
//                                 else if (MaterialAdd)
//                                 {
//                                     CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Splat3", MaterialAdd);
//                                 }
//
//                                 selProcedural = 3;
//                                 PreceduralAdd = null;
//                                 MaterialAdd = null;
//                                 IniNewSelect();
//                             }
//
//                         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat4"))
//                             if (GUILayout.Button((Texture)AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/up.png", typeof(Texture)), GUILayout.Width(54)))
//                             {
//                                 if (!PreceduralAdd && !MaterialAdd && Precedural)
//                                     PreceduralAdd = Precedural;
//
//                                 if (PreceduralAdd)
//                                 {
//                                     CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Splat4", PreceduralAdd.GetTexture("_MainTex"));
//                                 }
//                                 else if (MaterialAdd)
//                                 {
//                                     CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Splat4", MaterialAdd);
//                                 }
//
//                                 selProcedural = 4;
//                                 PreceduralAdd = null;
//                                 MaterialAdd = null;
//                                 IniNewSelect();
//                             }
//
//                         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat5"))
//                             if (GUILayout.Button((Texture)AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/up.png", typeof(Texture)), GUILayout.Width(54)))
//                             {
//                                 if (!PreceduralAdd && !MaterialAdd && Precedural)
//                                     PreceduralAdd = Precedural;
//
//                                 if (PreceduralAdd)
//                                 {
//                                     CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Splat5", PreceduralAdd.GetTexture("_MainTex"));
//                                 }
//                                 else if (MaterialAdd)
//                                 {
//                                     CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Splat5", MaterialAdd);
//                                 }
//
//                                 selProcedural = 5;
//                                 PreceduralAdd = null;
//                                 MaterialAdd = null;
//                                 IniNewSelect();
//                             }
//
//                         EditorGUILayout.EndHorizontal();
//
//
//                         string AssetName = AssetDatabase.GetAssetPath(CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat" + selProcedural)) as string;
//
//
//                         SubstanceImporter SubstanceI = AssetImporter.GetAtPath(AssetName) as SubstanceImporter;
//
//                         if (SubstanceI)
//                         {
//                             Material[] ProcMat = SubstanceI.GetMaterials() as Material[];
//
//                             for (int i = 0; i < ProcMat.Length; i++)
//                             {
//                                 if (ProcMat[i].name + "_Diffuse" == CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat" + selProcedural).name)
//                                 {
//                                     Precedural = ProcMat[i];
//                                     //SubstanceI.SetTextureAlphaSource(Precedural, Precedural.name+"_Diffuse", ProceduralOutputType.Diffuse);
//                                 }
//                             }
//                         }
//                         else Precedural = null;
//
//                         EditorGUILayout.Space();
//                         EditorGUILayout.Space();
//
//
//                         //MaterialTyp = (MaterialType)EditorGUILayout.EnumPopup("Material Type", MaterialTyp, GUILayout.Width(340));
//                         MaterialTyp = MaterialType.Classic;
//                         EditorGUILayout.BeginHorizontal();
//
//                         if (MaterialTyp != MaterialType.Classic)
//                         {
//                             GUILayout.Label("Substances To Add : ");
//                             MaterialAdd = null;
//                             PreceduralAdd = EditorGUILayout.ObjectField(PreceduralAdd, typeof(Material), true, GUILayout.Width(220)) as Material;
//                         }
//                         else
//                         {
//                             GUILayout.Label("Texture To Add : ");
//                             PreceduralAdd = null;
//                             MaterialAdd = EditorGUILayout.ObjectField(MaterialAdd, typeof(Texture2D), true, GUILayout.Width(220)) as Texture;
//                         }
//
//
//                         GUILayout.FlexibleSpace();
//
//                         EditorGUILayout.EndVertical();
//                         EditorGUILayout.Space();
//                         EditorGUILayout.EndHorizontal();
//
//                         EditorGUILayout.Space();
//
//                         if (Precedural)
//                         {
//                             EditorGUILayout.BeginVertical("box");
//                             GUILayout.Label("修改", EditorStyles.boldLabel);
//                             EditorGUILayout.BeginHorizontal();
//                             scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(350), GUILayout.Height(296));
//                             //Substance();
//                             EditorGUILayout.EndScrollView();
//                             EditorGUILayout.EndHorizontal();
//                             EditorGUILayout.EndVertical();
//                         }
//                         else
//                         {
//                             ClassicMat();
//                         }
//                     }
//
//                     break;
//             }
//         }
//         else
//         {
//             GUILayout.BeginHorizontal();
//             GUILayout.FlexibleSpace();
//             GUILayout.Label("Please, select the T4M Object", EditorStyles.boldLabel);
//             GUILayout.FlexibleSpace();
//             GUILayout.EndHorizontal();
//         }
//     }
//
//     void ClassicMat()
//     {
//         GUILayout.BeginVertical("Box");
//         GUILayout.Label("修改材质层贴图", EditorStyles.boldLabel);
//         EditorGUILayout.Space();
//         GUILayout.BeginHorizontal();
//         if (selProcedural == 0)
//         {
//             if (Layer1)
//             {
//                 GUILayout.Label((Texture)AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/TDiff.jpg", typeof(Texture)));
//                 Layer1 = EditorGUILayout.ObjectField(Layer1, typeof(Texture2D), true, GUILayout.Width(75), GUILayout.Height(75)) as Texture;
//                 CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Splat0", Layer1);
//                 if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Normal0"))
//                 {
//                     GUILayout.Label((Texture)AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/TBump.jpg", typeof(Texture)));
//                     Layer1Bump = EditorGUILayout.ObjectField(Layer1Bump, typeof(Texture2D), true, GUILayout.Width(75), GUILayout.Height(75)) as Texture;
//                     CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Normal0", Layer1Bump);
//                 }
//
//                 if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Mask0"))
//                 {
//                     GUILayout.Label((Texture)AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/TROME.jpg", typeof(Texture)));
//                     Layer1Mask = EditorGUILayout.ObjectField(Layer1Mask, typeof(Texture2D), true, GUILayout.Width(75), GUILayout.Height(75)) as Texture;
//                     CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Mask0", Layer1Mask);
//                 }
//             }
//         }
//         else if (selProcedural == 1)
//         {
//             if (Layer2)
//             {
//                 GUILayout.Label((Texture)AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/TDiff.jpg", typeof(Texture)));
//                 Layer2 = EditorGUILayout.ObjectField(Layer2, typeof(Texture2D), true, GUILayout.Width(75), GUILayout.Height(75)) as Texture;
//                 CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Splat1", Layer2);
//                 if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Normal1"))
//                 {
//                     GUILayout.Label((Texture)AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/TBump.jpg", typeof(Texture)));
//                     Layer2Bump = EditorGUILayout.ObjectField(Layer2Bump, typeof(Texture2D), true, GUILayout.Width(75), GUILayout.Height(75)) as Texture;
//                     CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Normal1", Layer2Bump);
//                 }
//
//                 if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Mask1"))
//                 {
//                     GUILayout.Label((Texture)AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/TROME.jpg", typeof(Texture)));
//                     Layer2Mask = EditorGUILayout.ObjectField(Layer2Mask, typeof(Texture2D), true, GUILayout.Width(75), GUILayout.Height(75)) as Texture;
//                     CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Mask1", Layer2Mask);
//                 }
//             }
//         }
//         else if (selProcedural == 2)
//         {
//             if (Layer3)
//             {
//                 GUILayout.Label((Texture)AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/TDiff.jpg", typeof(Texture)));
//                 Layer3 = EditorGUILayout.ObjectField(Layer3, typeof(Texture2D), true, GUILayout.Width(75), GUILayout.Height(75)) as Texture;
//                 CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Splat2", Layer3);
//                 if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Normal2"))
//                 {
//                     GUILayout.Label((Texture)AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/TBump.jpg", typeof(Texture)));
//                     Layer3Bump = EditorGUILayout.ObjectField(Layer3Bump, typeof(Texture2D), true, GUILayout.Width(75), GUILayout.Height(75)) as Texture;
//                     CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Normal2", Layer3Bump);
//                 }
//
//                 if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Mask2"))
//                 {
//                     GUILayout.Label((Texture)AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/TROME.jpg", typeof(Texture)));
//                     Layer3Mask = EditorGUILayout.ObjectField(Layer3Mask, typeof(Texture2D), true, GUILayout.Width(75), GUILayout.Height(75)) as Texture;
//                     CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Mask2", Layer3Mask);
//                 }
//             }
//         }
//         else if (selProcedural == 3)
//         {
//             if (Layer4)
//             {
//                 GUILayout.Label((Texture)AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/TDiff.jpg", typeof(Texture)));
//                 Layer4 = EditorGUILayout.ObjectField(Layer4, typeof(Texture2D), true, GUILayout.Width(75), GUILayout.Height(75)) as Texture;
//                 CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Splat3", Layer4);
//                 if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Normal3"))
//                 {
//                     GUILayout.Label((Texture)AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/TBump.jpg", typeof(Texture)));
//                     Layer4Bump = EditorGUILayout.ObjectField(Layer4Bump, typeof(Texture2D), true, GUILayout.Width(75), GUILayout.Height(75)) as Texture;
//                     CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Normal3", Layer4Bump);
//                 }
//
//                 if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Mask3"))
//                 {
//                     GUILayout.Label((Texture)AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/TROME.jpg", typeof(Texture)));
//                     Layer4Mask = EditorGUILayout.ObjectField(Layer4Mask, typeof(Texture2D), true, GUILayout.Width(75), GUILayout.Height(75)) as Texture;
//                     CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Mask3", Layer4Mask);
//                 }
//             }
//         }
//         else if (selProcedural == 4)
//         {
//             if (Layer5)
//             {
//                 GUILayout.Label("Modify", EditorStyles.boldLabel);
//                 GUILayout.BeginHorizontal("Box");
//                 GUILayout.Label((Texture)AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/TDiff.jpg", typeof(Texture)));
//                 Layer5 = EditorGUILayout.ObjectField(Layer5, typeof(Texture2D), true, GUILayout.Width(75), GUILayout.Height(75)) as Texture;
//                 CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Splat4", Layer5);
//                 GUILayout.FlexibleSpace();
//                 GUILayout.EndHorizontal();
//             }
//         }
//         else if (selProcedural == 5)
//         {
//             if (Layer6)
//             {
//                 GUILayout.Label("Modify", EditorStyles.boldLabel);
//                 GUILayout.BeginHorizontal("Box");
//                 GUILayout.Label((Texture)AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/TDiff.jpg", typeof(Texture)));
//                 Layer6 = EditorGUILayout.ObjectField(Layer6, typeof(Texture2D), true, GUILayout.Width(75), GUILayout.Height(75)) as Texture;
//                 CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Splat5", Layer6);
//             }
//         }
//
//         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.shader == Shader.Find("T4MShaders/ShaderModel1/T4M 2 Textures ManualAdd BeastLM_1DC") ||
//             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.shader == Shader.Find("T4MShaders/ShaderModel1/T4M 2 Textures ManualAdd CustoLM 1DC"))
//         {
//             GUILayout.Label("Manual Lightmap Add", EditorStyles.boldLabel);
//             GUILayout.BeginHorizontal("Box");
//             GUILayout.Label((Texture)AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/TLM.jpg", typeof(Texture)));
//             LMMan = EditorGUILayout.ObjectField(LMMan, typeof(Texture2D), true, GUILayout.Width(75), GUILayout.Height(75)) as Texture;
//             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Lightmap", LMMan);
//             GUILayout.FlexibleSpace();
//             GUILayout.EndHorizontal();
//         }
//
//         GUILayout.FlexibleSpace();
//         GUILayout.EndHorizontal();
//
//         EditorGUILayout.Space();
//         GUILayout.EndVertical();
//     }
//
//     void ProjectionWorldConfig()
//     {
//         if (UpSideTile.x != UpSideTile.y && joinTiles == true || UpSideTile.z != UpSideTile.w && joinTiles == true)
//         {
//             joinTiles = false;
//         }
//
//         EditorGUILayout.Space();
//         GUILayout.Label("Painting Menu is not available for this shader", EditorStyles.boldLabel);
//         EditorGUILayout.Space();
//         EditorGUILayout.Space();
//         GUILayout.Label("World Projection Shaders Options", EditorStyles.boldLabel);
//         EditorGUILayout.Space();
//         UpSideF = EditorGUILayout.Slider("UP/SIDES Fighting :", UpSideF, 0, 10);
//         BlendFac = EditorGUILayout.Slider("Blend Factor :", BlendFac, 0, 20);
//         EditorGUILayout.Space();
//         EditorGUILayout.Space();
//         joinTiles = EditorGUILayout.Toggle("Tiling : Join X/Y", joinTiles);
//         EditorGUILayout.Space();
//         if (joinTiles)
//         {
//             UpSideTile.x = UpSideTile.y = EditorGUILayout.Slider("Up Texture Tiling :", UpSideTile.x, 0.01f, 10);
//             UpSideTile.z = UpSideTile.w = EditorGUILayout.Slider("Side Tecture Tiling :", UpSideTile.z, 0.01f, 10);
//         }
//         else
//         {
//             UpSideTile.x = EditorGUILayout.Slider("Up Texture Tiling X:", UpSideTile.x, 0.01f, 2);
//             UpSideTile.y = EditorGUILayout.Slider("Up Texture Tiling Y:", UpSideTile.y, 0.01f, 2);
//             UpSideTile.z = EditorGUILayout.Slider("Side Tecture Tiling X:", UpSideTile.z, 0.01f, 2);
//             UpSideTile.w = EditorGUILayout.Slider("Side Tecture Tiling Y:", UpSideTile.w, 0.01f, 2);
//         }
//
//         CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetVector("_Tiling", new Vector4(UpSideTile.x, UpSideTile.y, UpSideTile.z, UpSideTile.w));
//         CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetFloat("_UpSide", UpSideF);
//         CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetFloat("_Blend", BlendFac);
//     }
//
//     void MyT4M()
//     {
//         if (CurrentSelect.GetComponent(typeof(T4MObjSC)))
//         {
//             GUILayout.BeginHorizontal();
//             GUILayout.FlexibleSpace();
//             GUILayout.FlexibleSpace();
//             GUILayout.EndHorizontal();
//
//             GUILayout.Label("场景清理", EditorStyles.boldLabel);
//             GUILayout.BeginHorizontal();
//             GUILayout.FlexibleSpace();
//             if (GUILayout.Button("开始清理", GUILayout.Width(200), GUILayout.Height(20)))
//             {
//                 MeshRenderer[] prev = GameObject.FindObjectsOfType(typeof(MeshRenderer)) as MeshRenderer[];
//                 foreach (MeshRenderer go in prev)
//                 {
//                     if (go.hideFlags == HideFlags.HideInHierarchy)
//                     {
//                         go.hideFlags = 0;
//                         DestroyImmediate(go.gameObject);
//                     }
//                 }
//
//                 EditorUtility.DisplayDialog("Scene Cleaned", "", "OK");
//             }
//
//             GUILayout.FlexibleSpace();
//             GUILayout.EndHorizontal();
//             EditorGUILayout.Space();
//
//
//             GUILayout.Label("材质着色器设置", EditorStyles.boldLabel);
//             EditorGUI.BeginChangeCheck();
//             //lightModel = (LightModel)EditorGUILayout.EnumPopup("光照计算方式", lightModel, GUILayout.Width(340));
//             //brushCountNumIdx = EditorGUILayout.Popup("材质支持笔刷层数", brushCountNumIdx, brushCountNumStr, GUILayout.Width(340));
//             //brushCount = (BrushCount)brushCountNumIdx;
//
//             Material mat = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial;
//             bool hasUseUV4 = mat.HasProperty("_UseUV4Toggle");
//             if (hasUseUV4)
//             {
//                 float useUV4Float = mat.GetFloat("_UseUV4Toggle");
//                 useUV4 = useUV4Float >= 1f;
//             }
//             using (new EditorGUI.DisabledScope(!hasUseUV4))
//             {
//                 useUV4 = EditorGUILayout.Toggle("使用第四套 UV", useUV4);
//             }
//
//             if (EditorGUI.EndChangeCheck())
//             {
//                 MyT4MApplyChange();
//             }
//
//             EditorGUILayout.Space();
//             //GUILayout.BeginHorizontal();
//             //GUILayout.FlexibleSpace();
//             //if (GUILayout.Button("应用修改", GUILayout.Width(100), GUILayout.Height(25)))
//             //{
//             //    MyT4MApplyChange();
//             //}
//             //GUILayout.FlexibleSpace();
//             //GUILayout.EndHorizontal();
//             GUILayout.FlexibleSpace();
//         }
//         else
//         {
//             GUILayout.BeginHorizontal();
//             GUILayout.FlexibleSpace();
//             GUILayout.Label("请选择转换过的对象进行修改.", EditorStyles.boldLabel);
//             GUILayout.FlexibleSpace();
//             GUILayout.EndHorizontal();
//         }
//     }
//
//     void MyT4MApplyChange()
//     {
//         string T4MPrefabFolder = EditorPrefs.GetString(T4MPrefabFolderEditorKey, T4MPrefabFolderDefaultValue);
//
//         //if (lightModel == LightModel.Diffuse)
//         //{
//         //    switch (brushCount)
//         //    {
//         //        case BrushCount.Two:
//         //            CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.shader = Shader.Find("Hidden/T4M Shaders/Shader Model 2/Diffuse/Diffuse 2 Textures");
//         //            break;
//         //        case BrushCount.Three:
//         //            CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.shader = Shader.Find("Hidden/T4M Shaders/Shader Model 2/Diffuse/Diffuse 3 Textures");
//         //            break;
//         //        default:
//         //        case BrushCount.Four:
//         //            CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.shader = Shader.Find("Hidden/T4M Shaders/Shader Model 2/Diffuse/Diffuse 4 Textures");
//         //            break;
//         //    }
//         //}
//         //else if (lightModel == LightModel.Lit)
//         //{
//         //switch (brushCount)
//         //{
//         //    case BrushCount.Two:
//         //    CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.shader = Shader.Find("Hidden/T4M Shaders/Shader Model 3/Lit/Lit 2 Textures");
//         //    break;
//         //case BrushCount.Three:
//         //    CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.shader = Shader.Find("Hidden/T4M Shaders/Shader Model 3/Lit/Lit 3 Textures");
//         //    break;
//         //default:
//         //    case BrushCount.Four:
//         CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.shader = Shader.Find(defaultShaderName);
//         //        break;
//         //}
//         //}
//
//         Material mat = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial;
//         mat.SetFloat("_UseUV4Toggle", useUV4 ? 1f : 0f);
//         if (mat.GetFloat("_UseUV4Toggle") >= 1f)
//         {
//             mat.EnableKeyword("USE_UV4_SAMPLE_CONTROL_MAP");
//         }
//         else
//         {
//             mat.DisableKeyword("USE_UV4_SAMPLE_CONTROL_MAP");
//         }
//
//         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Control2"))
//         {
//             Texture Control2;
//             if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Control") != null)
//             {
//                 Control2 = (Texture)AssetDatabase.LoadAssetAtPath(
//                     T4MPrefabFolder + "Terrains/Texture/" + CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Control").name + "C2.png", typeof(Texture));
//             }
//             else Control2 = (Texture)AssetDatabase.LoadAssetAtPath(T4MPrefabFolder + "Terrains/Texture/" + CurrentSelect.gameObject.name + "C2.png", typeof(Texture));
//
//             if (Control2 == null)
//                 CreateControl2Text();
//             else CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Control2", Control2);
//         }
//
//
//         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat0"))
//             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Splat0", Layer1);
//
//         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat1"))
//             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Splat1", Layer2);
//
//         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat2"))
//         {
//             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Splat2", Layer3);
//         }
//
//         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat3"))
//         {
//             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Splat3", Layer4);
//         }
//
//         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat4"))
//         {
//             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Splat4", Layer5);
//         }
//
//         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat5"))
//         {
//             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Splat5", Layer6);
//         }
//
//         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Normal0"))
//         {
//             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Normal0", Layer1Bump);
//         }
//
//         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Normal1"))
//         {
//             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Normal1", Layer2Bump);
//         }
//
//         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Normal2"))
//         {
//             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Normal2", Layer3Bump);
//         }
//
//         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Normal3"))
//         {
//             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Normal3", Layer4Bump);
//         }
//
//         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Mask0"))
//         {
//             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Mask0", Layer1Mask);
//         }
//
//         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Mask1"))
//         {
//             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Mask1", Layer2Mask);
//         }
//
//         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Mask2"))
//         {
//             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Mask2", Layer3Mask);
//         }
//
//         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Mask3"))
//         {
//             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Mask3", Layer4Mask);
//         }
//         //if (T4MMaster)
//         //{
//         //    CurrentSelect.GetComponent<T4MObjSC>().EnabledLODSystem = ActivatedLOD;
//         //    CurrentSelect.GetComponent<T4MObjSC>().enabledBillboard = ActivatedBillboard;
//         //    CurrentSelect.GetComponent<T4MObjSC>().enabledLayerCul = ActivatedLayerCul;
//         //    CurrentSelect.GetComponent<T4MObjSC>().CloseView = CloseDistMaxView;
//         //    CurrentSelect.GetComponent<T4MObjSC>().NormalView = NormalDistMaxView;
//         //    CurrentSelect.GetComponent<T4MObjSC>().FarView = FarDistMaxView;
//         //    CurrentSelect.GetComponent<T4MObjSC>().BackGroundView = BGDistMaxView;
//         //    CurrentSelect.GetComponent<T4MObjSC>().Master = 1;
//         //    CurrentSelect.GetComponent<T4MObjSC>().PlayerCamera = PlayerCam;
//         //    PrefabUtility.RecordPrefabInstancePropertyModifications(CurrentSelect.gameObject.GetComponent<T4MObjSC>());
//         //}
//         //else
//         //{
//         //    CurrentSelect.GetComponent<T4MObjSC>().EnabledLODSystem = false;
//         //    CurrentSelect.GetComponent<T4MObjSC>().enabledBillboard = false;
//         //    CurrentSelect.GetComponent<T4MObjSC>().enabledLayerCul = false;
//         //    CurrentSelect.GetComponent<T4MObjSC>().Master = 0;
//
//         //    T4MLodObjSC[] T4MLodObjGet = GameObject.FindObjectsOfType(typeof(T4MLodObjSC)) as T4MLodObjSC[];
//         //    for (var i = 0; i < T4MLodObjGet.Length; i++)
//         //    {
//         //        T4MLodObjGet[i].LOD2.enabled = T4MLodObjGet[i].LOD3.enabled = false;
//         //        T4MLodObjGet[i].LOD1.enabled = true;
//         //        if (LODModeControler == LODMod.Mass_Control)
//         //            T4MLodObjGet[i].Mode = 0;
//         //        else if (LODModeControler == LODMod.Independent_Control)
//         //            T4MLodObjGet[i].Mode = 0;
//         //        PrefabUtility.RecordPrefabInstancePropertyModifications(T4MLodObjGet[i].GetComponent<T4MLodObjSC>());
//         //    }
//         //    CurrentSelect.gameObject.GetComponent<T4MObjSC>().ObjLodScript = new T4MLodObjSC[0];
//         //    CurrentSelect.gameObject.GetComponent<T4MObjSC>().ObjPosition = new Vector3[0];
//         //    CurrentSelect.gameObject.GetComponent<T4MObjSC>().ObjLodStatus = new int[0];
//         //    CurrentSelect.gameObject.GetComponent<T4MObjSC>().Mode = 0;
//
//         //    T4MBillBObjSC[] T4MBillObjGet = GameObject.FindObjectsOfType(typeof(T4MBillBObjSC)) as T4MBillBObjSC[];
//         //    for (var i = 0; i < T4MBillObjGet.Length; i++)
//         //    {
//         //        T4MBillObjGet[i].Render.enabled = true;
//         //        T4MBillObjGet[i].Transf.LookAt(Vector3.zero, Vector3.up);
//         //    }
//         //    CurrentSelect.gameObject.GetComponent<T4MObjSC>().BillboardPosition = new Vector3[0];
//         //    CurrentSelect.gameObject.GetComponent<T4MObjSC>().BillStatus = new int[0];
//         //    CurrentSelect.gameObject.GetComponent<T4MObjSC>().BillScript = new T4MBillBObjSC[0];
//         //    PrefabUtility.RecordPrefabInstancePropertyModifications(CurrentSelect.gameObject.GetComponent<T4MObjSC>());
//         //}
//         TexTexture = null;
//         IniNewSelect();
//     }
//
//     void CreateControl2Text()
//     {
//         string T4MPrefabFolder = EditorPrefs.GetString(T4MPrefabFolderEditorKey, T4MPrefabFolderDefaultValue);
//         Texture2D Control2 = new Texture2D(512, 512, TextureFormat.ARGB32, true);
//         Color[] ColorBase = new Color[512 * 512];
//         for (var t = 0; t < ColorBase.Length; t++)
//         {
//             ColorBase[t] = new Color(1, 0, 0, 0);
//         }
//
//         Control2.SetPixels(ColorBase);
//         string path;
//         if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Control") != null)
//         {
//             path = T4MPrefabFolder + "Terrains/Texture/" + CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Control").name + "C2.png";
//         }
//         else path = T4MPrefabFolder + "Terrains/Texture/" + CurrentSelect.gameObject.name + "C2.png";
//
//         byte[] data = Control2.EncodeToPNG();
//         File.WriteAllBytes(path, data);
//         AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
//
//         TextureImporter TextureI = AssetImporter.GetAtPath(path) as TextureImporter;
//
//         TextureI.textureType = TextureImporterType.Default;
//         TextureImporterPlatformSettings tSetting = new TextureImporterPlatformSettings();
//         tSetting.overridden = true;
//         tSetting.format = TextureImporterFormat.RGBA32;
//         TextureI.SetPlatformTextureSettings(tSetting);
//         //TextureI.textureFormat = TextureImporterFormat.ARGB32;
//         TextureI.isReadable = true;
//         TextureI.anisoLevel = 9;
//         TextureI.mipmapEnabled = false;
//         TextureI.wrapMode = TextureWrapMode.Clamp;
//         AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
//
//         Texture Contr2 = (Texture)AssetDatabase.LoadAssetAtPath(path, typeof(Texture));
//         CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Control2", Contr2);
//         IniNewSelect();
//     }
//
//     /// <summary>
//     /// Convert unity terrain to T4M OBJ
//     /// </summary>
//     void ConvertUTerrain()
//     {
//         if (terrainName == "")
//             terrainName = CurrentSelect.name;
//
//         string T4MPrefabFolder = EditorPrefs.GetString(T4MPrefabFolderEditorKey, T4MPrefabFolderDefaultValue);
//
//         if (!System.IO.Directory.Exists(T4MPrefabFolder + "Terrains/"))
//         {
//             System.IO.Directory.CreateDirectory(T4MPrefabFolder + "Terrains/");
//         }
//
//         if (!System.IO.Directory.Exists(T4MPrefabFolder + "Terrains/Material/"))
//         {
//             System.IO.Directory.CreateDirectory(T4MPrefabFolder + "Terrains/Material/");
//         }
//
//         if (!System.IO.Directory.Exists(T4MPrefabFolder + "Terrains/Texture/"))
//         {
//             System.IO.Directory.CreateDirectory(T4MPrefabFolder + "Terrains/Texture/");
//         }
//
//         if (!System.IO.Directory.Exists(T4MPrefabFolder + "Terrains/Meshes/"))
//         {
//             System.IO.Directory.CreateDirectory(T4MPrefabFolder + "Terrains/Meshes/");
//         }
//
//         AssetDatabase.Refresh();
//
//         Terrain terrain = CurrentSelect.GetComponent<Terrain>();
//         if (terrain == null)
//         {
//             return;
//         }
//
//         terrainData = terrain.terrainData;
//         int w = terrainData.heightmapResolution;
//         int h = terrainData.heightmapResolution;
//         Vector3 meshScale = terrainData.size;
//         // Terrain Height/Width / pixel counts = distance between two vertices on T4M height/width
//         meshScale = new Vector3(meshScale.x / (T4MResolution - 1), meshScale.y, meshScale.z / (T4MResolution - 1));
//
//         // + —— + —— +
//         // |    |    |
//         // + —— + —— +
//         // |    |    |
//         // + —— + —— +
//         // terrainData.GetHeights(0, 0, w, h) returns a float[w, h] array
//         float[,] tData = terrainData.GetHeights(0, 0, w, h);
//         // T4M vertices count
//         w = T4MResolution;
//         h = T4MResolution;
//         Vector3[] tVertices = new Vector3[w * h];
//         Vector2[] tUV = new Vector2[w * h];
//         Vector3[] tNormals = new Vector3[w * h];
//         int[] tPolys = new int[(w - 1) * (h - 1) * 6];
//         int y = 0;
//         int x = 0;
//         for (y = 0; y < h; y++)
//         {
//             for (x = 0; x < w; x++)
//             {
//                 tVertices[y * w + x] = Vector3.Scale(meshScale, new Vector3(x, tData[Mathf.CeilToInt(x * tScale), Mathf.CeilToInt(y * tScale)], y));
//                 tUV[y * w + x] = new Vector2(y / (h - 1f), x / (w - 1f));
//                 tNormals[y * w + x] = terrainData.GetInterpolatedNormal(x / (w - 1f), y / (h - 1f));
//             }
//         }
//
//         y = 0;
//         x = 0;
//         int index = 0;
//         for (y = 0; y < h - 1; y++)
//         {
//             for (x = 0; x < w - 1; x++)
//             {
//                 tPolys[index++] = (y * w) + x;
//                 tPolys[index++] = ((y + 1) * w) + x;
//                 tPolys[index++] = (y * w) + x + 1;
//
//                 tPolys[index++] = ((y + 1) * w) + x;
//                 tPolys[index++] = ((y + 1) * w) + x + 1;
//                 tPolys[index++] = (y * w) + x + 1;
//             }
//         }
//
//         // 重名处理
//         bool ExportNameSuccess = false;
//         int num = 1;
//         string Next;
//         do
//         {
//             Next = terrainName + num;
//
//             if (!System.IO.File.Exists(T4MPrefabFolder + "Terrains/" + terrainName + ".prefab"))
//             {
//                 FinalExpName = terrainName;
//                 ExportNameSuccess = true;
//             }
//             else if (!System.IO.File.Exists(T4MPrefabFolder + "Terrains/" + Next + ".prefab"))
//             {
//                 FinalExpName = Next;
//                 ExportNameSuccess = true;
//             }
//
//             num++;
//         } while (!ExportNameSuccess);
//
//         //StreamWriter  sw = new StreamWriter(T4MPrefabFolder+"Terrains/Meshes/"+FinalExpName+".obj");
//         StreamWriter sw = new StreamWriter(FinalExpName + ".obj");
//         try
//         {
//             sw.WriteLine("# T4M File");
//             System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
//             counter = tCount = 0;
//             totalCount = (int)((tVertices.Length * 2 + (tPolys.Length / 3)) / progressUpdateInterval);
//             for (int i = 0; i < tVertices.Length; i++)
//             {
//                 UpdateProgress();
//                 StringBuilder sb = new StringBuilder("v ", 1000);
//                 sb.Append(tVertices[i].x.ToString()).Append(" ").Append(tVertices[i].y.ToString()).Append(" ").Append(tVertices[i].z.ToString());
//                 sw.WriteLine(sb);
//             }
//
//             for (int i = 0; i < tUV.Length; i++)
//             {
//                 UpdateProgress();
//                 StringBuilder sb = new StringBuilder("vt ", 1000);
//                 sb.Append(tUV[i].x.ToString()).Append(" ").Append(tUV[i].y.ToString());
//                 sw.WriteLine(sb);
//             }
//
//             //for (int i = 0; i < tNormals.Length; i++)
//             //{
//             //    UpdateProgress();
//             //    StringBuilder sb = new StringBuilder("vn ", 1000);
//             //    sb.Append(tNormals[i].x.ToString()).Append(" ").
//             //       Append(tNormals[i].y.ToString()).Append(" ").
//             //       Append(tNormals[i].z.ToString());
//             //    sw.WriteLine(sb);
//             //}
//
//             for (int i = 0; i < tPolys.Length; i += 3)
//             {
//                 UpdateProgress();
//                 StringBuilder sb = new StringBuilder("f ", 1000);
//                 sb.Append(tPolys[i] + 1).Append("/").Append(tPolys[i] + 1).Append(" ").Append(tPolys[i + 1] + 1).Append("/").Append(tPolys[i + 1] + 1).Append(" ").Append(tPolys[i + 2] + 1).Append("/")
//                     .Append(tPolys[i + 2] + 1);
//                 sw.WriteLine(sb);
//             }
//         }
//         catch (Exception err)
//         {
//             Debug.Log("Error saving file: " + err.Message);
//         }
//
//         sw.Close();
//         AssetDatabase.SaveAssets();
//
//         string controlMapPath = T4MPrefabFolder + "Terrains/Texture/" + FinalExpName + ".png";
//
//         //Control Texture Creation or Recuperation
//         string AssetName = AssetDatabase.GetAssetPath(CurrentSelect.GetComponent<Terrain>().terrainData) as string;
//         UnityEngine.Object[] AssetName2 = AssetDatabase.LoadAllAssetsAtPath(AssetName);
//         if (AssetName2 != null && AssetName2.Length > 1 && keepTexture)
//         {
//             for (var b = 0; b < AssetName2.Length; b++)
//             {
//                 if (AssetName2[b].name == "SplatAlpha 0")
//                 {
//                     Texture2D texture = AssetName2[b] as Texture2D;
//                     byte[] bytes = texture.EncodeToPNG();
//                     File.WriteAllBytes(controlMapPath, bytes);
//                     AssetDatabase.ImportAsset(controlMapPath, ImportAssetOptions.ForceUpdate);
//                 }
//             }
//         }
//         else
//         {
//             Texture2D NewMaskText = new Texture2D(512, 512, TextureFormat.ARGB32, true);
//             Color[] ColorBase = new Color[512 * 512];
//             for (var t = 0; t < ColorBase.Length; t++)
//             {
//                 ColorBase[t] = new Color(1, 0, 0, 0);
//             }
//
//             NewMaskText.SetPixels(ColorBase);
//             byte[] data = NewMaskText.EncodeToPNG();
//             File.WriteAllBytes(controlMapPath, data);
//             AssetDatabase.ImportAsset(controlMapPath, ImportAssetOptions.ForceUpdate);
//         }
//
//         AssetDatabase.ImportAsset(controlMapPath, ImportAssetOptions.ForceUpdate);
//
//         UpdateProgress();
//
//         // Reimport
//         TextureImporter TextureI = AssetImporter.GetAtPath(controlMapPath) as TextureImporter;
//         TextureI.wrapMode = TextureWrapMode.Clamp;
//         // sRGB 一定要关闭，否则采样的结果会经过伽马矫正得到错误的值，导致融合效果很怪
//         TextureI.sRGBTexture = false;
//         AssetDatabase.Refresh();
//
//         AssetDatabase.ImportAsset(controlMapPath, ImportAssetOptions.ForceUpdate);
//
//         UpdateProgress();
//
//         // Material
//         Material Tmaterial;
//         Tmaterial = new Material(Shader.Find(defaultShaderName));
//         AssetDatabase.CreateAsset(Tmaterial, T4MPrefabFolder + "Terrains/Material/" + FinalExpName + ".mat");
//         AssetDatabase.ImportAsset(T4MPrefabFolder + "Terrains/Material/" + FinalExpName + ".mat", ImportAssetOptions.ForceUpdate);
//         AssetDatabase.Refresh();
//
//         //Recuperation des Texture du terrain
//         //Unity 2019.4 Terrain System, use terrainLayer instead
//         if (keepTexture)
//         {
//             TerrainLayer[] layers = terrainData.terrainLayers;
//             int layerCount = layers.Length;
//             for (int i = 0; i < layerCount; i++)
//             {
//                 if (i < 4)
//                 {
//                     float tillingScaleX = layers[i].tileSize.x == 0f ? 0f : terrainData.size.x / layers[i].tileSize.x;
//                     float tillingScaleY = layers[i].tileSize.y == 0f ? 0f : terrainData.size.z / layers[i].tileSize.y;
//                     float tillingOffsetX = layers[i].tileSize.x == 0f ? 0f : layers[i].tileOffset.x / layers[i].tileSize.x;
//                     float tillingOffsetY = layers[i].tileSize.y == 0f ? 0f : layers[i].tileOffset.y / layers[i].tileSize.y;
//                     Tmaterial.SetTextureScale("_Splat" + i, new Vector2(tillingScaleX, tillingScaleY));
//                     Tmaterial.SetTextureOffset("_Splat" + i, new Vector2(tillingOffsetX, tillingOffsetY));
//                     Tmaterial.SetTexture("_Splat" + i, layers[i].diffuseTexture);
//
//                     Tmaterial.SetTexture("_Normal" + i, layers[i].normalMapTexture);
//                     Tmaterial.SetTexture("_Mask" + i, layers[i].maskMapTexture);
//                 }
//             }
//
//             //Attribution de la Texture Control sur le materiau
//             Texture2D savedControlMap = (Texture2D)AssetDatabase.LoadAssetAtPath(controlMapPath, typeof(Texture2D));
//             Tmaterial.SetTexture("_Control", savedControlMap);
//         }
//
//         // GI
//         Material terrainMat = terrain.materialTemplate;
//         float indirectSpecularAtten = terrainMat.GetFloat("_GIIndirectSpecularAtten");
//         Tmaterial.SetFloat("_GIIndirectSpecularAtten", indirectSpecularAtten);
//
//         UpdateProgress();
//
//
//         //Deplacement de l'obj dans les repertoire mesh
//         FileUtil.CopyFileOrDirectory(FinalExpName + ".obj", T4MPrefabFolder + "Terrains/Meshes/" + FinalExpName + ".obj");
//         FileUtil.DeleteFileOrDirectory(FinalExpName + ".obj");
//
//
//         //Force Update
//         AssetDatabase.ImportAsset(T4MPrefabFolder + "Terrains/Meshes/" + FinalExpName + ".obj", ImportAssetOptions.ForceUpdate);
//
//         UpdateProgress();
//
//         //Instance du T4M
//         GameObject prefab = (GameObject)AssetDatabase.LoadAssetAtPath(T4MPrefabFolder + "Terrains/Meshes/" + FinalExpName + ".obj", typeof(GameObject));
//
//         AssetDatabase.Refresh();
//
//
//         GameObject forRotate = (GameObject)Instantiate(prefab, CurrentSelect.transform.position, Quaternion.identity) as GameObject;
//         Transform childCheck = forRotate.transform.Find("default");
//         Child = childCheck.gameObject;
//         forRotate.transform.DetachChildren();
//         DestroyImmediate(forRotate);
//         Child.name = FinalExpName;
//         Child.AddComponent<T4MObjSC>();
//         Child.transform.rotation = Quaternion.Euler(0, 90, 0);
//
//         UpdateProgress();
//
//         //Application des Parametres sur le Script
//         Child.GetComponent<T4MObjSC>().T4MMaterial = Tmaterial;
//         Child.GetComponent<T4MObjSC>().ConvertType = "UT";
//
//         //Regalges Divers
//         vertexInfo = 0;
//         partofT4MObj = 0;
//         trisInfo = 0;
//         int countchild = Child.transform.childCount;
//
//         List<string> allLayers = new List<string>();
//         for (int i = 0; i < 31; i++)
//         {
//             string layer = LayerMask.LayerToName(i);
//             if (!string.IsNullOrEmpty(layer))
//             {
//                 allLayers.Add(layer);
//             }
//         }
//
//         if (countchild > 0)
//         {
//             Renderer[] T4MOBJPART = Child.GetComponentsInChildren<Renderer>();
//             for (int i = 0; i < T4MOBJPART.Length; i++)
//             {
//                 if (!T4MOBJPART[i].gameObject.AddComponent<MeshCollider>())
//                     T4MOBJPART[i].gameObject.AddComponent<MeshCollider>();
//                 T4MOBJPART[i].gameObject.isStatic = true;
//                 T4MOBJPART[i].material = Tmaterial;
//                 T4MOBJPART[i].gameObject.layer = LayerMask.NameToLayer(allLayers[targetT4MLayerIndex]);
//                 T4MOBJPART[i].gameObject.AddComponent<T4MPartSC>();
//                 Child.GetComponent<T4MObjSC>().T4MMesh = T4MOBJPART[0].GetComponent<MeshFilter>();
//                 partofT4MObj += 1;
//                 vertexInfo += T4MOBJPART[i].gameObject.GetComponent<MeshFilter>().sharedMesh.vertexCount;
//                 trisInfo += T4MOBJPART[i].gameObject.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
//             }
//         }
//         else
//         {
//             Child.AddComponent<MeshCollider>();
//             Child.isStatic = true;
//             Child.GetComponent<Renderer>().material = Tmaterial;
//             Child.layer = LayerMask.NameToLayer(allLayers[targetT4MLayerIndex]);
//             vertexInfo += Child.GetComponent<MeshFilter>().sharedMesh.vertexCount;
//             trisInfo += Child.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
//             partofT4MObj += 1;
//         }
//
//         UpdateProgress();
//
//         GameObject BasePrefab2 = PrefabUtility.SaveAsPrefabAsset(Child, T4MPrefabFolder + "Terrains/" + FinalExpName + ".prefab");
//         AssetDatabase.ImportAsset(T4MPrefabFolder + "Terrains/" + FinalExpName + ".prefab", ImportAssetOptions.ForceUpdate);
//         GameObject forRotate2 = (GameObject)PrefabUtility.InstantiatePrefab(BasePrefab2) as GameObject;
//
//         DestroyImmediate(Child.gameObject);
//
//         Child = forRotate2.gameObject;
//
//         if (!deleteOriginUnityTerrain)
//         {
//             terrain.gameObject.SetActive(!hideOriginUnityTerrain);
//         }
//
//         EditorUtility.SetSelectedRenderState(Child.GetComponent<Renderer>(), EditorSelectedRenderState.Wireframe);
//
//         UnityTerrain = CurrentSelect.gameObject;
//
//         EditorUtility.ClearProgressBar();
//
//         AssetDatabase.DeleteAsset(T4MPrefabFolder + "Terrains/Meshes/Materials");
//         terrainName = "";
//         AssetDatabase.StartAssetEditing();
//         //Modification des attribut du mesh avant de le préfabriquer
//         ModelImporter OBJI = ModelImporter.GetAtPath(T4MPrefabFolder + "Terrains/Meshes/" + FinalExpName + ".obj") as ModelImporter;
//         OBJI.globalScale = 1;
//         //OBJI.splitTangentsAcrossSeams = true;
//         OBJI.importTangents = ModelImporterTangents.CalculateLegacyWithSplitTangents;
//         //OBJI.normalImportMode = ModelImporterTangentSpaceMode.Calculate;
//         OBJI.importNormals = ModelImporterNormals.Calculate;
//         //OBJI.tangentImportMode = ModelImporterTangentSpaceMode.Calculate;
//         OBJI.generateAnimations = ModelImporterGenerateAnimations.None;
//         OBJI.meshCompression = ModelImporterMeshCompression.Off;
//         OBJI.normalSmoothingAngle = 180f;
//         //AssetDatabase.ImportAsset (T4MPrefabFolder+"Terrains/Meshes/"+FinalExpName+".obj", ImportAssetOptions.TryFastReimportFromMetaData);
//         AssetDatabase.ImportAsset(T4MPrefabFolder + "Terrains/Meshes/" + FinalExpName + ".obj", ImportAssetOptions.ForceSynchronousImport);
//         AssetDatabase.StopAssetEditing();
//         PrefabUtility.RevertObjectOverride(Child, InteractionMode.AutomatedAction);
//
//         IniNewSelect();
//     }
//
//     /// <summary>
//     /// Convert any gameObject to T4M OBJ
//     /// </summary>
//     void Obj2T4M()
//     {
//         string T4MPrefabFolder = EditorPrefs.GetString(T4MPrefabFolderEditorKey, T4MPrefabFolderDefaultValue);
//
//         if (!System.IO.Directory.Exists(newMatPath))
//         {
//             System.IO.Directory.CreateDirectory(newMatPath);
//         }
//
//         if (!System.IO.Directory.Exists(T4MPrefabFolder + "Models/Texture/"))
//         {
//             System.IO.Directory.CreateDirectory(T4MPrefabFolder + "Models/Texture/");
//         }
//
//         AssetDatabase.Refresh();
//
//         if (string.IsNullOrEmpty(newMatName))
//         {
//             newMatName = oldModelMaterial == null ? "NewLayeredMaterial" : oldModelMaterial.name + "_Layered";
//         }
//
//         // 重名处理
//         bool ExportNameSuccess = false;
//         int num = 1;
//         string Next;
//         do
//         {
//             Next = newMatName + num;
//
//             if (!System.IO.File.Exists(newMatPath + newMatName + ".mat"))
//             {
//                 FinalExpName = newMatName;
//                 ExportNameSuccess = true;
//             }
//             else if (!System.IO.File.Exists(newMatPath + "Models/Material/" + Next + ".mat"))
//             {
//                 FinalExpName = Next;
//                 ExportNameSuccess = true;
//             }
//
//             num++;
//         } while (!ExportNameSuccess);
//
//         Texture2D NewMaskText = new Texture2D(512, 512, TextureFormat.ARGB32, true);
//         Color[] ColorBase = new Color[512 * 512];
//         for (var t = 0; t < ColorBase.Length; t++)
//         {
//             ColorBase[t] = new Color(1, 0, 0, 0);
//         }
//
//         NewMaskText.SetPixels(ColorBase);
//
//         var controlMapPath = T4MPrefabFolder + "Models/Texture/" + FinalExpName + ".png";
//         var data = NewMaskText.EncodeToPNG();
//         File.WriteAllBytes(controlMapPath, data);
//         AssetDatabase.ImportAsset(controlMapPath, ImportAssetOptions.ForceUpdate);
//         var TextureIm = AssetImporter.GetAtPath(controlMapPath) as TextureImporter;
//
//         // Reimport Mask
//         TextureIm.textureType = TextureImporterType.Default;
//         TextureImporterPlatformSettings tSetting = new TextureImporterPlatformSettings();
//         tSetting.overridden = true;
//         tSetting.format = TextureImporterFormat.RGBA32;
//         TextureIm.SetPlatformTextureSettings(tSetting);
//         // sRGB 一定要关闭，否则采样的结果会经过伽马矫正得到错误的值，导致融合效果很怪
//         TextureIm.sRGBTexture = false;
//         TextureIm.isReadable = true;
//         TextureIm.anisoLevel = 9;
//         TextureIm.mipmapEnabled = false;
//         TextureIm.wrapMode = TextureWrapMode.Clamp;
//         AssetDatabase.ImportAsset(controlMapPath, ImportAssetOptions.ForceUpdate);
//         Texture controlMap = (Texture)AssetDatabase.LoadAssetAtPath(controlMapPath, typeof(Texture));
//
//         // Material
//         Shader T4MShader = Shader.Find(defaultShaderName);
//
//         Texture baseAlbedo = null;
//         Texture baseROME = null;
//         Texture baseNormal = null;
//         float baseNormalScale = 1f;
//         Vector2 tillingScale = Vector2.one;
//         Vector2 tillingOffset = Vector2.zero;
//         if (oldModelMaterial != null)
//         {
//             if (oldModelMaterial.HasProperty("_BaseMap"))
//             {
//                 baseAlbedo = oldModelMaterial.GetTexture("_BaseMap");
//                 tillingOffset = oldModelMaterial.GetTextureOffset("_BaseMap");
//                 tillingScale = oldModelMaterial.GetTextureScale("_BaseMap");
//             }
//
//             if (oldModelMaterial.HasProperty("_MetallicGlossMap"))
//             {
//                 baseROME = oldModelMaterial.GetTexture("_MetallicGlossMap");
//             }
//
//             if (oldModelMaterial.HasProperty("_BumpMap"))
//             {
//                 baseNormal = oldModelMaterial.GetTexture("_BumpMap");
//             }
//
//             if (oldModelMaterial.HasProperty("_BumpScale"))
//             {
//                 baseNormalScale = oldModelMaterial.GetFloat("_BumpScale");
//             }
//         }
//
//         Material Tmaterial;
//         if (needCreateNewMat)
//         {
//             string finalMatPath = newMatPath + FinalExpName + ".mat";
//             Tmaterial = new Material(T4MShader);
//             AssetDatabase.CreateAsset(Tmaterial, finalMatPath);
//             AssetDatabase.ImportAsset(finalMatPath, ImportAssetOptions.ForceUpdate);
//
//             if (oldModelMaterial != null)
//             {
//                 Tmaterial.SetTextureScale("_Splat0", tillingScale);
//                 Tmaterial.SetTextureOffset("_Splat0", tillingOffset);
//                 if (baseAlbedo != null)
//                 {
//                     Tmaterial.SetTexture("_Splat0", baseAlbedo);
//                 }
//
//                 if (baseAlbedo != null)
//                 {
//                     Tmaterial.SetTexture("_Normal0", baseNormal);
//                 }
//
//                 if (baseNormal != null)
//                 {
//                     Tmaterial.SetFloat("_NormalScale0", baseNormalScale);
//                 }
//
//                 if (baseROME != null)
//                 {
//                     Tmaterial.SetTexture("_Mask0", baseROME);
//                 }
//             }
//
//             Tmaterial.SetTexture("_Control", controlMap);
//         }
//         else
//         {
//             if (oldModelMaterial != null)
//             {
//                 Tmaterial = oldModelMaterial;
//                 oldModelMaterial.shader = T4MShader;
//
//                 oldModelMaterial.SetTextureScale("_Splat0", tillingScale);
//                 oldModelMaterial.SetTextureOffset("_Splat0", tillingOffset);
//                 if (baseAlbedo != null)
//                 {
//                     oldModelMaterial.SetTexture("_Splat0", baseAlbedo);
//                 }
//
//                 if (baseAlbedo != null)
//                 {
//                     oldModelMaterial.SetTexture("_Normal0", baseNormal);
//                 }
//
//                 if (baseNormal != null)
//                 {
//                     oldModelMaterial.SetFloat("_NormalScale0", baseNormalScale);
//                 }
//
//                 if (baseROME != null)
//                 {
//                     oldModelMaterial.SetTexture("_Mask0", baseROME);
//                 }
//             }
//             else
//             {
//                 return;
//             }
//         }
//
//         CurrentSelect.gameObject.AddComponent<T4MObjSC>();
//
//         List<string> allLayers = new List<string>();
//         for (int i = 0; i < 31; i++)
//         {
//             string layer = LayerMask.LayerToName(i);
//             if (!string.IsNullOrEmpty(layer))
//             {
//                 allLayers.Add(layer);
//             }
//         }
//
//         int countchild = CurrentSelect.transform.childCount;
//         if (countchild > 0)
//         {
//             Renderer[] T4MOBJPART = CurrentSelect.GetComponentsInChildren<Renderer>();
//             for (int i = 0; i < T4MOBJPART.Length; i++)
//             {
//                 if (T4MOBJPART[i].gameObject.GetComponent<Collider>())
//                     DestroyImmediate(T4MOBJPART[i].gameObject.GetComponent<Collider>());
//
//                 T4MOBJPART[i].gameObject.AddComponent<MeshCollider>();
//
//                 T4MOBJPART[i].gameObject.isStatic = true;
//
//                 T4MOBJPART[i].material = Tmaterial;
//                 T4MOBJPART[i].gameObject.layer = LayerMask.NameToLayer(allLayers[targetT4MLayerIndex]);
//                 T4MOBJPART[i].gameObject.AddComponent<T4MPartSC>();
//                 CurrentSelect.GetComponent<T4MObjSC>().T4MMesh = T4MOBJPART[0].GetComponent<MeshFilter>();
//             }
//         }
//         else
//         {
//             if (CurrentSelect.GetComponent<Collider>())
//                 DestroyImmediate(CurrentSelect.GetComponent<Collider>());
//
//             CurrentSelect.gameObject.AddComponent<MeshCollider>();
//             CurrentSelect.gameObject.GetComponent<Renderer>().material = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial = Tmaterial;
//             CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMesh = CurrentSelect.gameObject.GetComponent<MeshFilter>();
//         }
//
//         CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial = Tmaterial;
//         //CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.SetTexture("_Control", maskTex);
//         CurrentSelect.gameObject.isStatic = true;
//         CurrentSelect.gameObject.layer = LayerMask.NameToLayer(allLayers[targetT4MLayerIndex]);
//
//
//         Selection.activeTransform = CurrentSelect.transform;
//         EditorUtility.SetSelectedRenderState(CurrentSelect.GetComponent<Renderer>(), EditorSelectedRenderState.Wireframe);
//
//         EditorUtility.DisplayDialog("T4M Message", "Conversion Completed !", "OK");
//
//         T4MMenuToolbar = 0;
//         terrainName = "";
//         AssetDatabase.SaveAssets();
//
//         IniNewSelect();
//     }
//
//
//     void GetHeightmap()
//     {
//         terrainDat = CurrentSelect.GetComponent<Terrain>().terrainData;
//         HeightmapWidth = terrainDat.heightmapResolution;
//         HeightmapHeight = terrainDat.heightmapResolution;
//     }
//
//     void UpdateProgress()
//     {
//         if (counter++ == progressUpdateInterval)
//         {
//             counter = 0;
//             EditorUtility.DisplayProgressBar("Generate...", "", Mathf.InverseLerp(0, totalCount, ++tCount));
//         }
//     }
//
//     void IniNewSelect()
//     {
//         if (UnityTerrain && deleteOriginUnityTerrain)
//         {
//             DestroyImmediate(UnityTerrain);
//             if (Child)
//             {
//                 Selection.activeTransform = Child.transform;
//                 vertexInfo = 0;
//                 trisInfo = 0;
//                 partofT4MObj = 0;
//             }
//         }
//
//         if (CurrentSelect && CurrentSelect.GetComponent<T4MObjSC>() && CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial)
//         {
//             EditorUtility.SetSelectedRenderState(CurrentSelect.GetComponent<Renderer>(), EditorSelectedRenderState.Wireframe);
//
//             if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat0"))
//             {
//                 Layer1 = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat0");
//                 Layer1Tile = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTextureScale("_Splat0");
//             }
//             else Layer1 = null;
//
//             if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat1"))
//             {
//                 Layer2 = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat1");
//                 Layer2Tile = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTextureScale("_Splat1");
//             }
//             else Layer2 = null;
//
//             if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat2"))
//             {
//                 Layer3 = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat2");
//                 Layer3Tile = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTextureScale("_Splat2");
//             }
//             else Layer3 = null;
//
//             if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat3"))
//             {
//                 Layer4 = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat3");
//                 Layer4Tile = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTextureScale("_Splat3");
//             }
//             else Layer4 = null;
//
//             if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat4"))
//             {
//                 Layer5 = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat4");
//                 Layer5Tile = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTextureScale("_Splat4");
//             }
//             else Layer5 = null;
//
//             if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Splat5"))
//             {
//                 Layer6 = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Splat5");
//                 Layer6Tile = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTextureScale("_Splat5");
//             }
//             else Layer6 = null;
//
//             if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Normal0"))
//             {
//                 Layer1Bump = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Normal0");
//                 Layer2Bump = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Normal1");
//                 if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Normal2"))
//                     Layer3Bump = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Normal2");
//                 if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Normal3"))
//                     Layer4Bump = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Normal3");
//             }
//
//             if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Mask0"))
//             {
//                 Layer1Mask = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Mask0");
//                 Layer2Mask = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Mask1");
//                 if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Mask2"))
//                     Layer3Mask = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Mask2");
//                 if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Mask3"))
//                     Layer4Mask = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Mask3");
//             }
//
//             if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.shader == Shader.Find("T4MShaders/ShaderModel1/T4M 2 Textures ManualAdd BeastLM_1DC") ||
//                 CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.shader == Shader.Find("T4MShaders/ShaderModel1/T4M 2 Textures ManualAdd CustoLM 1DC"))
//             {
//                 LMMan = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Lightmap");
//             }
//
//             CheckShader();
//
//             if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_SpecColor"))
//             {
//                 ShinessColor = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetColor("_SpecColor");
//
//                 if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_ShininessL0"))
//                 {
//                     shiness0 = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetFloat("_ShininessL0");
//                 }
//
//                 if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_ShininessL1"))
//                 {
//                     shiness1 = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetFloat("_ShininessL1");
//                 }
//
//                 if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_ShininessL2"))
//                 {
//                     shiness2 = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetFloat("_ShininessL2");
//                 }
//
//                 if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_ShininessL3"))
//                 {
//                     shiness3 = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetFloat("_ShininessL3");
//                 }
//             }
//
//             if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Control2") && CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Control2"))
//                 T4MMaskTex2 = (Texture2D)CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Control2");
//             else T4MMaskTex2 = null;
//             if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.HasProperty("_Control"))
//             {
//                 T4MMaskTexUVCoord = CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTextureScale("_Control").x;
//                 T4MMaskTex = (Texture2D)CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.GetTexture("_Control");
//                 intialized = true;
//             }
//         }
//
//         terrainDat = null;
//         vertexInfo = 0;
//         trisInfo = 0;
//         partofT4MObj = 0;
//         TexTexture = null;
//
//         T4MSelectID = Selection.activeInstanceID;
//     }
//
//     void CheckShader()
//     {
//         //if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.shader == Shader.Find(defaultShaderName))
//         //{
//         //    lightModel = LightModel.Lit;
//         //    brushCount = BrushCount.Four;
//         //}
//         //else if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.shader == Shader.Find("Hidden/T4M Shaders/Shader Model 3/Lit/Lit 3 Textures"))
//         //{
//         //    lightModel = LightModel.Lit;
//         //    brushCount = BrushCount.Three;
//         //}
//         //else if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.shader == Shader.Find("Hidden/T4M Shaders/Shader Model 3/Lit/Lit 2 Textures"))
//         //{
//         //    lightModel = LightModel.Lit;
//         //    brushCount = BrushCount.Two;
//         //}
//         //else if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.shader == Shader.Find("Hidden/T4M Shaders/Shader Model 2/Diffuse/Diffuse 4 Textures"))
//         //{
//         //    lightModel = LightModel.Diffuse;
//         //    brushCount = BrushCount.Four;
//         //}
//         //else if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.shader == Shader.Find("Hidden/T4M Shaders/Shader Model 2/Diffuse/Diffuse 3 Textures"))
//         //{
//         //    lightModel = LightModel.Diffuse;
//         //    brushCount = BrushCount.Three;
//         //}
//         //else if (CurrentSelect.gameObject.GetComponent<T4MObjSC>().T4MMaterial.shader == Shader.Find("Hidden/T4M Shaders/Shader Model 2/Diffuse/Diffuse 2 Textures"))
//         //{
//         //    lightModel = LightModel.Diffuse;
//         //    brushCount = BrushCount.Two;
//         //}
//     }
// }