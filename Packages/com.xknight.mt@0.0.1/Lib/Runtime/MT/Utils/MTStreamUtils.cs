using System;
using System.IO;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.Utils
{
    /// <summary>
    /// 流操作实用工具
    /// </summary>
    public static class MTStreamUtils
    {
        public static void WriteByte(Stream stream, byte val)
        {
            byte[] sBuff = { val };
            stream.Write(sBuff, 0, sBuff.Length);
        }

        public static byte ReadByte(Stream stream)
        {
            byte[] sBuff = new byte[1];
            
            stream.Read(sBuff, 0, sBuff.Length);
            
            return sBuff[0];
        }

        public static void WriteInt(Stream stream, int val)
        {
            byte[] sBuff = BitConverter.GetBytes(val);
            stream.Write(sBuff, 0, sBuff.Length);
        }

        public static void WriteIntTo(Stream stream, int val, int offset)
        {
            byte[] sBuff = BitConverter.GetBytes(val);
            stream.Write(sBuff, offset, sBuff.Length);
        }

        public static int ReadInt(Stream stream)
        {
            int len = sizeof(int);
            byte[] sBuff = new byte[len];
            
            stream.Read(sBuff, 0, sBuff.Length);
            int val = BitConverter.ToInt32(sBuff, 0);
            
            return val;
        }

        public static void WriteUShort(Stream stream, ushort val)
        {
            byte[] sBuff = BitConverter.GetBytes(val);
            stream.Write(sBuff, 0, sBuff.Length);
        }

        public static ushort ReadUShort(Stream stream)
        {
            int len = sizeof(ushort);
            byte[] sBuff = new byte[len];
            
            stream.Read(sBuff, 0, sBuff.Length);
            ushort val = BitConverter.ToUInt16(sBuff, 0);
            
            return val;
        }

        public static void WriteFloat(Stream stream, float val)
        {
            byte[] sBuff = BitConverter.GetBytes(val);
            stream.Write(sBuff, 0, sBuff.Length);
        }

        public static float ReadFloat(Stream stream)
        {
            int len = sizeof(float);
            byte[] sBuff = new byte[len];
            
            stream.Read(sBuff, 0, sBuff.Length);
            float val = BitConverter.ToSingle(sBuff, 0);
            
            return val;
        }

        public static void WriteVector2(Stream stream, Vector2 val)
        {
            byte[] sBuff = BitConverter.GetBytes(val.x);
            stream.Write(sBuff, 0, sBuff.Length);
            
            sBuff = BitConverter.GetBytes(val.y);
            stream.Write(sBuff, 0, sBuff.Length);
        }

        public static Vector2 ReadVector2(Stream stream)
        {
            int len = sizeof(float);
            byte[] sBuff = new byte[len];
            Vector2 val = Vector2.zero;
            
            stream.Read(sBuff, 0, sBuff.Length);
            val.x = BitConverter.ToSingle(sBuff, 0);
            
            stream.Read(sBuff, 0, sBuff.Length);
            val.y = BitConverter.ToSingle(sBuff, 0);
            
            return val;
        }
        
        public static void WriteVector3(Stream stream, Vector3 val)
        {
            byte[] sBuff = BitConverter.GetBytes(val.x);
            stream.Write(sBuff, 0, sBuff.Length);
            
            sBuff = BitConverter.GetBytes(val.y);
            stream.Write(sBuff, 0, sBuff.Length);
            
            sBuff = BitConverter.GetBytes(val.z);
            stream.Write(sBuff, 0, sBuff.Length);
        }

        public static Vector3 ReadVector3(Stream stream)
        {
            int len = sizeof(float);
            byte[] sBuff = new byte[len];
            Vector3 val = Vector3.zero;
            
            stream.Read(sBuff, 0, sBuff.Length);
            val.x = BitConverter.ToSingle(sBuff, 0);
            
            stream.Read(sBuff, 0, sBuff.Length);
            val.y = BitConverter.ToSingle(sBuff, 0);
            
            stream.Read(sBuff, 0, sBuff.Length);
            val.z = BitConverter.ToSingle(sBuff, 0);
            
            return val;
        }

        public static void WriteVector4(Stream stream, Vector4 val)
        {
            byte[] sBuff = BitConverter.GetBytes(val.x);
            stream.Write(sBuff, 0, sBuff.Length);
            
            sBuff = BitConverter.GetBytes(val.y);
            stream.Write(sBuff, 0, sBuff.Length);
            
            sBuff = BitConverter.GetBytes(val.z);
            stream.Write(sBuff, 0, sBuff.Length);
            
            sBuff = BitConverter.GetBytes(val.w);
            stream.Write(sBuff, 0, sBuff.Length);
        }

        public static Vector4 ReadVector4(Stream stream)
        {
            int len = sizeof(float);
            byte[] sBuff = new byte[len];
            Vector4 val = Vector4.zero;
            
            stream.Read(sBuff, 0, sBuff.Length);
            val.x = BitConverter.ToSingle(sBuff, 0);
            
            stream.Read(sBuff, 0, sBuff.Length);
            val.y = BitConverter.ToSingle(sBuff, 0);
            
            stream.Read(sBuff, 0, sBuff.Length);
            val.z = BitConverter.ToSingle(sBuff, 0);
            
            stream.Read(sBuff, 0, sBuff.Length);
            val.w = BitConverter.ToSingle(sBuff, 0);
            
            return val;
        }
        
    }
}