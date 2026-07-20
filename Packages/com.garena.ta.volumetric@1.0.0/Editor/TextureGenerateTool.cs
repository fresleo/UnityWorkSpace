using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Threading.Tasks;
using Unity.Collections;

public class TextureGenerateTool : EditorWindow
{
    [MenuItem("Window/TA工具集/资源-生成工具/体积雾/体积贴图生成器", false, 38)]
    static void ShowWindow()
    {
        TextureGenerateTool ew = TextureGenerateTool.CreateWindow<TextureGenerateTool>();
        ew.Show();
    }
    int m_mainShapeSize = 128;
    int m_mainShapeDepth = 32;
    float m_mainShapeCoverage = 0.6f;
    int m_mainShapeFrequency = 4;

    int m_detailShapeSize = 64;
    public Texture2D[] m_blueNoise;

    SerializedObject m_so;
    SerializedProperty m_sp;
    private void OnEnable()
    {
        m_so = new SerializedObject(this);
        m_sp = m_so.FindProperty("m_blueNoise");
    }
    private void OnGUI()
    {
        m_mainShapeSize = EditorGUILayout.IntField("MainShape Size", m_mainShapeSize);
        m_mainShapeDepth = EditorGUILayout.IntField("MainShape Depth", m_mainShapeDepth);
        m_mainShapeFrequency = EditorGUILayout.IntField("MainShape Frequency", m_mainShapeFrequency);

        m_mainShapeCoverage = EditorGUILayout.Slider("MainShape Coverage", m_mainShapeCoverage, 0, 1);
        m_mainShapeSize = Mathf.Max(1, m_mainShapeSize);
        m_mainShapeSize = Mathf.NextPowerOfTwo(m_mainShapeSize);

        m_mainShapeDepth = Mathf.Max(1, m_mainShapeDepth);
        m_mainShapeDepth = Mathf.NextPowerOfTwo(m_mainShapeDepth);

        if (GUILayout.Button("Generate MainShape3D"))
        {
            DoGenerateMainShape3D();
        }
        if (GUILayout.Button("Generate MainShape2D"))
        {
            DoGenerateMainShape2D();
        }
        GUILayout.Space(20);

        m_detailShapeSize = EditorGUILayout.IntField("DetailShape Size", m_detailShapeSize);
        m_detailShapeSize = Mathf.Max(4, m_detailShapeSize);
        m_detailShapeSize = Mathf.NextPowerOfTwo(m_detailShapeSize);
        if (GUILayout.Button("Generate DetailShape"))
        {
            DoGenerateDetailShape();
        }

        GUILayout.Space(20);
        m_so.Update();
        EditorGUILayout.PropertyField(m_sp);
        m_so.ApplyModifiedProperties();
        //if (GUILayout.Button("Generate BlueNoise"))
        //{
        //    GenerateBlueNoise3D();
        //}
    }
    bool GetBlueNoiseSize(out int size, out int depth)
    {
        size = 1;
        depth = 1;
        if (m_blueNoise == null || m_blueNoise.Length == 0)
            return false;
        
        for (int i = 0; i < m_blueNoise.Length; ++i)
        {
            if (m_blueNoise[i] == null)
            {
                Debug.LogError("blue noise texture is null. index:" + i);
                return false;
            }
            if (m_blueNoise[i].width != m_blueNoise[i].height)
            {
                Debug.LogError("blue noise width != height.", m_blueNoise[i]);
                return false;
            }
            if (size == 1)
            {
                size = m_blueNoise[i].width;
            }
            if (size != m_blueNoise[i].width)
            {
                Debug.LogError("blue noise width != height.", m_blueNoise[i]);
                return false;
            }
        }
        depth = m_blueNoise.Length;
        return true;
    }
    void GenerateBlueNoise3D()
    {
        if (!GetBlueNoiseSize(out var size, out var depth))
        {
            return;
        }
        Texture3D tex3D = new Texture3D(size, depth, size, TextureFormat.R8, false);
        tex3D.filterMode = FilterMode.Point;

        for (int z = 0; z < depth; ++z)
        {
            Texture2D tex2D = m_blueNoise[z];

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    var color = tex2D.GetPixel(x, y);
                    tex3D.SetPixel(x, z, y, color);
                }
            }
        }
        tex3D.Apply();

        AssetDatabase.CreateAsset(tex3D, "Assets/BlueNoise.asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    void DoGenerateDetailShape()
    {
        int size = m_detailShapeSize;
        Texture3D tex3D = new Texture3D(size, size / 2, size, TextureFormat.R8, true);

        Material mat = new Material(Shader.Find("Hidden/DetailShape"));
        for (int z = 0; z < size / 2; ++z)
        {
            RenderTexture singleRT = new RenderTexture(size, size, 0, RenderTextureFormat.RHalf);
            mat.SetFloat("_Layer", z / (float)(size - 1));// - 1
            Graphics.Blit(null, singleRT, mat);

            RenderTexture.active = singleRT;

            Texture2D tex2D = new Texture2D(size, size, TextureFormat.R8, false);
            tex2D.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            tex2D.Apply();

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    var color = tex2D.GetPixel(x, y);
                    tex3D.SetPixel(x, z, y, color);
                }
            }
          
            singleRT.Release();
            Texture2D.DestroyImmediate(tex2D);
        }
        
        tex3D.Apply();
        AssetDatabase.CreateAsset(tex3D, "Assets/DetailShape.asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    void DoGenerateMainShape2D()
    {
        //
        int size = m_mainShapeSize;
        Texture2D tex2D = new Texture2D(size, size, TextureFormat.R8, true);

        Material mat = new Material(Shader.Find("Hidden/MainShape"));

        RenderTexture singleRT = new RenderTexture(size, size, 0, RenderTextureFormat.R8);

        mat.SetFloat("_Frequency", m_mainShapeFrequency);
        mat.SetVector("_Size", new Vector3(size, size, size));
        Graphics.Blit(null, singleRT, mat, mat.FindPass("MainShape2D"));

        RenderTexture.active = singleRT;

        tex2D.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        tex2D.Apply();

        singleRT.Release();

        //
        AssetDatabase.CreateAsset(tex2D, "Assets/MainShape2D.asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

    }
    void DoGenerateMainShape3D()
    {
        //
        int size = m_mainShapeSize;
        int depth = m_mainShapeDepth;
        Texture3D tex3D = new Texture3D(size, depth, size, TextureFormat.R8, true);

        Material mat = new Material(Shader.Find("Hidden/MainShape"));
        for (int z = 0; z < depth; ++z)
        {
            RenderTexture singleRT = new RenderTexture(size, size, 0, RenderTextureFormat.R8);
            mat.SetFloat("_Layer", z / (float)(depth - 1));// 
            mat.SetFloat("_Coverage", m_mainShapeCoverage);
            mat.SetFloat("_Frequency", m_mainShapeFrequency);
            mat.SetVector("_Size", new Vector3(size, depth, size));
            Graphics.Blit(null, singleRT, mat, mat.FindPass("MainShape3D"));

            RenderTexture.active = singleRT;

            Texture2D tex2D = new Texture2D(size, size, TextureFormat.R8, false);
            tex2D.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            tex2D.Apply();

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    var color = tex2D.GetPixel(x, y);
                    tex3D.SetPixel(x, z, y, color);
                }
            }

            singleRT.Release();
            Texture2D.DestroyImmediate(tex2D);
        }
        //先移除SDF优化
        //GenerateSDFCPU(tex3D); 
        tex3D.Apply();
        //
        
        //Texture3D tex3DSimple = new Texture3D(size, depth, size, TextureFormat.R8, true);
        //NativeArray<byte> rawData = new NativeArray<byte>(size * size * depth * 2, Allocator.Temp); // 2 channels

        //Color[] sourceColors = tex3D.GetPixels();

        //for (int i = 0; i < sourceColors.Length; i++)
        //{
        //    var color = sourceColors[i];
        //    int rawIndex = i * 2;

        //    rawData[rawIndex + 0] = (byte)Mathf.Clamp(color.r * 255, 0, 255);
        //    rawData[rawIndex + 1] = (byte)Mathf.Clamp(color.g / 32f * 255, 0, 255); //如果depth的分辨率变得比32大，那么有可能sdf的最大距离会大于32，这里就需要修改一下
        //}

        //tex3DSimple.SetPixelData<byte>(rawData, 0);
        //tex3DSimple.Apply(true);

        AssetDatabase.CreateAsset(tex3D, "Assets/MainShape3D.asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        //tex3D.Dispose();
    }

    float[,,] sdfData;
    float[,,] densityData;
    float densityThreshold = 0.01f;
    void GenerateSDFCPU(Texture3D tex)
    {
        var textureSize = new Vector3Int(
            tex.width,
            tex.height,
            tex.depth
        );

        densityData = new float[textureSize.x, textureSize.y, textureSize.z];

        Color[] pixels = tex.GetPixels();

        for (int z = 0; z < textureSize.z; z++)
        {
            for (int y = 0; y < textureSize.y; y++)
            {
                for (int x = 0; x < textureSize.x; x++)
                {
                    int index = x + y * textureSize.x + z * textureSize.x * textureSize.y;
                    densityData[x, y, z] = pixels[index].r; // 密度存储在红色通道
                }
            }
        }

        GenerateSDFFastSweeping(textureSize);
        //
        for (int z = 0; z < textureSize.z; z++)
        {
            for (int y = 0; y < textureSize.y; y++)
            {
                for (int x = 0; x < textureSize.x; x++)
                {
                    float minDist = sdfData[x, y, z];
                    //minDist = Mathf.Clamp01(minDist / 255.0f);
                    Color color = tex.GetPixel(x, y, z);
                    color.g = minDist;
                    tex.SetPixel(x, y, z, color);
                }
            }
        }
        
    }


    // 方法2：Fast Sweeping Algorithm（O(n^3) - 推荐）
    private void GenerateSDFFastSweeping(Vector3Int textureSize)
    {
        sdfData = new float[textureSize.x, textureSize.y, textureSize.z];

        // 初始化
        for (int x = 0; x < textureSize.x; x++)
        {
            for (int y = 0; y < textureSize.y; y++)
            {
                for (int z = 0; z < textureSize.z; z++)
                {
                    if (densityData[x, y, z] > densityThreshold)
                    {
                        sdfData[x, y, z] = 0.0f; // 种子点
                    }
                    else
                    {
                        sdfData[x, y, z] = float.MaxValue; // 初始距离无穷大
                    }
                }
            }
        }

        // Fast Sweeping: 8个方向扫描
        int[] dx = { 1, -1, 1, -1, 1, -1, 1, -1 };
        int[] dy = { 1, 1, -1, -1, 1, 1, -1, -1 };
        int[] dz = { 1, 1, 1, 1, -1, -1, -1, -1 };

        // 进行多次扫描直到收敛
        for (int sweep = 0; sweep < 8; sweep++)
        {
            SweepDirection(textureSize, dx[sweep], dy[sweep], dz[sweep]);
        }
    }

    private void SweepDirection(Vector3Int textureSize, int dx, int dy, int dz)
    {
        int startX = dx > 0 ? 0 : textureSize.x - 1;
        int endX = dx > 0 ? textureSize.x : -1;
        int startY = dy > 0 ? 0 : textureSize.y - 1;
        int endY = dy > 0 ? textureSize.y : -1;
        int startZ = dz > 0 ? 0 : textureSize.z - 1;
        int endZ = dz > 0 ? textureSize.z : -1;

        for (int x = startX; x != endX; x += dx)
        {
            for (int y = startY; y != endY; y += dy)
            {
                for (int z = startZ; z != endZ; z += dz)
                {
                    if (densityData[x, y, z] > densityThreshold)
                        continue; // 跳过种子点

                    float minDist = sdfData[x, y, z];

                    // 检查6个邻居
                    int[] neighbors = { -1, 1 };

                    foreach (int offsetX in neighbors)
                    {
                        int nx = x + offsetX;
                        //我们的图是一个tileable的，所以在求解tileable sdf时，溢出的部分要回环到另一侧
                        nx = nx < 0 ? (nx + textureSize.x) : nx;
                        nx = nx >= textureSize.x ? (nx - textureSize.x) : nx;
                        //if (nx >= 0 && nx < textureSize.x)
                        {
                            minDist = Mathf.Min(minDist, sdfData[nx, y, z] + 1.0f);
                        }
                    }

                    foreach (int offsetY in neighbors)
                    {
                        int ny = y + offsetY;
                        ny = ny < 0 ? (ny + textureSize.y) : ny;
                        ny = ny >= textureSize.y ? (ny - textureSize.y) : ny;
                        //if (ny >= 0 && ny < textureSize.y)
                        {
                            minDist = Mathf.Min(minDist, sdfData[x, ny, z] + 1.0f);
                        }
                    }

                    foreach (int offsetZ in neighbors)
                    {
                        int nz = z + offsetZ;
                        nz = nz < 0 ? (nz + textureSize.z) : nz;
                        nz = nz >= textureSize.z ? (nz - textureSize.z) : nz;
                        //if (nz >= 0 && nz < textureSize.z)
                        {
                            minDist = Mathf.Min(minDist, sdfData[x, y, nz] + 1.0f);
                        }
                    }

                    sdfData[x, y, z] = minDist;
                }
            }
        }
    }
}
