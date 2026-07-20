using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ShaderHotSwap.Util
{
    public class EditorCoroutine
    {
        public static EditorCoroutine Start(IEnumerator coroutine)
        {
            var editorCoroutine = new EditorCoroutine(coroutine);
            editorCoroutine.EditorStart();
            return editorCoroutine;
        }

        private IEnumerator m_coroutine;
        private CustomYieldInstruction m_currentYield;

        private EditorCoroutine(IEnumerator coroutine)
        {
            m_coroutine = coroutine;
        }

        private void EditorStart()
        {
            m_currentYield = null;
            EditorApplication.update += EditorUpdate;
        }

        public void EditorStop()
        {
            EditorApplication.update -= EditorUpdate;
        }

        private void EditorUpdate()
        {
            if (m_currentYield != null && !m_currentYield.IsDone)
            {
                return;
            }

            if (!m_coroutine.MoveNext())
            {
                EditorStop();
            }

            m_currentYield = m_coroutine.Current as CustomYieldInstruction;
        }

        /// <summary>
        /// 自定义 Yield 指令
        /// </summary>
        public class CustomYieldInstruction
        {
            private Func<bool> m_isDone;
            public virtual bool IsDone => m_isDone();
            
            public CustomYieldInstruction(Func<bool> isDone = null)
            {
                m_isDone = isDone;
            }
        }

        /// <summary>
        /// 自定义等待秒数
        /// </summary>
        public class CustomWaitForSeconds : CustomYieldInstruction
        {
            private double m_begin;
            private float m_seconds;

            public CustomWaitForSeconds(float seconds)
            {
                m_begin = EditorApplication.timeSinceStartup;
                m_seconds = seconds;
            }

            public override bool IsDone
            {
                get
                {
                    double diff = EditorApplication.timeSinceStartup - m_begin;
                    bool result = diff >= m_seconds;
                    return result;
                }
            }
        }
    }
}