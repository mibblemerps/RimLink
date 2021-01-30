using UnityEngine;
using Verse;

namespace PlayerTrade.Mechanoids.Designer
{
    public class MechPartConfigQuantity : MechPartConfig
    {
        public int Quantity = 1;

        public override float Price => base.Price * Quantity;

        private string _quantityBuffer = "1";

        public MechPartConfigQuantity(MechCluster mechCluster, MechPart mechPart) : base(mechCluster, mechPart) {}

        public override Rect Draw(Rect rect)
        {
            Rect drawRect = base.Draw(rect);

            Rect qtyRect = new Rect(rect.x + 40f, 0, 40f, 25f).CenteredOnYIn(rect);
            Widgets.TextFieldNumeric(qtyRect, ref Quantity, ref _quantityBuffer, 1);
            if (Quantity < 0 || Quantity > 9999)
                Quantity = 1;

            return drawRect;
        }
    }
}
