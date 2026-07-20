using System.IO;
using System.Text;
using UnityEngine;

public class OBJExporter : MonoBehaviour
{
    public void ExportMesh(MeshFilter meshFilter, string fileName)
    {
        Mesh mesh = meshFilter.sharedMesh;
        StringBuilder sb = new StringBuilder();

        // 添加顶点
        foreach (Vector3 vertex in mesh.vertices)
            sb.AppendFormat("v {0} {1} {2}\n", vertex.x, vertex.y, vertex.z);

        // 添加UV
        foreach (Vector2 uv in mesh.uv)
            sb.AppendFormat("vt {0} {1}\n", uv.x, uv.y);

        // 添加法线
        foreach (Vector3 normal in mesh.normals)
            sb.AppendFormat("vn {0} {1} {2}\n", normal.x, normal.y, normal.z);

        // 添加三角形面
        int[] triangles = mesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int idx1 = triangles[i] + 1;
            int idx2 = triangles[i + 1] + 1;
            int idx3 = triangles[i + 2] + 1;
            sb.AppendFormat("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", idx1, idx2, idx3);
        }

        // 写入文件
        string path = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllText(path, sb.ToString());
        Debug.Log("OBJ saved to: " + path);
    }
}