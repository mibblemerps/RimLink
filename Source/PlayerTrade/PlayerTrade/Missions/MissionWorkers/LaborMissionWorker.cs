using System.Collections.Generic;
using PlayerTrade.Net.Packets;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace PlayerTrade.Missions.MissionWorkers
{
    public class LaborMissionWorker : MissionWorker
    {
        public int Payment;
        public int Bond;

        public int TotalAmountPayable => Payment + Bond;

        public override bool CanFulfillAsReceiver()
        {
            return base.CanFulfillAsReceiver() && TradeUtility.ColonyHasEnoughSilver(Find.CurrentMap, TotalAmountPayable);
        }

        public override void OfferReceived()
        {
            Letter letter = new ChoiceLetter_LaborOffer(Offer);
            letter.ID = Find.UniqueIDsManager.GetNextLetterID();
            Find.LetterStack.ReceiveLetter(letter);
        }

        public override void FulfillAsSender(Map map)
        {
            base.FulfillAsSender(map);

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

        public override void FulfillAsReceiver(Map map)
        {
            base.FulfillAsReceiver(map);

            Log.Verbose("Fulfilling as receiver: taking payment $" + TotalAmountPayable);
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
        }

        public override void ReturnedColonistsReceived(List<Pawn> pawns, bool moreLeft, bool mainGroup, bool escaped)
        {
            base.ReturnedColonistsReceived(pawns, moreLeft, mainGroup, escaped);

            if (mainGroup && moreLeft)
            {
                // Not everyone returned
                if (Bond > 0)
                {
                    // Sender gets bond payment since not all colonists came back in the main group
                    var bondThing = ThingMaker.MakeThing(ThingDefOf.Silver);
                    bondThing.stackCount = Bond;
                    IntVec3 cell = DropCellFinder.TradeDropSpot(Find.CurrentMap);
                    TradeUtility.SpawnDropPod(cell, Find.CurrentMap, bondThing);

                    Find.LetterStack.ReceiveLetter("Bond Paid",
                        $"You have been paid the ${Bond.ToString().Colorize(ColoredText.CurrencyColor)} bond because {Offer.For.GuidToName()} failed to return your colonists.",
                        LetterDefOf.PositiveEvent, new LookTargets(new GlobalTargetInfo(cell, Find.CurrentMap)));
                }
            }
        }

        public override void SetSlateVars(Slate slate)
        {
            base.SetSlateVars(slate);

            List<Thing> bondThings = null;
            if (Bond > 0)
            {
                bondThings = new List<Thing>();
                var bondThing = ThingMaker.MakeThing(ThingDefOf.Silver);
                bondThing.stackCount = Bond;
                bondThings.Add(bondThing);
            }

            slate.Set("bond", Bond);
            slate.Set("bond_things", bondThings);
        }

        public override void Read(PacketBuffer buffer)
        {
            base.Read(buffer);
            Payment = buffer.ReadInt();
            Bond = buffer.ReadInt();
        }

        public override void Write(PacketBuffer buffer)
        {
            base.Write(buffer);
            buffer.WriteInt(Payment);
            buffer.WriteInt(Bond);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref Payment, "payment");
            Scribe_Values.Look(ref Bond, "bond");
        }
    }
}
