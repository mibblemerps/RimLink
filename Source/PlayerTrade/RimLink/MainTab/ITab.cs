using UnityEngine;

namespace RimLink.MainTab
{
    public interface ITab
    {
        void Draw(Rect mainRect);

        void Update();
    }
}