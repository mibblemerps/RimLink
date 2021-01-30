using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace PlayerTrade.Mechanoids.Designer
{
    public class MechPart
    {
        public ThingDef ThingDef;
        public float BasePrice;
        public Type ConfigType;

        public Texture2D Icon => ThingDef.uiIcon;

        public MechPart(ThingDef thingDef, float basePrice, Type configType = null)
        {
            ThingDef = thingDef;
            BasePrice = basePrice;
            ConfigType = configType;

            if (ConfigType == null)
                ConfigType = typeof(MechPartConfigQuantity); // Default to basic quantity config
        }

        public MechPartConfig CreateConfig(MechCluster cluster)
        {
            return (MechPartConfig) Activator.CreateInstance(ConfigType, cluster, this);
        }
    }
}
