/*******************************************************************************
 * File: TimeOfDayFixedTime.cs
 * Author: WangYu
 * Date: 2026-01-09
 * Description: 设置 TOD 的固定时段，有的场景没有 TimeOfDayManager 管理时态数据，
 *              但是又想让分时态的自发光逻辑生效（少用材质球），就需要通过这个脚本来实现
 *
 * Notice: 
 *******************************************************************************/

using System;
using UnityEngine;

namespace XKT.TOD
{
    [ExecuteAlways]
    [AddComponentMenu("TOD/TimeOfDayFixedTime")]
    public class TimeOfDayFixedTime : MonoBehaviour
    {
        [Range(0, 3)]
        public int fixedIndex;

        private void OnDisable()
        {
            ChangeIndex(0);
        }
        
        private void OnEnable()
        {
            ChangeIndex(fixedIndex);
        }
        
        private void OnValidate()
        {
            ChangeIndex(fixedIndex);
        }

        private void ChangeIndex(int index)
        {
            Shader.SetGlobalFloat(StoredTimeOfDayDataRestorer.SPID_TODTimeIndex, index);
        }
        
    }
}