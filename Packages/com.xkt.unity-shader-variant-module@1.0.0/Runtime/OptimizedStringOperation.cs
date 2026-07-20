/**
The MIT License (MIT)

Copyright (c) 2016 Yusuke Kurokawa

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */

using System.Threading;
using System.Text;

namespace StringOperationUtil
{
    /// <summary>
    /// Using this,you can optimize string concat operation easily.
    /// To use this , you should put this on the top of code.
    /// ------
    /// using StrOpe = StringOperationUtil.OptimizedStringOperation;
    /// ------
    /// 
    /// - before code
    /// string str = "aaa" + 20 + "bbbb";
    /// 
    /// - after code
    /// string str = StrOpe.i + "aaa" + 20 + "bbbb";
    /// 
    /// "StrOpe.i" is for MainThread , do not call from other theads.
    /// If "StrOpe.i" is called from Mainthread , reuse same object.
    /// 
    /// You can also use "StrOpe.small" / "StrOpe.medium" / "StrOpe.large" instead of "StrOpe.i". 
    /// These are creating instance.
    /// </summary>
    public class OptimizedStringOperation
    {
        private static OptimizedStringOperation s_instance = null;
#if !UNITY_WEBGL
        private static Thread s_singletonThread = null;
#endif
        private StringBuilder m_sb = null;

        static OptimizedStringOperation()
        {
            s_instance = new OptimizedStringOperation(1024);
        }

        private OptimizedStringOperation(int capacity)
        {
            m_sb = new StringBuilder(capacity);
        }

        public static OptimizedStringOperation Create(int capacity)
        {
            return new OptimizedStringOperation(capacity);
        }

        public static OptimizedStringOperation small => Create(64);

        public static OptimizedStringOperation medium => Create(256);

        public static OptimizedStringOperation large => Create(1024);

        public static OptimizedStringOperation i
        {
            get
            {
#if !UNITY_WEBGL
                // Bind instance to thread.
                if (s_singletonThread == null)
                {
                    s_singletonThread = Thread.CurrentThread;
                }

                // check thread...
                if (s_singletonThread != Thread.CurrentThread)
                {
#if DEBUG || UNITY_EDITOR
                    UnityEngine.Debug.LogError("Execute from another thread.");
#endif
                    return small;
                }
#endif

                s_instance.m_sb.Length = 0;
                return s_instance;
            }
        }

        public int Capacity
        {
            set => this.m_sb.Capacity = value;
            get => m_sb.Capacity;
        }

        public int Length
        {
            set => this.m_sb.Length = value;
            get => this.m_sb.Length;
        }

        public OptimizedStringOperation Remove(int startIndex, int length)
        {
            m_sb.Remove(startIndex, length);
            return this;
        }

        public OptimizedStringOperation Replace(string oldValue, string newValue)
        {
            m_sb.Replace(oldValue, newValue);
            return this;
        }

        public override string ToString()
        {
            return m_sb.ToString();
        }

        public void Clear()
        {
            // StringBuilder.Clear() doesn't support .Net 3.5...
            // "Capasity = 0" doesn't work....
            m_sb = new StringBuilder(0);
        }

        public OptimizedStringOperation ToLower()
        {
            int length = m_sb.Length;
            for (int i = 0; i < length; ++i)
            {
                if (char.IsUpper(m_sb[i]))
                {
                    m_sb.Replace(m_sb[i], char.ToLower(m_sb[i]), i, 1);
                }
            }

            return this;
        }

        public OptimizedStringOperation ToUpper()
        {
            int length = m_sb.Length;
            for (int i = 0; i < length; ++i)
            {
                if (char.IsLower(m_sb[i]))
                {
                    m_sb.Replace(m_sb[i], char.ToUpper(m_sb[i]), i, 1);
                }
            }

            return this;
        }

        public OptimizedStringOperation Trim()
        {
            return TrimEnd().TrimStart();
        }

        public OptimizedStringOperation TrimStart()
        {
            int length = m_sb.Length;
            for (int i = 0; i < length; ++i)
            {
                if (!char.IsWhiteSpace(m_sb[i]))
                {
                    if (i > 0)
                    {
                        m_sb.Remove(0, i);
                    }

                    break;
                }
            }

            return this;
        }

        public OptimizedStringOperation TrimEnd()
        {
            int length = m_sb.Length;
            for (int i = length - 1; i >= 0; --i)
            {
                if (!char.IsWhiteSpace(m_sb[i]))
                {
                    if (i < length - 1)
                    {
                        m_sb.Remove(i, length - i);
                    }

                    break;
                }
            }

            return this;
        }


        public static implicit operator string(OptimizedStringOperation t)
        {
            return t.ToString();
        }

        #region ADD_OPERATOR

        public static OptimizedStringOperation operator +(OptimizedStringOperation t, bool v)
        {
            t.m_sb.Append(v);
            return t;
        }

        public static OptimizedStringOperation operator +(OptimizedStringOperation t, int v)
        {
            t.m_sb.Append(v);
            return t;
        }

        public static OptimizedStringOperation operator +(OptimizedStringOperation t, short v)
        {
            t.m_sb.Append(v);
            return t;
        }

        public static OptimizedStringOperation operator +(OptimizedStringOperation t, byte v)
        {
            t.m_sb.Append(v);
            return t;
        }

        public static OptimizedStringOperation operator +(OptimizedStringOperation t, float v)
        {
            t.m_sb.Append(v);
            return t;
        }

        public static OptimizedStringOperation operator +(OptimizedStringOperation t, char c)
        {
            t.m_sb.Append(c);
            return t;
        }

        public static OptimizedStringOperation operator +(OptimizedStringOperation t, char[] c)
        {
            t.m_sb.Append(c);
            return t;
        }

        public static OptimizedStringOperation operator +(OptimizedStringOperation t, string str)
        {
            t.m_sb.Append(str);
            return t;
        }

        public static OptimizedStringOperation operator +(OptimizedStringOperation t, StringBuilder sb)
        {
            t.m_sb.Append(sb);
            return t;
        }

        #endregion ADD_OPERATOR
    }
}