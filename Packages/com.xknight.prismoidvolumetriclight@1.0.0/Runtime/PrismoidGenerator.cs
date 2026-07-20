/// <summary>
/// 假聚焦灯光效果
/// 作者：Ling mei an
/// 修改日期：2025-9-12
/// 功能：假聚焦灯光效果。
/// </summary>
#if UNITY_EDITOR
using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using UnityEngine.Rendering.Universal;



namespace  knightTA.FakeSpotLightTool
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class PrismoidGenerator : MonoBehaviour
    {
        public enum type1 {
            [InspectorName("模型")]
            Tmodels,
            [InspectorName("自定义模型")]
            Customs
        }

        [Header("主要参数")] 
        [Title("模型类型")]
        public type1 theStyle = 0;
        [Title("底面/顶面边数")]
        [Range(3, 32)] public int sides = 6;          // 底面/顶面边数
        [Title("高度方向分段数")]
        [Range(1, 16)] public int heightSegments = 3;  // 高度方向分段数
        [Title("底面半径")]
        public float bottomRadius = 2f;                // 底面半径
        [Title("顶面半径")]
        public float topRadius = 1f;                   // 顶面半径
        [Title("高度")]
        public float height = 3f;                      // 高度
        
        [Header("选项")]
        [Title("是否生成底面")]
        public bool generateBottom = false;             // 是否生成底面
        [Title("是否生成顶面")]
        public bool generateTop = false;                // 是否生成顶面

        [Title("是否平滑法线（默认为true）")] 
        
        public bool smoothNormals = true;              // 是否平滑法线（默认为true）
        [Title("保存Mesh路径")] 
        public string savePath = "Assets/OriginalRes/Temp/Engine_TA/Graphical/FakePointLight";
        [Title("替换模型放置在此（默认为圆锥体）")]
        public Mesh changeMesh;              // 是否平滑法线（默认为true）

        public Mesh mesh;
        private Material material;
        private List<Vector3> vertices;
        private List<int> triangles;
        private List<Vector2> uvs;
        private List<Vector3> normals;

        void Start()
        {
            if(theStyle == 0)
                InitializeMesh();
            else
            {
                GeneratePrismoid();
            }

        }

        void OnValidate()
        {
            if (mesh != null && Application.isPlaying)
            {
                GeneratePrismoid();
            }
        }
        
        

        [ContextMenu("生成多棱台")]
        public void GeneratePrismoid()
        {
            InitializeMesh();
            CreateVertices();
            CreateTriangles();
            CreateUVs();
            CalculateNormals();
            UpdateMesh();
        }

        public void InitializeMesh()
        {
            if (material == null)
            {
                material = Resources.Load<Material>("Materials/FakeLit");
                GetComponent<MeshRenderer>().castShadows = false;
                GetComponent<MeshRenderer>().material = material;
            }

            if (theStyle == 0)
            {
                if (changeMesh == null)
                {
                    changeMesh = Resources.Load<Mesh>("Models/VolumetricLightShaft");
                    GetComponent<MeshFilter>().sharedMesh = changeMesh;
                    
                }

                if (GetComponent<MeshFilter>().sharedMesh != changeMesh)
                {
                    GetComponent<MeshFilter>().sharedMesh = changeMesh;
                    
                }
                
            }
            else
            {
                if (mesh == null)
                {
                    mesh = new Mesh();
                    mesh.name = gameObject.name;
                    GetComponent<MeshFilter>().mesh = mesh;
                }
                if (GetComponent<MeshFilter>().sharedMesh != mesh)
                {
                    GetComponent<MeshFilter>().sharedMesh = mesh;
                }
            
                vertices = new List<Vector3>();
                triangles = new List<int>();
                normals = new List<Vector3>();
                uvs = new List<Vector2>();
            }
            

        }

        private void CreateVertices()
        {
            vertices.Clear();
            
            // 计算高度偏移，使中心点在(0,0,0)
            float halfHeight = height * 0.5f;
            // 生成侧面顶点
            for (int y = 0; y <= heightSegments; y++)
            {
                float v = (float)y / heightSegments;
                float currentRadius = Mathf.Lerp(bottomRadius, topRadius, v);
                float yPos = v * height - height;
                
                for (int i = 0; i < sides; i++)
                {
                    float angle = 2 * Mathf.PI * i / sides;
                    float x = currentRadius * Mathf.Cos(angle);
                    float z = currentRadius * Mathf.Sin(angle);
                    vertices.Add(new Vector3(x, yPos, z));
                }
            }
            
            // 添加底面中心点
            if (generateBottom)
            {
                vertices.Add(new Vector3(0, -height, 0));
            }
            
            // 添加顶面中心点
            if (generateTop)
            {
                vertices.Add(new Vector3(0, 0, 0));
            }
        }

        private void CreateTriangles()
        {
            triangles.Clear();
            
            // 生成侧面三角形
            for (int y = 0; y < heightSegments; y++)
            {
                for (int i = 0; i < sides; i++)
                {
                    int current = y * sides + i;
                    int next = (y + 1) * sides + i;
                    int nextSide = (i + 1) % sides;
                    
                    int currentNext = y * sides + nextSide;
                    int nextNext = (y + 1) * sides + nextSide;
                    
                    // 四边形 = 两个三角形
                    triangles.Add(current);
                    triangles.Add(next);
                    triangles.Add(nextNext);
                    
                    triangles.Add(current);
                    triangles.Add(nextNext);
                    triangles.Add(currentNext);
                }
            }
            
            // 生成底面
            if (generateBottom)
            {
                int bottomCenterIndex = vertices.Count - (generateTop ? 2 : 1);
                
                for (int i = 0; i < sides; i++)
                {
                    int next = (i + 1) % sides;
                    
                    // 底面三角形（注意顶点顺序确保法线朝下）
                    triangles.Add(bottomCenterIndex);
                    triangles.Add(i);
                    triangles.Add(next);
                }
            }
            
            // 生成顶面
            if (generateTop)
            {
                int topCenterIndex = vertices.Count - 1;
                int topRingStart = heightSegments * sides;
                
                for (int i = 0; i < sides; i++)
                {
                    int current = topRingStart + i;
                    int next = topRingStart + (i + 1) % sides;
                    
                    // 顶面三角形（注意顶点顺序确保法线朝上）
                    triangles.Add(topCenterIndex);
                    triangles.Add(next);
                    triangles.Add(current);
                }
            }
        }

        private void CreateUVs()
        {
            uvs.Clear();
            
            // 侧面UV映射 - 优化柱面投影
            for (int y = 0; y <= heightSegments; y++)
            {
                for (int i = 0; i < sides; i++)
                {
                    // 改进的UV映射，避免纹理拉伸
                    float u = (float)i / sides;
                    float v = (float)y / heightSegments;
                    uvs.Add(new Vector2(u, v));
                }
            }
            
            // 底面中心UV
            if (generateBottom)
            {
                uvs.Add(new Vector2(0.5f, 0.5f));
            }
            
            // 顶面中心UV
            if (generateTop)
            {
                uvs.Add(new Vector2(0.5f, 0.5f));
            }
        }

        private void CalculateNormals()
        {
            normals.Clear();
            
            // 初始化法线数组
            Vector3[] vertexNormals = new Vector3[vertices.Count];
            
            // 如果启用平滑法线，使用加权平均法
            if (smoothNormals)
            {
                // 第一步：计算每个三角形的法线并加权累加到顶点
                for (int i = 0; i < triangles.Count; i += 3)
                {
                    int index0 = triangles[i];
                    int index1 = triangles[i + 1];
                    int index2 = triangles[i + 2];
                    
                    Vector3 v0 = vertices[index0];
                    Vector3 v1 = vertices[index1];
                    Vector3 v2 = vertices[index2];
                    
                    // 计算三角形面积（用于加权）
                    Vector3 edge1 = v1 - v0;
                    Vector3 edge2 = v2 - v0;
                    float triangleArea = Vector3.Cross(edge1, edge2).magnitude * 0.5f;
                    
                    // 计算三角形法线
                    Vector3 faceNormal = Vector3.Cross(edge1, edge2).normalized;
                    
                    // 按三角形面积加权累加
                    vertexNormals[index0] += faceNormal * triangleArea;
                    vertexNormals[index1] += faceNormal * triangleArea;
                    vertexNormals[index2] += faceNormal * triangleArea;
                }
                
                // 第二步：归一化所有法线
                for (int i = 0; i < vertexNormals.Length; i++)
                {
                    if (vertexNormals[i].sqrMagnitude > 0.0001f)
                    {
                        normals.Add(vertexNormals[i].normalized);
                    }
                    else
                    {
                        // 对于法线为零的顶点（如中心点），使用位置方向
                        Vector3 pos = vertices[i];
                        if (pos.sqrMagnitude < 0.001f) // 中心点
                        {
                            normals.Add(i < vertices.Count - (generateTop ? 1 : 0) ? Vector3.down : Vector3.up);
                        }
                        else
                        {
                            normals.Add(pos.normalized);
                        }
                    }
                }
            }
            else
            {
                // 硬边法线：为每个三角形计算法线并直接使用
                Vector3[] faceNormals = new Vector3[triangles.Count / 3];
                
                // 计算每个三角形的法线
                for (int i = 0; i < triangles.Count; i += 3)
                {
                    Vector3 v0 = vertices[triangles[i]];
                    Vector3 v1 = vertices[triangles[i + 1]];
                    Vector3 v2 = vertices[triangles[i + 2]];
                    
                    faceNormals[i / 3] = Vector3.Cross(v1 - v0, v2 - v0).normalized;
                }
                
                // 为每个顶点分配所属三角形的法线
                for (int i = 0; i < vertices.Count; i++)
                {
                    bool found = false;
                    
                    // 查找包含此顶点的第一个三角形
                    for (int j = 0; j < triangles.Count; j++)
                    {
                        if (triangles[j] == i)
                        {
                            normals.Add(faceNormals[j / 3]);
                            found = true;
                            break;
                        }
                    }
                    
                    if (!found)
                    {
                        // 未找到三角形的顶点（如中心点）
                        Vector3 pos = vertices[i];
                        if (pos.sqrMagnitude < 0.001f) // 中心点
                        {
                            normals.Add(i < vertices.Count - (generateTop ? 1 : 0) ? Vector3.down : Vector3.up);
                        }
                        else
                        {
                            normals.Add(pos.normalized);
                        }
                    }
                }
            }
        }

        private void UpdateMesh()
        {
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);
            mesh.RecalculateBounds();
            
        }



        // 动态更新参数的方法
        public void UpdateParameters(int newSides, int newHeightSegments, 
                                    float newBottomRadius, float newTopRadius, 
                                    float newHeight)
        {
            sides = Mathf.Clamp(newSides, 3, 32);
            heightSegments = Mathf.Clamp(newHeightSegments, 1, 16);
            bottomRadius = newBottomRadius;
            topRadius = newTopRadius;
            height = newHeight;
            
            GeneratePrismoid();
        }
        
        public void BakeGameObject(Mesh mesh, string name)
        {
            SaveAsset(mesh, name);
            
        }
    
        public void SaveAsset(Mesh mesh,string name)
        {
            string path = savePath;
            string thepath = path + "/"+ name + ".asset";
            Mesh dummy = (Mesh)AssetDatabase.LoadAssetAtPath(thepath, typeof(Mesh));
            if (mesh != null && !dummy) {
                AssetDatabase.CreateAsset(mesh, thepath);
                Debug.Log("提取mesh成功：提取_" + name);
            }
            else if(mesh == null)
                Debug.LogWarning("提取mesh失败：无MeshFilter组件");
            else if(mesh != null && dummy)
            {
                bool ifDelete = AssetDatabase.DeleteAsset(thepath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                if (ifDelete)
                {
                    AssetDatabase.CreateAsset(mesh, thepath);
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            DestroyImmediate(this);
        }
    }
}
#endif