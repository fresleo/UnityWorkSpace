using System;
using System.Text;
using UnityEngine;

namespace GRTools.Localization
{
    /// <summary>
    /// 二进制反序列化
    /// </summary>
    public partial class BinarySerializer
    {
        private byte[] buffer;
        private int offset;

        public bool Finish
        {
            get
            {
                if (buffer == null)
                {
                    return true;
                }

                return buffer.Length == offset;
            }
        }

        public BinarySerializer(byte[] _buffer)
        {
            buffer = _buffer;
            offset = 0;
        }

        #region Deserialize Methods

        public void Deserialize(ref bool b)
        {
            b = buffer[offset] != 0;
            offset++;
        }

        public void Deserialize(ref byte b)
        {
            b = buffer[offset];
            offset++;
        }

        public void Deserialize(ref short s)
        {
            s = BitConverter.ToInt16(buffer, offset);
            offset += 2;
        }

        public void Deserialize(ref int i)
        {
            i = BitConverter.ToInt32(buffer, offset);
            offset += 4;
        }

        public void Deserialize(ref int[] arrayInt)
        {
            int length = 0;
            Deserialize(ref length);

            if (length <= 0)
            {
                return;
            }

            arrayInt = new int[length];
            for (int i = 0; i < length; i++)
            {
                Deserialize(ref arrayInt[i]);
            }
        }

        public void Deserialize(ref float f)
        {
            f = BitConverter.ToSingle(buffer, offset);
            offset += 4;
        }

        public void Deserialize(ref float[] arrayFloat)
        {
            int length = 0;
            Deserialize(ref length);

            if (length <= 0)
            {
                return;
            }

            arrayFloat = new float[length];
            for (int i = 0; i < length; i++)
            {
                Deserialize(ref arrayFloat[i]);
            }
        }

        public void Deserialize(ref long l)
        {
            l = BitConverter.ToInt64(buffer, offset);
            offset += 8;
        }

        public void Deserialize(ref long[] arrayLong)
        {
            int length = 0;
            Deserialize(ref length);

            if (length <= 0)
            {
                return;
            }

            arrayLong = new long[length];
            for (int i = 0; i < length; i++)
            {
                Deserialize(ref arrayLong[i]);
            }
        }

        public void Deserialize(ref double d)
        {
            d = BitConverter.ToDouble(buffer, offset);
            offset += 8;
        }

        public void Deserialize(ref double[] arrayDouble)
        {
            int length = 0;
            Deserialize(ref length);

            if (length <= 0)
            {
                return;
            }

            arrayDouble = new double[length];
            for (int i = 0; i < length; i++)
            {
                Deserialize(ref arrayDouble[i]);
            }
        }

        public void Deserialize(ref string str)
        {
            int length = 0;
            Deserialize(ref length);

            if (length <= 0)
            {
                return;
            }

            str = Encoding.UTF8.GetString(buffer, offset, length).Replace("\\n", Environment.NewLine);
            offset += length;
        }

        public void Deserialize(ref string[] arrayString)
        {
            int length = 0;
            Deserialize(ref length);

            if (length <= 0)
            {
                return;
            }

            arrayString = new string[length];
            for (int i = 0; i < length; i++)
            {
                Deserialize(ref arrayString[i]);
            }
        }

        public void Deserialize(ref ushort us)
        {
            us = BitConverter.ToUInt16(buffer, offset);
            offset += 2;
        }

        public void Deserialize(ref uint ui)
        {
            ui = BitConverter.ToUInt32(buffer, offset);
            offset += 4;
        }

        public void Deserialize(ref ulong ul)
        {
            ul = BitConverter.ToUInt64(buffer, offset);
            offset += 8;
        }

        public void Deserialize(ref Vector3 vector3)
        {
            Deserialize(ref vector3.x);
            Deserialize(ref vector3.y);
            Deserialize(ref vector3.z);
        }

        #endregion
    }
}