namespace AirSticker.Runtime.Core
{
    /// <summary>
    /// 凸多边形信息包装
    /// </summary>
    public class ConvexPolygonInfo
    {
        public ConvexPolygon ConvexPolygon { get; set; }

        /// <summary>
        /// 此标志指示凸多边形是否位于贴花框定义的剪辑空间之外。
        /// </summary>
        public bool IsOutsideClipSpace { get; set; }
    }
}