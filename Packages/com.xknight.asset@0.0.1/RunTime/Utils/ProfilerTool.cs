using UnityEngine;

namespace XKAsset
{
    /// <summary>
    /// 耗时工具
    /// </summary>
    public class ProfilerTool
    {
        private float _time;

        public ProfilerTool()
        {
            _time = Time.realtimeSinceStartup;
        }

        public void TimeTag(string tag)
        {
            var time = Time.realtimeSinceStartup - _time;
            Debug.LogError(tag + " : " + time);
            _time = Time.realtimeSinceStartup;
        }
    }
}