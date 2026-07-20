using System;
using UnityEngine;
using System.IO;
using System.Text;

namespace GRTools.Localization
{
    /// <summary>
    /// 二进制序列化
    /// </summary>
    public partial class BinarySerializer
    {
        private MemoryStream ms = null;

        public BinarySerializer()
        {
            ms = new MemoryStream();
        }

        #region Serialize Methods

        public void Serialize(bool b)
        {
            byte[] bytes = BitConverter.GetBytes(b);
            ms.Write(bytes, 0, 1);
            offset += 1;
        }

        public void Serialize(int i)
        {
            byte[] bytes = BitConverter.GetBytes(i);
            ms.Write(bytes, 0, 4);
            offset += 4;
        }

        public void Serialize(long l)
        {
            byte[] bytes = BitConverter.GetBytes(l);
            ms.Write(bytes, 0, 8);
            offset += 8;
        }

        public void Serialize(float f)
        {
            byte[] bytes = BitConverter.GetBytes(f);
            ms.Write(bytes, 0, 4);
            offset += 4;
        }

        public void Serialize(double d)
        {
            byte[] bytes = BitConverter.GetBytes(d);
            ms.Write(bytes, 0, 8);
            offset += 8;
        }

        public void Serialize(string s)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            int length = bytes.Length;
            Serialize(length);

            ms.Write(bytes, 0, length);
            offset += length;
        }

        public void Serialize(int[] arrayInt)
        {
            if (arrayInt == null)
            {
                Serialize(0);
                return;
            }

            int length = arrayInt.Length;
            Serialize(length);

            for (int i = 0; i < length; i++)
            {
                Serialize(arrayInt[i]);
            }
        }

        public void Serialize(long[] arrayLong)
        {
            if (arrayLong == null)
            {
                Serialize(0);
                return;
            }

            int length = arrayLong.Length;
            Serialize(length);

            for (int i = 0; i < length; i++)
            {
                Serialize(arrayLong[i]);
            }
        }

        public void Serialize(float[] arrayFloat)
        {
            if (arrayFloat == null)
            {
                Serialize(0);
                return;
            }

            int length = arrayFloat.Length;
            Serialize(length);

            for (int i = 0; i < length; i++)
            {
                Serialize(arrayFloat[i]);
            }
        }

        public void Serialize(double[] arrayDouble)
        {
            if (arrayDouble == null)
            {
                Serialize(0);
                return;
            }

            int length = arrayDouble.Length;
            Serialize(length);

            for (int i = 0; i < length; i++)
            {
                Serialize(arrayDouble[i]);
            }
        }

        public void Serialize(string[] arrayString)
        {
            if (arrayString == null)
            {
                Serialize(0);
                return;
            }

            int length = arrayString.Length;
            Serialize(length);

            for (int i = 0; i < length; i++)
            {
                Serialize(arrayString[i]);
            }
        }

        public void Serialize(Vector3 vector)
        {
            for (int i = 0; i < 3; i++)
            {
                Serialize(vector[i]);
            }
        }

        #endregion

        /// <summary>
        /// 获取序列化后的byte数组数据
        /// </summary>
        /// <returns></returns>
        public byte[] GetBuffer()
        {
            var bytes = ms.GetBuffer();
            byte[] desArray = new byte[offset];
            Array.Copy(bytes, 0, desArray, 0, offset);

            return desArray;
        }
    }
}