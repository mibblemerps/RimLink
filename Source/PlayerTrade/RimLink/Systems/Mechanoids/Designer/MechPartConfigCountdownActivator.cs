using System;
using RimLink.Net;
using RimLink.Net.Packets;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimLink.Systems.Mechanoids.Designer
{
    [Serializable]
    public class MechPartConfigCountdownActivator : MechPartConfig
    {
        public float Days = 4;

        [NonSerialized]
        private string _daysBuffer = "3";

        public override float Price => Mathf.Round(base.Price * Curve.Evaluate(Days)); // Increase price closer to 0 days we get

        private static SimpleCurve Curve = new SimpleCurve(new []
        {
            new CurvePoint(0, 5f),
            new CurvePoint(0.5f, 3f),
            new CurvePoint(1f, 2f),
            new CurvePoint(2f, 1.5f),
            new CurvePoint(3f, 1.2f),
            new CurvePoint(4f, 1f),
            new CurvePoint(float.MaxValue, 1f), 
        });

        public override Rect Draw(Rect rect)
        {
            rect = base.Draw(rect);

            Widgets.TextFieldNumericLabeled(rect, "Countdown (days)", ref Days, ref _daysBuffer, 0.1f, 90f);

            return Rect.zero;
        }

        public override void Configure(Thing thing)
        {
            base.Configure(thing);
            CompSendSignalOnCountdown countdown = thing.TryGetComp<CompSendSignalOnCountdown>();
            if (countdown != null)
                countdown.ticksLeft = Mathf.RoundToInt(Days * 60000);
            else
                Log.Warn("Can't find countdown activator comp");
        }

        public override void PostWrite(PacketBuffer buffer)
        {
            base.PostWrite(buffer);
            buffer.WriteFloat(Days);
        }

        public override void PostRead(PacketBuffer buffer)
        {
            base.PostRead(buffer);
            Days = buffer.ReadFloat();
        }
    }
}
