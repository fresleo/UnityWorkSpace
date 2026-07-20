using UnityEngine;

namespace Garena.TA
{
    /// <summary>
    /// 挂这个组件标记物体需要进行逐物体阴影
    /// </summary>
    [ExecuteAlways]
    public class GPerObjectShadowTarget : MonoBehaviour
    {
        /// <summary>
        /// 使用自定义包围盒
        /// </summary>
        public bool useCustomBounds;

        /// <summary>
        /// 自定义包围盒
        /// </summary>
        public Bounds customBounds;

        /// <summary>
        /// 每帧更新包围盒(静态物体，建议默认关闭。动态物体，建议默认开启)
        /// </summary>
        public bool updateBounds = true; // 默认开启更新包围盒

        /// <summary>
        /// 使用独立的光照信息
        /// </summary>
        public bool useCustomLightData = false;

        private GPerObjectShadowTargetData data = new GPerObjectShadowTargetData();

        private void OnEnable()
        {
            data.go = gameObject;
            data.useCustomBounds = useCustomBounds;
            data.customBounds = customBounds;
            data.updateBounds = updateBounds;
            data.useCustomLightData = useCustomLightData;

            data.Enable();
            GPerObjectShadowManager.Instance.Add(data);

        }

        private void OnDisable()
        {
            data.Disable();
            GPerObjectShadowManager.Instance.Remove(data);
        }
    }
}