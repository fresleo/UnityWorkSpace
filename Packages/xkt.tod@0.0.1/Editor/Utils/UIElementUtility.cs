// Created By: WangYu  Date: 2025-06-28

using UnityEngine;
using UnityEngine.UIElements;

namespace XKT.TOD.Utils
{
    public class UIElementUtility
    {
        public static Label CreateLabel(Color labelColor)
        {
            var newLabel = new Label();
            newLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            newLabel.style.fontSize = 14;
            newLabel.style.color = labelColor;
            newLabel.style.whiteSpace = WhiteSpace.NoWrap;

            return newLabel;
        }
    }
}