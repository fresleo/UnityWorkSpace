// Created By: WangYu  Date: 2022-10-12

using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.Detail
{
    /// <summary>
    /// 细节Patch淡入淡出动画
    /// </summary>
    internal class DetailPatchCutoffAnim
    {
        internal enum EState
        {
            /// <summary>
            /// 播放中
            /// </summary>
            Playing,
            /// <summary>
            /// 播完
            /// </summary>
            PlayDone,
        }
        
        const float c_cutoffMaxValue = 1.01f;
        const float c_cutoffAnimDuration = 0.3f;
        
        private static readonly int _Cutoff = Shader.PropertyToID("_Cutoff");
        
        private Material m_mat;
        private float m_cutoffAnimStartTime;
        private float m_cutoffVal = 0.5f;
        private float m_animCutoffVal = 0.5f;

        /// <summary>
        /// 状态
        /// </summary>
        public EState State { get; private set; }
        /// <summary>
        /// 反向
        /// </summary>
        public bool Reversed { get; private set; }
        
        /// <summary>
        /// 材质不可见
        /// </summary>
        public bool MatInvisible => State == EState.PlayDone && Reversed;
        
        public DetailPatchCutoffAnim(Material mat)
        {
            State = EState.PlayDone;
            Reversed = false;
            
            m_mat = mat;
            m_cutoffVal = m_mat.GetFloat(_Cutoff);
        }

        /// <summary>
        /// 重播
        /// </summary>
        /// <param name="reverse">反向</param>
        public void Replay(bool reverse)
        {
            //正在播放，且也没换方向时，什么也不用干
            if (State == EState.Playing && Reversed == reverse)
            {
                return;
            }
            Reversed = reverse;
            
            if (State == EState.Playing)
            {
                float timeSkiped = c_cutoffAnimDuration - (Time.time - m_cutoffAnimStartTime);
                m_cutoffAnimStartTime = Time.time - timeSkiped;
                InterpolateValue(timeSkiped);
            }
            else
            {
                m_cutoffAnimStartTime = Time.time;
                m_animCutoffVal = Reversed ? m_cutoffVal : c_cutoffMaxValue;
            }
            
            State = EState.Playing;
        }

        //插值
        private void InterpolateValue(float timePast)
        {
            float rate = timePast / c_cutoffAnimDuration;
            if (Reversed)
            {
                m_animCutoffVal = Mathf.Lerp(m_cutoffVal, c_cutoffMaxValue, rate);
            }
            else
            {
                m_animCutoffVal = Mathf.Lerp(c_cutoffMaxValue, m_cutoffVal, rate);
            }
        }

        /// <summary>
        /// 更新
        /// </summary>
        public void Update()
        {
            if (State == EState.PlayDone)
            {
                return;
            }
            
            float timePast = Time.time - m_cutoffAnimStartTime;
            if (timePast >= c_cutoffAnimDuration)
            {
                State = EState.PlayDone;
                timePast = c_cutoffAnimDuration;
            }

            InterpolateValue(timePast);
            m_mat.SetFloat(_Cutoff, m_animCutoffVal);
        }
        
    }
}