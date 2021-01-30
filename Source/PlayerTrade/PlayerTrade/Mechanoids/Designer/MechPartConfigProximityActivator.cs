using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace PlayerTrade.Mechanoids.Designer
{
    public class MechPartConfigProximityActivator : MechPartConfig
    {
        public float Proximity = 10;
        private string _proximityBuffer = "10";

        public override float Price => Mathf.Round(base.Price * Curve.Evaluate(Proximity));

        private static SimpleCurve Curve = new SimpleCurve(new []
        {
            new CurvePoint(0, 1f),
            new CurvePoint(10, 1.33f),
            new CurvePoint(20, 1.66f),
            new CurvePoint(30, 2.5f),
            new CurvePoint(45, 6f), 
        });

        public MechPartConfigProximityActivator(MechCluster mechCluster, MechPart mechPart) : base(mechCluster, mechPart) {}

        public override Rect Draw(Rect rect)
        {
            rect = base.Draw(rect);

            Proximity = Widgets.HorizontalSlider(rect, Proximity, 3f, 45f, label: $"Proximity Distance: {Mathf.RoundToInt(Proximity)} tiles");

            return Rect.zero;
        }
    }
}
