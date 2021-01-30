using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace PlayerTrade.Mechanoids.Designer
{
    public class MechPartConfigCountdownActivator : MechPartConfig
    {
        public float Days = 3;
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

        public MechPartConfigCountdownActivator(MechCluster mechCluster, MechPart mechPart) : base(mechCluster, mechPart) {}

        public override Rect Draw(Rect rect)
        {
            rect = base.Draw(rect);

            Widgets.TextFieldNumericLabeled(rect, "Countdown (days)", ref Days, ref _daysBuffer, 0.1f, 90f);

            return Rect.zero;
        }
    }
}
