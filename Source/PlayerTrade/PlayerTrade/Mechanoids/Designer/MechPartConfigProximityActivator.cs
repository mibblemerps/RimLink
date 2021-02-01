using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net;
using UnityEngine;
using Verse;

namespace PlayerTrade.Mechanoids.Designer
{
    [Serializable]
    public class MechPartConfigProximityActivator : MechPartConfigQuantity
    {
        public int Proximity = 10;

        public override float Price => Mathf.Round(base.Price * Curve.Evaluate(Proximity));

        private static SimpleCurve Curve = new SimpleCurve(new []
        {
            new CurvePoint(0, 1f),
            new CurvePoint(10, 1.33f),
            new CurvePoint(20, 1.66f),
            new CurvePoint(30, 2.5f),
            new CurvePoint(45, 6f), 
        });

        public override Rect Draw(Rect rect)
        {
            rect = base.Draw(rect);

            Proximity = Mathf.RoundToInt(Widgets.HorizontalSlider(rect, Proximity, 3f, 45f, label: $"Proximity Distance: {Mathf.RoundToInt(Proximity)} tiles"));

            return Rect.zero;
        }

        public new void Write(PacketBuffer buffer)
        {
            base.Write(buffer);
            buffer.WriteInt(Proximity);
        }

        public new void Read(PacketBuffer buffer)
        {
            base.Read(buffer);
            Proximity = buffer.ReadInt();
        }
    }
}
