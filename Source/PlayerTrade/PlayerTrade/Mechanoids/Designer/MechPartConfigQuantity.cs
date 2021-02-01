using System;
using PlayerTrade.Net;
using UnityEngine;
using Verse;

namespace PlayerTrade.Mechanoids.Designer
{
    public class MechPartConfigQuantity : MechPartConfig
    {
        public int Quantity = 1;

        public override float Price => base.Price * Quantity;

        private string _quantityBuffer = "1";

        public MechPartConfigQuantity()
        {
        }

        public override Rect Draw(Rect rect)
        {
            Rect drawRect = base.Draw(rect);

            Rect qtyRect = new Rect(rect.x + 40f, 0, 40f, 25f).CenteredOnYIn(rect);
            Widgets.TextFieldNumeric(qtyRect, ref Quantity, ref _quantityBuffer, 1);
            if (Quantity < 0 || Quantity > 9999)
                Quantity = 1;

            return drawRect;
        }

        public new void Write(PacketBuffer buffer)
        {
            base.Write(buffer);
            buffer.WriteInt(Quantity);
        }

        public new void Read(PacketBuffer buffer)
        {
            base.Read(buffer);
            Quantity = buffer.ReadInt();
        }
    }
}
