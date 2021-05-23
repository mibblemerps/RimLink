using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net;
using PlayerTrade.Net.Packets;
using RimWorld;
using UnityEngine;
using Verse;

namespace PlayerTrade.Mechanoids.Designer
{
    [Serializable]
    public class MechPartConfigProximityActivator : MechPartConfigQuantity
    {
        public int Proximity = 13;

        //public override float Price => Mathf.Round(base.Price * Curve.Evaluate(Proximity));

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

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, "<b>Proximity:</b> 13 tiles");
            Text.Anchor = TextAnchor.UpperLeft;
            //Proximity = Mathf.RoundToInt(Widgets.HorizontalSlider(rect, Proximity, 3f, 40f, label: $"Proximity Distance: {Mathf.RoundToInt(Proximity)} tiles"));

            return Rect.zero;
        }

        public override void Configure(Thing thing)
        {
            base.Configure(thing);
            // Thought I could set proximity here, turns out you can't.
            // Maybe one day I'll Harmony patch a way to set it...
        }

        public override void PostWrite(PacketBuffer buffer)
        {
            base.PostWrite(buffer);
            buffer.WriteInt(Proximity);
        }
        
        public override void PostRead(PacketBuffer buffer)
        {
            base.PostRead(buffer);
            Proximity = buffer.ReadInt();
        }
    }
}
