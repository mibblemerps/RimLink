using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net;
using RimWorld;
using UnityEngine;
using Verse;

namespace PlayerTrade.Raids
{
    [Serializable]
    public class BountyRaid : IExposable
    {
        public string Id;
        public string From;
        public string FactionName;
        public string ArrivalModeDefName;
        public float Strength;
        public string Strategy;
        public int ArrivesInTicks;

        public PawnsArrivalModeDef ArrivalMode
        {
            get => DefDatabase<PawnsArrivalModeDef>.GetNamed(ArrivalModeDefName);
            set => ArrivalModeDefName = value.defName;
        }

        public Faction Faction
        {
            get
            {
                foreach (Faction faction in Find.FactionManager.AllFactions)
                {
                    if (string.Equals(FactionName, faction.Name, StringComparison.InvariantCultureIgnoreCase))
                        return faction;
                }
                return null;
            }
        }

        public async Task<bool> Send(string target)
        {
            RimLinkComp.Instance.Client.SendPacket(new PacketTriggerRaid
            {
                For = target,
                Raid = this
            });

            return true;
        }

        public void Execute()
        {
            Log.Message($"Execute raid {Id}");

            Map map = Find.RandomPlayerHomeMap;

            float points = StorytellerUtility.DefaultThreatPointsNow(map) * Strength;

            if (Faction == null)
                Log.Error("Bounty raid faction is null");

            var incidentParams = new IncidentParms
            {
                forced = true,
                target = map,
                faction = Faction,
                points = points,
                pawnGroupMakerSeed = Rand.Int,
                raidArrivalMode = ArrivalMode,
                raidStrategy = DefDatabase<RaidStrategyDef>.GetNamed(Strategy)
            };

            var raid = IncidentDefOf.RaidEnemy.Worker;
            raid.TryExecute(incidentParams);
        }

        public void InformTargetBountyPlaced()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{RimLinkComp.Find().Client.GetName(From).Colorize(ColoredText.FactionColor_Neutral)} has placed a bounty on you with {FactionName}!\n");

            if (Strength > 1.35f)
                sb.AppendLine("Apparently they've paid a significant sum, so the attack will be quite large.");
            else if (Strength < 0.4f)
                sb.AppendLine("The attack is expected to be quite small.");

            if (Strategy == "Siege")
                sb.AppendLine("We believe they intend on besieging the colony.");

            if (ArrivalMode == PawnsArrivalModeDefOf.CenterDrop)
                sb.AppendLine("We believe they intend to land directly on top of us!");
            else if (ArrivalMode == PawnsArrivalModeDefOf.RandomDrop)
                sb.AppendLine("We believe they intend to land scattered across the map!");

            sb.AppendLine();
            if (ArrivesInTicks <= 60)
                sb.AppendLine("They are arriving immediately.");
            else if (ArrivesInTicks < 60000)
                sb.AppendLine($"They will arrive in {Mathf.FloorToInt(ArrivesInTicks / 2500f)} hours");
            else
                sb.AppendLine($"They will arrive in {Mathf.FloorToInt(ArrivesInTicks / 60000f)} days");

            Find.LetterStack.ReceiveLetter($"Bounty Placed ({RimLinkComp.Find().Client.GetName(From)})", sb.ToString(), LetterDefOf.ThreatBig, $"Id = {Id}");
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref Id, "id");
            Scribe_Values.Look(ref From, "from");
            Scribe_Values.Look(ref FactionName, "faction");
            Scribe_Values.Look(ref Strategy, "strategy");
            Scribe_Values.Look(ref ArrivalModeDefName, "arrival_mode");
            Scribe_Values.Look(ref Strength, "strength");
            Scribe_Values.Look(ref ArrivesInTicks, "arrives_in_ticks");
        }
    }
}
