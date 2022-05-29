using System.Collections.Generic;
using System.Linq;
using RimLink.Core;
using RimLink.Systems.Trade;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RimLink.Systems.World
{
    [StaticConstructorOnStartup]
    public class PlayerColonyWorldObject : MapParent
    {
        private static readonly Texture2D TradeCommandTexture = ContentFinder<Texture2D>.Get("UI/Commands/Trade");
        
        public string Name;
        public Player Player;

        public override string Label => Name;
        public override bool HasName => true;
        public override Texture2D ExpandingIcon => Faction.def.FactionIcon;
        
        public override Material Material
        {
            get
            {
                if (_cachedMat == null)
                    _cachedMat = MaterialPool.MatFrom(Faction.def.settlementTexturePath, ShaderDatabase.WorldOverlayTransparentLit, Faction.Color, WorldMaterials.WorldObjectRenderQueue);
                return _cachedMat;
            }
        }

        private Material _cachedMat;

        public override string GetDescription() => "Rl_PlayerColonyDescription".Translate(Name);

        public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan)
        {
            // Trade
            Gizmo trade = new Command_Action
            {
                icon = TradeCommandTexture,
                defaultLabel = "CommandTrade".Translate(),
                defaultDesc = "CommandTradeDesc".Translate(),
                action = () =>
                {
                    _ = TradeUtil.InitiateTrade(caravan.PawnsListForReading.FirstOrDefault(), Player);
                }
            };

            // Ensure we have a pawn with social skill
            Pawn pawn = BestCaravanPawnUtility.FindPawnWithBestStat(caravan, StatDefOf.TradePriceImprovement);
            if (pawn == null)
                trade.Disable("CommandTradeFailNoNegotiator".Translate());

            yield return trade;

            foreach (Gizmo gizmo in base.GetCaravanGizmos(caravan))
                yield return gizmo;
        }
        
        public override void ExposeData()
        {
            Scribe_Values.Look(ref Name, "name");
            Scribe_References.Look(ref Player, "player");
            base.ExposeData();
        }
    }
}