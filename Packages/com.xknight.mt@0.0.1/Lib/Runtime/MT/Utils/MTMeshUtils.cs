// Created By: WangYu  Date: 2022-10-15

using System;
using System.Collections.Generic;
using System.IO;
using com.xknight.mt.Lib.Runtime.MT.TerrainMesh;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.Utils
{
    /// <summary>
    /// 网格实用工具
    /// </summary>
    public static class MTMeshUtils
    {
        /// <summary>
        /// 序列化
        /// </summary>
        public static void Serialize(Stream stream, TriangulateMeshData.LOD lod)
        {
            if (stream == null || lod == null)
            {
                return;
            }
            
            MTStreamUtils.WriteVector2(stream, lod.uvMin);
            MTStreamUtils.WriteVector2(stream, lod.uvMax);

            //vertices
            byte[] buffer = BitConverter.GetBytes(lod.vertices.Length);
            stream.Write(buffer, 0, buffer.Length);
            foreach (var v in lod.vertices)
            {
                MTStreamUtils.WriteVector3(stream, v);
            }

            //normals
            buffer = BitConverter.GetBytes(lod.normals.Length);
            stream.Write(buffer, 0, buffer.Length);
            foreach (var n in lod.normals)
            {
                MTStreamUtils.WriteVector3(stream, n);
            }

            //uvs
            buffer = BitConverter.GetBytes(lod.uvs.Length);
            stream.Write(buffer, 0, buffer.Length);
            foreach (var uv in lod.uvs)
            {
                MTStreamUtils.WriteVector2(stream, uv);
            }

            //faces
            buffer = BitConverter.GetBytes(lod.faces.Length);
            stream.Write(buffer, 0, buffer.Length);
            foreach (var face in lod.faces)
            {
                ushort val = (ushort)face;
                buffer = BitConverter.GetBytes(val);
                stream.Write(buffer, 0, buffer.Length);
            }
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        public static void Deserialize(Stream stream, TerrainMeshData tmd)
        {
            if (stream == null || tmd == null)
            {
                return;
            }
            
            tmd.mesh = new Mesh();
            tmd.uvMin = MTStreamUtils.ReadVector2(stream);
            tmd.uvMax = MTStreamUtils.ReadVector2(stream);
            
            //vertices
            byte[] iBuffer = new byte[sizeof(int)];
            List<Vector3> vec3Cache = new List<Vector3>();
            stream.Read(iBuffer, 0, sizeof(int));
            int len = BitConverter.ToInt32(iBuffer, 0);
            for (int i = 0; i < len; i++)
            {
                vec3Cache.Add(MTStreamUtils.ReadVector3(stream));
            }
            tmd.mesh.SetVertices(vec3Cache.ToArray());

            //normals
            vec3Cache.Clear();
            stream.Read(iBuffer, 0, sizeof(int));
            len = BitConverter.ToInt32(iBuffer, 0);
            for (int i = 0; i < len; i++)
            {
                vec3Cache.Add(MTStreamUtils.ReadVector3(stream));
            }
            tmd.mesh.SetNormals(vec3Cache.ToArray());

            //uvs
            List<Vector2> vec2Cache = new List<Vector2>();
            stream.Read(iBuffer, 0, sizeof(int));
            len = BitConverter.ToInt32(iBuffer, 0);
            for (int i = 0; i < len; i++)
            {
                vec2Cache.Add(MTStreamUtils.ReadVector2(stream));
            }
            tmd.mesh.SetUVs(0, vec2Cache.ToArray());

            //faces
            byte[] uBuffer = new byte[sizeof(ushort)];
            List<int> intCache = new List<int>();
            stream.Read(iBuffer, 0, sizeof(int));
            len = BitConverter.ToInt32(iBuffer, 0);
            for (int i = 0; i < len; i++)
            {
                stream.Read(uBuffer, 0, sizeof(ushort));
                intCache.Add(BitConverter.ToUInt16(uBuffer, 0));
            }
            tmd.mesh.SetTriangles(intCache.ToArray(), 0);
        }
        
    }
}