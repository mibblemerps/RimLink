using System;
using UnityEngine;

namespace PlayerTrade
{
    public static class MathExtensions
    {
        public static Rect OffsetMin(this Rect rect, float x, float y)
        {
            var newRect = new Rect(rect);
            newRect.xMin += x;
            newRect.yMin += y;
            return newRect;
        }

        public static Color ToColor(this float[] floatArray)
        {
            if (floatArray.Length < 3)
                throw new ArgumentException("Given float array isn't a valid color", nameof(floatArray));
            return new Color(floatArray[0], floatArray[1], floatArray[2]);
        }

        public static float[] ToFloats(this Color color)
        {
            return new[] {color.r, color.g, color.b};
        }
    }
}
