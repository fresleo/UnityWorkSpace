using UnityEngine;

namespace MaterialInspectorExtensionTool.Editor.PublicExtension
{
    public static class RectExtension
    {
        public enum EAnchorType
        {
            UpperLeft = 0,
            UpperCenter = 1,
            UpperRight = 2,
            MiddleLeft = 3,
            MiddleCenter = 4,
            MiddleRight = 5,
            LowerLeft = 6,
            LowerCenter = 7,
            LowerRight = 8
        }

        public enum ESplitType
        {
            Vertical,
            Horizontal
        }

        public enum EAutoFillRect
        {
            FirstRect,
            SecondRect
        }

        /// <summary>
        /// 以某个点为中心缩放 rect 块
        /// </summary>
        /// <param name="self"></param>
        /// <param name="pixel">要缩放的像素</param>
        public static Rect Zoom(this Rect self, 
            float pixel, EAnchorType anchorType)
        {
            Rect r = self;
            switch (anchorType)
            {
                case EAnchorType.MiddleCenter:
                {
                    r = self.CutLeft((-pixel) * 0.5f)
                        .CutRigth((-pixel) * 0.5f)
                        .CutTop((-pixel) * 0.5f)
                        .CutBottom((-pixel) * 0.5f);
                }
                    break;
            }

            return r;
        }

        /// <summary>
        /// 求两个 rect 之间的区域
        /// </summary>
        /// <param name="self">rect 数组，最多两个 rect</param>
        /// <returns>两个 rect 中间的 rect</returns>
        public static Rect GetMidTowRect(this Rect[] self, 
            ESplitType splitType)
        {
            if (splitType == ESplitType.Vertical)
            {
                return new Rect(self[0].xMin, self[0].yMax, self[0].width, self[1].yMin - self[0].yMax);
            }
            else
            {
                return new Rect(self[0].xMax, self[0].yMin, self[1].xMin - self[0].xMax, self[0].height);
            }
        }

        /// <summary>
        /// 总分割
        /// </summary>
        /// <param name="self"></param>
        /// <param name="splitType">分割方式</param>
        /// <param name="size">以这个尺寸分割</param>
        /// <param name="padding">两个块之间的间隙</param>
        /// <param name="justMid">居中</param>
        /// <returns>两个 rect 块</returns>
        public static Rect[] Split(this Rect self, 
            ESplitType splitType, float size, 
            float padding = 0, bool justMid = true, EAutoFillRect autoFillRect = EAutoFillRect.FirstRect)
        {
            if (splitType == ESplitType.Vertical)
            {
                return VerticalSplit(self, size, padding, justMid, autoFillRect);
            }
            else
            {
                return HorizontalSplit(self, size, padding, justMid);
            }
        }

        public static Rect[] Split(this Rect self, 
            int count, ESplitType splitType, 
            float padding = 0, bool justMid = true)
        {
            Rect[] newRects = new Rect[count];

            if (splitType == ESplitType.Vertical)
            {
                var rect = new Rect(0, 0, self.width, self.height / count);
                var newHeight = self.height / count;
                for (int i = 0; i < count; i++)
                {
                    newRects[i] = rect;
                    rect.y += newHeight;
                }
            }
            else
            {
                var rect = new Rect(self.x, self.y, self.width / count, self.height);
                var newWidth = self.width / count;
                for (int i = 0; i < count; i++)
                {
                    newRects[i] = rect;
                    rect.x += newWidth;
                }
            }

            return newRects;
        }

        /// <summary>
        /// 左右分割
        /// </summary>
        /// <param name="self"></param>
        /// <param name="size">以这个尺寸分割</param>
        /// <param name="padding">两个块之间的间隙</param>
        /// <param name="justMid">是否居中</param>
        /// <returns>两个 rect 块</returns>
        public static Rect[] HorizontalSplit(this Rect self, 
            float size, 
            float padding = 0, bool justMid = true)
        {
            if (justMid)
            {
                return new Rect[2]
                {
                    self.CutRigth(self.width - size + padding * 0.5f),
                    self.CutLeft(size + padding * 0.5f),
                };
            }

            return new Rect[2]
            {
                new Rect(),
                new Rect()
            };
        }

        /// <summary>
        /// 上下分割
        /// </summary>
        /// <param name="self"></param>
        /// <param name="size">以这个尺寸分割</param>
        /// <param name="padding">两个块之间的间隙</param>
        /// <param name="justMid">是否居中</param>
        /// <returns>两个 rect 块</returns>
        public static Rect[] VerticalSplit(this Rect self, 
            float size, 
            float padding = 0, bool justMid = true, EAutoFillRect autoFillRect = EAutoFillRect.FirstRect)
        {
            if (justMid)
            {
                if (autoFillRect == EAutoFillRect.SecondRect)
                {
                    return new Rect[2]
                    {
                        self.CutBottom(size + padding * 0.5f),
                        self.CutTop(self.height - size + padding * 0.5f),
                    };
                }
                else
                {
                    return new Rect[2]
                    {
                        self.CutBottom(self.height - size + padding * 0.5f),
                        self.CutTop(size + padding * 0.5f),
                    };
                }
            }

            return new Rect[2]
            {
                new Rect(),
                new Rect()
            };
        }

        // 裁切某个部分 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        public static Rect CutRigth(this Rect self, float pixels)
        {
            self.xMax -= pixels;
            return self;
        }

        public static Rect CutLeft(this Rect self, float pixels)
        {
            self.xMin += pixels;
            return self;
        }

        public static Rect CutTop(this Rect self, float pixels)
        {
            self.yMin += pixels;
            return self;
        }

        public static Rect CutBottom(this Rect self, float pixels)
        {
            self.yMax -= pixels;
            return self;
        }
    }
}