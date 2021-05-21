using UnityEngine;

namespace PlayerTrade.MainTab
{
    public interface ITab
    {
        void Draw(Rect mainRect);

        void Update();
    }
}