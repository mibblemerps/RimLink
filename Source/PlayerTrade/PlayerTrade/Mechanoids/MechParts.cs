using System.Collections.Generic;
using PlayerTrade.Mechanoids.Designer;

namespace PlayerTrade.Mechanoids
{
    public static class MechParts
    {
        public static List<MechPart> Parts = new List<MechPart>
        {
            // Buildings
            new MechPart(MechPart.PartType.Building, "ActivatorCountdown", false, 200, typeof(MechPartConfigCountdownActivator)),
            new MechPart(MechPart.PartType.Building, "ActivatorProximity", false, 150, typeof(MechPartConfigProximityActivator)),
            new MechPart(MechPart.PartType.Building, "MechAssembler", true),
            new MechPart(MechPart.PartType.Building, "MechCapsule", true),
            new MechPart(MechPart.PartType.Building, "MechDropBeacon", true),
            new MechPart(MechPart.PartType.Building, "Turret_AutoChargeBlaster", true),
            new MechPart(MechPart.PartType.Building, "Turret_AutoInferno", true),
            new MechPart(MechPart.PartType.Building, "Turret_AutoMortar", true),
            new MechPart(MechPart.PartType.Building, "Turret_AutoMiniTurret", true),
            new MechPart(MechPart.PartType.Building, "ShieldGeneratorMortar", false, 400),
            new MechPart(MechPart.PartType.Building, "ShieldGeneratorBullets", false, 400),
            new MechPart(MechPart.PartType.Building, "UnstablePowerCell", false, 400),
            new MechPart(MechPart.PartType.Building, "Gloomlight", false, 150),
            
            // Problem causers
            new MechPart(MechPart.PartType.Building, "EMIDynamo", true, 800),
            new MechPart(MechPart.PartType.Building, "ToxicSpewer", true, 700),
            new MechPart(MechPart.PartType.Building, "PsychicDroner", true, 700),
            new MechPart(MechPart.PartType.Building, "PsychicSuppressor", true, 700),
            new MechPart(MechPart.PartType.Building, "Defoliator", true, 700),
            new MechPart(MechPart.PartType.Building, "SunBlocker", true, 600),
            new MechPart(MechPart.PartType.Building, "SmokeSpewer", true, 600),
            new MechPart(MechPart.PartType.Building, "WeatherController", true, 600),
            new MechPart(MechPart.PartType.Building, "ClimateAdjuster", true, 600),

            // Creatures
            new MechPart(MechPart.PartType.Pawn, "Mech_Centipede", false),
            new MechPart(MechPart.PartType.Pawn, "Mech_Lancer", false),
            new MechPart(MechPart.PartType.Pawn, "Mech_Scyther", false),
            new MechPart(MechPart.PartType.Pawn, "Mech_Pikeman", false),
        };
    }
}