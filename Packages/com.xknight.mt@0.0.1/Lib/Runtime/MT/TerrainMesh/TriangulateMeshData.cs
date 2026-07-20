using System;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.TerrainMesh
{
    /// <summary>
    /// 三角测量的网格数据
    /// </summary>
    public class TriangulateMeshData
    {
        /// <summary>
        /// lod 数据
        /// </summary>
        public class LOD
        {
            /// <summary>
            /// 顶点
            /// </summary>
            public Vector3[] vertices = Array.Empty<Vector3>();
            /// <summary>
            /// 法线
            /// </summary>
            public Vector3[] normals = Array.Empty<Vector3>();
            /// <summary>
            /// uv
            /// </summary>
            public Vector2[] uvs = Array.Empty<Vector2>();
            /// <summary>
            /// 三角面索引
            /// </summary>
            public int[] faces = Array.Empty<int>();
            
            /// <summary>
            /// 最小uv
            /// </summary>
            public Vector2 uvMin;
            /// <summary>
            /// 最大uv
            /// </summary>
            public Vector2 uvMax;
        }

        /// <summary>
        /// 网格id
        /// </summary>
        public int MeshId { get; private set; }
        /// <summary>
        /// 包围盒
        /// </summary>
        public Bounds BND { get; private set; }
        
        /// <summary>
        /// 当前mesh数据所属的lod级别
        /// </summary>
        public int lodLv = -1;

        /// <summary>
        /// lod 数据，当 lodLv 有值时，这个数组只有一个元素
        /// </summary>
        public LOD[] lods = Array.Empty<LOD>();

        
        /// <summary>
        /// 构造 - 用于保存多级lod数据
        /// </summary>
        public TriangulateMeshData(int id, Bounds bnd)
        {
            MeshId = id;
            BND = bnd;
        }

        /// <summary>
        /// 构造 - 用于保存单级lod数据
        /// </summary>
        public TriangulateMeshData(int id, Bounds bnd, int lv)
        {
            MeshId = id;
            BND = bnd;
            lodLv = lv;
        }
    }
}