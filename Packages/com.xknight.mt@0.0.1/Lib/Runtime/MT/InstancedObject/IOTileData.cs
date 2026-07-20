// Created By: WangYu  Date: 2023-12-01

using System.IO;
using com.xknight.mt.Lib.Runtime.MT.Serialize;
using com.xknight.mt.Lib.Runtime.MT.Utils;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.InstancedObject
{
    public struct IOTileData
    {
        public Bounds bnd;
        public Matrix4x4 matrix;
        public LightmapConfig lmc;
    }

    public static class IOTileDataExt
    {
        public static void Serialize(Stream stream, IOTileData data)
        {
            var bnd = data.bnd;
            MTStreamUtils.WriteVector3(stream, bnd.center);
            MTStreamUtils.WriteVector3(stream, bnd.size);

            Matrix4x4 matr = data.matrix;
            MTStreamUtils.WriteVector4(stream, matr.GetColumn(0));
            MTStreamUtils.WriteVector4(stream, matr.GetColumn(1));
            MTStreamUtils.WriteVector4(stream, matr.GetColumn(2));
            MTStreamUtils.WriteVector4(stream, matr.GetColumn(3));
            
            data.lmc.Serialize(stream);
        }

        public static IOTileData Deserialize(Stream stream)
        {
            IOTileData data = new IOTileData();

            Vector3 center = MTStreamUtils.ReadVector3(stream);
            Vector3 size = MTStreamUtils.ReadVector3(stream);
            data.bnd = new Bounds(center, size);
            
            var column0 = MTStreamUtils.ReadVector4(stream);
            var column1 = MTStreamUtils.ReadVector4(stream);
            var column2 = MTStreamUtils.ReadVector4(stream);
            var column3 = MTStreamUtils.ReadVector4(stream);
            data.matrix = new Matrix4x4(column0, column1, column2, column3);

            data.lmc = new LightmapConfig();
            data.lmc.Deserialize(stream);
            
            return data;
        }
        
    }
}