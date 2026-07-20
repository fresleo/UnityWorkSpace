using System;

namespace UnityPie
{
    /// <summary>
    /// 饼菜单属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class PieMenuAttribute : Attribute
    {
        public string path;

        public PieMenuAttribute()
        {
        }

        public PieMenuAttribute(string path)
        {
            this.path = path;
        }
    }
}