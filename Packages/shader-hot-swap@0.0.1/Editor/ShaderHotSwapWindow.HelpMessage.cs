using System.Collections;
using ShaderHotSwap.Util;
using UnityEditor;
using UnityEngine;

namespace ShaderHotSwap
{
    public partial class ShaderHotSwapWindow
    {
        // 帮助信息最少显示时间
        private const float c_minHelpMessageShowSec = 1f;
        
        private string m_helpMessage;
        private MessageType m_helpMessageType;
        private double m_lastMessageTime;
        private int m_lastMessageIndex;

        protected void ShowHelpMessage()
        {
            if(string.IsNullOrEmpty(m_helpMessage)) return;
            
            //EditorGUILayout.HelpBox(m_helpMessage, m_helpMessageType);
            var width = EditorGUIUtility.currentViewWidth - 20;
            var height = EditorStyles.helpBox.CalcHeight(new GUIContent(m_helpMessage), width);
            height = Mathf.Max(40, height);
            var rc = new Rect(10, 20, width, height);
            EditorGUI.HelpBox(rc, m_helpMessage, m_helpMessageType);
            EditorGUI.HelpBox(rc, m_helpMessage, m_helpMessageType);
            EditorGUI.HelpBox(rc, m_helpMessage, m_helpMessageType);
        }

        protected bool HasErrorMessage()
        {
            return !string.IsNullOrEmpty(m_helpMessage) && m_helpMessageType == MessageType.Error;
        }

        protected virtual void SetHelpMessage(string message, MessageType type, bool autoClear = true)
        {
            if (type == MessageType.Error)
            {
                Debug.LogError(message);
            }

            m_helpMessage = message;
            m_helpMessageType = type;
            m_lastMessageTime = EditorApplication.timeSinceStartup;
            m_lastMessageIndex++;
            Repaint();

            if (autoClear)
            {
                ClearHelpMessage();
            }
        }

        private void ClearHelpMessageImpl()
        {
            m_helpMessage = null;
            Repaint();
        }
        
        protected void ClearHelpMessage()
        {
            double diff = EditorApplication.timeSinceStartup - m_lastMessageTime;
            if (diff >= c_minHelpMessageShowSec)
            {
                ClearHelpMessageImpl();
            }
            else
            {
                EditorCoroutine.Start(DelayedClearHelpMessage(m_lastMessageIndex));
            }
        }

        private IEnumerator DelayedClearHelpMessage(int clearMessageIndex)
        {
            yield return new EditorCoroutine.CustomWaitForSeconds(c_minHelpMessageShowSec);

            if (m_lastMessageIndex == clearMessageIndex)
            {
                ClearHelpMessageImpl();
            }
        }
        
    }
}