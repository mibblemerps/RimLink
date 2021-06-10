using System;
using UnityEngine;
using Verse;

namespace RimLink.Systems.Mechanoids.Designer
{
    public class MechPart
    {
        private const float CombatPowerPriceMultiplier = 3f;
        
        public PartType Type;
        public string DefName;
        public float BasePrice;
        public Type ConfigType;

        public ThingDef ThingDef
        {
            get
            {
                if (Type != PartType.Building) throw new Exception("Attempt to get ThingDef on non Building type mech part");
                if (_cachedThingDef == null)
                    _cachedThingDef = DefDatabase<ThingDef>.GetNamed(DefName);
                return _cachedThingDef;
            }
        }

        public PawnKindDef PawnKindDef
        {
            get
            {
                if (Type != PartType.Pawn) throw new Exception("Attempt to get PawnKindDef on non Pawn type mech part");
                if (_cachedPawnKindDef == null)
                    _cachedPawnKindDef = DefDatabase<PawnKindDef>.GetNamed(DefName);
                return _cachedPawnKindDef;
            }
        }

        public bool IsBuildingThreat;

        public Texture2D Icon
        {
            get
            {
                switch (Type)
                {
                    case PartType.Building:
                        return ThingDef.uiIcon;
                    case PartType.Pawn:
                        return PawnKindDef.race.uiIcon;
                }
                return null;
            }
        }
        
        private ThingDef _cachedThingDef;
        private PawnKindDef _cachedPawnKindDef;

        public MechPart(PartType type, string defName, bool isBuildingThreat, float? basePrice = null, Type configType = null)
        {
            Type = type;
            DefName = defName;
            IsBuildingThreat = isBuildingThreat;
            ConfigType = configType;
            
            BasePrice = basePrice ?? CalculateCombatPowerPrice();

            if (ConfigType == null)
                ConfigType = typeof(MechPartConfigQuantity); // Default to basic quantity config
        }

        public MechPartConfig CreateConfig()
        {
            var part = (MechPartConfig) Activator.CreateInstance(ConfigType);
            part.MechPart = this;
            return part;
        }

        private float CalculateCombatPowerPrice()
        {
            if (RimLinkMod.Instance == null) // don't run on server
                return 0;
            
            float combatPower = Type == PartType.Pawn ? PawnKindDef.combatPower : ThingDef.building.combatPower;
            return combatPower * CombatPowerPriceMultiplier;
        }

        public enum PartType
        {
            Building,
            Pawn
        }
    }
}
