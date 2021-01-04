using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
