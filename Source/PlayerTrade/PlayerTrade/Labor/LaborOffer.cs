using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Labor.Packets;
using PlayerTrade.Net;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace PlayerTrade.Labor
{
    public class LaborOffer : IExposable
    {
        public const float MarketValueVarianceAllowed = 50f;

        public string Guid;
        public List<Pawn> Colonists;
        public int ColonistCount;
        public float Days;
        public int Payment;
        public int Bond;
        public string From;
        public string For;

        /// <summary>
        /// Is a fresh labor offer? This will lose it's value when saved/loaded from disk, allowing labor offers to be invalidated when that happens.
        /// </summary>
        public bool Fresh;

        /// <summary>
        /// Used by the sender to store the market values of the pawnns at the time the offer was made.
        /// </summary>
        public Dictionary<Pawn, float> MarketValues = new Dictionary<Pawn, float>();

        public int TotalAmountPayable => Payment + Bond;

        public string GenerateOfferText()
        {
            Client client = RimLinkComp.Find().Client;

            var sb = new StringBuilder();

            sb.AppendLine($"{client.GetName(From).Colorize(ColoredText.FactionColor_Neutral)} has made an offer to lend you {(Colonists.Count == 1 ? "a colonist" : $"{Colonists.Count} colonists")} for {Days} days.\n");
            sb.AppendLine($"They are requesting {Payment} silver as payment.");
            if (Bond > 0)
                sb.AppendLine($"\nThey are requiring a bond of {Bond} silver. This will be paid and then returned to you if you return the colonists safely.");

            sb.AppendLine();
            foreach (Pawn pawn in Colonists)
                sb.AppendLine($"    {pawn.NameFullColored}, {pawn.story.TitleCap}");

            sb.AppendLine($"\nAmount payable now: {("$" + TotalAmountPayable).Colorize(ColoredText.CurrencyColor)}");

            return sb.ToString();
        }

        public bool CanFulfill()
        {
            return TradeUtility.ColonyHasEnoughSilver(Find.CurrentMap, TotalAmountPayable);
        }

        public bool LenderCanStillFulfill()
        {
            foreach (Pawn pawn in Colonists)
            {
                float intendedMarketValue = MarketValues[pawn];
                if (Mathf.Abs(pawn.MarketValue - intendedMarketValue) > MarketValueVarianceAllowed)
                    return false;
            }

            return true;
        }

        public void GenerateMarketValues()
        {
            foreach (Pawn pawn in Colonists)
            {
                MarketValues.Add(pawn, pawn.MarketValue);
            }
        }

        public void FulfillAsSender(Map map)
        {
            Log.Message($"Fulfilling labor offer {Guid} as sender (removing pawns receiving payment)");

            // Remove offered colonists from our map
            RimLinkComp.Find().PawnsToRemove.AddRange(Colonists);

            if (Payment > 0)
            {
                // Give payment
                Thing paymentThing = ThingMaker.MakeThing(ThingDefOf.Silver);
                paymentThing.stackCount = Payment;
                TradeUtility.SpawnDropPod(DropCellFinder.TradeDropSpot(map), map, paymentThing);
            }
            else if (Payment < 0)
            {
                // Take payment
                TradeUtility.LaunchSilver(map, -Payment);
            }
        }

        public void FulfillAsReceiver(Map map)
        {
            Log.Message($"Fulfilling labor offer {Guid} as receiver (giving pawns making payment)");

            // Spawn pawns
            foreach (Pawn pawn in Colonists)
                TradeUtility.SpawnDropPod(DropCellFinder.TradeDropSpot(map), map, pawn);

            if (TotalAmountPayable < 0)
            {
                // Give payment
                Thing paymentThing = ThingMaker.MakeThing(ThingDefOf.Silver);
                paymentThing.stackCount = Payment;
                TradeUtility.SpawnDropPod(DropCellFinder.TradeDropSpot(map), map, paymentThing);
            }
            else if (TotalAmountPayable > 0)
            {
                // Take payment
                TradeUtility.LaunchSilver(map, TotalAmountPayable);
            }

            List<Thing> bondThings = null;
            if (Bond > 0)
            {
                bondThings = new List<Thing>();
                var bondThing = ThingMaker.MakeThing(ThingDefOf.Silver);
                bondThing.stackCount = Bond;
                bondThings.Add(bondThing);
            }

            // Create quest
            var slate = new Slate();
            slate.Set("guid", Guid);
            slate.Set("from", RimLinkComp.Find().Client.GetName(From));
            slate.Set("days", Days);
            slate.Set("shuttle_arrival_ticks", Mathf.RoundToInt(Mathf.Max(0, (Days - 0.5f) * 60000f))); // shuttle arrives 12 hours early
            slate.Set("shuttle_leave_ticks", Mathf.RoundToInt(Days * 60000f));
            slate.Set("pawns", Colonists);
            slate.Set("pawn_count", Colonists.Count);
            slate.Set("bond", Bond);
            slate.Set("bond_things", bondThings);
            Quest quest = QuestGen.Generate(DefDatabase<QuestScriptDef>.GetNamed("PlayerLentColonists"), slate);
            Find.QuestManager.Add(quest);
        }

        /// <summary>
        /// Return colonists.
        /// </summary>
        public async Task ReturnColonists(List<Pawn> pawns)
        {
            Client client = RimLinkComp.Instance.Client;

            var netPawns = new List<NetHuman>();
            foreach (Pawn pawn in pawns)
                netPawns.Add(pawn.ToNetHuman());

            var packet = new PacketReturnLentColonists
            {
                For = From,
                Guid = Guid,
                ReturnedColonists = netPawns
            };

            client.SendPacket(packet);

            RimLinkComp.Instance.ActiveLaborOffers.Remove(this);
        }

        /// <summary>
        /// Called when a return colonists packet is received. This facilitates the giving back of the colonists and paying out and bond - if applicable. 
        /// </summary>
        public void ReturnedColonistsReceived(PacketReturnLentColonists packet)
        {
            Client client = RimLinkComp.Find().Client;

            if (From != client.Guid)
            {
                Log.Error($"Attempt to return colonists for a labor offer we didn't send!");
                return;
            }

            foreach (var colonist in packet.ReturnedColonists)
            {
                Pawn pawn = colonist.ToPawn();
                Log.Message($"Returning {pawn.Name.ToStringFull}...");
                IntVec3 pos = DropCellFinder.TradeDropSpot(Find.CurrentMap);
                TradeUtility.SpawnDropPod(pos, Find.CurrentMap, pawn);
            }

            Log.Message($"{packet.ReturnedColonists.Count}/{Colonists.Count} returned from labor deal.");
            if (packet.ReturnedColonists.Count < Colonists.Count)
            {
                if (Bond > 0)
                {
                    // Pay bond
                    var bondThing = ThingMaker.MakeThing(ThingDefOf.Silver);
                    bondThing.stackCount = Bond;
                    TradeUtility.SpawnDropPod(DropCellFinder.TradeDropSpot(Find.CurrentMap), Find.CurrentMap, bondThing);
                }

                Find.LetterStack.ReceiveLetter($"Colonists Lost ({client.GetName(For)})", $"{client.GetName(For).Colorize(ColoredText.FactionColor_Neutral)} didn't return all your colonists.", LetterDefOf.NegativeEvent);
            }
            else
            {
                Find.LetterStack.ReceiveLetter($"Colonists Returned ({client.GetName(For)})", $"{client.GetName(For).Colorize(ColoredText.FactionColor_Neutral)} returned your colonists.", LetterDefOf.PositiveEvent);
            }

            // Remove this as an active labor offer.
            RimLinkComp.Find().ActiveLaborOffers.Remove(this);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref Guid, "guid");
            Scribe_Values.Look(ref From, "from");
            Scribe_Values.Look(ref For, "for");
            Scribe_Values.Look(ref Days, "days");
            Scribe_Values.Look(ref Payment, "payment");
            Scribe_Values.Look(ref Bond, "bond");
            Scribe_Collections.Look(ref Colonists, "colonists", LookMode.Reference);
            //Scribe_Collections.Look(ref MarketValues, "market_values");
        }

        public PacketLaborOffer ToPacket()
        {
            var packet = new PacketLaborOffer
            {
                Guid = Guid,
                For = For,
                From = From,
                Payment = Payment,
                Bond = Bond,
                Days = Days,
                Colonists = new List<NetHuman>()
            };

            foreach (Pawn pawn in Colonists)
                packet.Colonists.Add(pawn.ToNetHuman());

            return packet;
        }

        public static LaborOffer FromPacket(PacketLaborOffer packet)
        {
            var offer = new LaborOffer
            {
                Guid = packet.Guid,
                For = packet.For,
                From = packet.From,
                Bond = packet.Bond,
                Payment = packet.Payment,
                Days = packet.Days,
                Fresh = true,
                Colonists = new List<Pawn>()
            };

            foreach (NetHuman netHuman in packet.Colonists)
                offer.Colonists.Add(netHuman.ToPawn());

            return offer;
        }
    }
}
