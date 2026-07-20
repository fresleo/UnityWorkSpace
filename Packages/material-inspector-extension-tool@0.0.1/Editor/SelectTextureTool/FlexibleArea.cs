using MaterialInspectorExtensionTool.Editor.PublicExtension;
using UnityEngine;

namespace MaterialInspectorExtensionTool.Editor.SelectTextureTool
{
    public class FlexibleArea : GUIBase
    {
        public override Rect Rect
        {
            get => new Rect(0, 0, 0, 0);
            set => base.Rect = value;
        }

        protected override void OnDispose()
        {
        }
    }
}