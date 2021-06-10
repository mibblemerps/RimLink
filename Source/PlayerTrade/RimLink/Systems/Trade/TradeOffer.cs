using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimLink.Anticheat;
using RimLink.Core;
using RimLink.Net;
using RimLink.Systems.Trade.Packets;
using RimLink.Util;
using RimWorld;
using UnityEngine;
using Verse;
using Resources = RimLink.Core.Resources;

namespace RimLink.Systems.Trade
{
    public class TradeOffer : IExposable
    {
        public Guid Guid;
        public string From;
        public string For;

        public List<TradeThing> Things = new List<TradeThing>();

        /// <summary>
        /// Is a fresh trade offer? This will lose it's value when saved/loaded from disk, allowing trade offers to be invalidated when that happens.
        /// </summary>
        public bool Fresh;

        public TaskCompletionSource<bool> TradeAccepted = new TaskCompletionSource<bool>();

        public bool IsForUs => For == RimLink.Find().Client.Guid;

        public float OfferedMarketValue
        {
            get
            {
                if (_offeredMarketValueCached < 0f)
                    _offeredMarketValueCached = CalculateMarketValue(true);
                return _offeredMarketValueCached;
            }
        }

        public float RequestedMarketValue
        {
            get
            {
                if (_requestedMarketValueCached < 0f)
                    _requestedMarketValueCached = CalculateMarketValue(false);
                return _requestedMarketValueCached;
            }
        }

        private float _offeredMarketValueCached = -1;
        private float _requestedMarketValueCached = -1;

        public string GetTradeOfferString(out List<ThingDef> hyperlinks)
        {
            hyperlinks = new List<ThingDef>();

            var builder = new StringBuilder();
            builder.AppendLine($"{RimLink.Find().Client.GetName(From)} has presented a trade offer. They are offering...");

            int offerCount = 0;
            foreach (TradeThing thing in Things)
            {
                if (thing.CountOffered <= 0 || thing.OfferedThings.Count == 0)
                    continue;
                builder.AppendLine($"      {thing.CountOffered}x {thing.OfferedThings.First().LabelCapNoCount}");
                offerCount++;

                hyperlinks.Add(thing.OfferedThings.First().def);
            }
            if (offerCount == 0)
                builder.AppendLine("      (nothing)");

            builder.AppendLine("In exchange for...");

            int requestCount = 0;
            foreach (TradeThing thing in Things)
            {
                if (thing.CountOffered >= 0 || thing.RequestedThings.Count == 0)
                    continue;
                builder.AppendLine($"      {-thing.CountOffered}x {thing.RequestedThings.First().LabelCapNoCount}");
                requestCount++;

                hyperlinks.Add(thing.RequestedThings.First().def);
            }
            if (requestCount == 0)
                builder.AppendLine("      (nothing)");

            return builder.ToString();
        }

        public void Accept()
        {
            if (!IsForUs)
            {
                Log.Error($"Attempt to accept trade offer that isn't for us. (For = {For})");
                return;
            }

            Client client = RimLink.Instance.Client;

            Fresh = false; // Make trade no longer "fresh" (acceptable)

            // Send accept packet
            client.SendPacket(new PacketAcceptTrade
            {
                Trade = Guid,
                Accept = true
            });
        }

        public void Reject()
        {
            Fresh = false; // Make trade no longer "fresh" (acceptable)

            RimLink.Instance.Client.SendPacket(new PacketAcceptTrade
            {
                Trade = Guid,
                Accept = false
            });
        }

        /// <summary>
        /// Remove things being offered and give items being received. (Or reverse if we're the receiver)
        /// </summary>
        /// <param name="asReceiver">Are we fulfilling from the perspective of the receiver?</param>
        public void Fulfill(bool asReceiver)
        {
            Log.Message("Fulfill trade. asReceiver = " + asReceiver);
            
            var toGive = new List<Thing>();

            // Give/receive things
            foreach (TradeThing trade in Things)
            {
                try
                {
                    // Give offered things
                    toGive.AddRange(GetQuantityFromThings(asReceiver ? trade.OfferedThings : trade.RequestedThings,
                        asReceiver ? trade.CountOffered : -trade.CountOffered, true));
                }
                catch (Exception e)
                {
                    Log.Error(e.Message);
                }
                
                // Take requested things
                int countToTake = asReceiver ? -trade.CountOffered : trade.CountOffered;
                if (countToTake > 0)
                {
                    if (trade.IsPawn)
                    {
                        if (trade.AllThings.FirstOrDefault() is Pawn pawn)
                        {
                            // Find original pawn
                            Pawn originalPawn = Core.Resources.FindSellablePawn(pawn.TryGetComp<PawnGuidComp>().Guid);
                            if (originalPawn != null)
                            {
                                // Remove pawn
                                originalPawn.Destroy();
                                Log.Message($"{originalPawn.LabelCap} has been sent to fulfill trade.");
                            }
                            else
                            {
                                Log.Warn("Unable to find original version of pawn!");
                            }

                        }
                        else
                        {
                            Log.Warn("Unable to find enough things to launch for trade. Missing pawn!");
                        }
                    }
                    else
                    {
                        int taken = 0;
                        foreach (Thing thing in (asReceiver ? trade.RequestedThings : trade.OfferedThings))
                        {
                            taken += LaunchUtil.LaunchThing(Find.CurrentMap, thing, Mathf.Min(thing.stackCount, countToTake - taken));
                            if (taken >= countToTake)
                                break; // taken enough
                        }

                        if (taken < countToTake)
                            Log.Warn($"Unable to find enough things to launch for trade. Player unable to fully fulfill their side of trade. ({taken}/{countToTake} launched)");

                    }
                }
                
                // Perform anticheat autosave to prevent reverting the trade.
                AnticheatUtil.AnticheatAutosave();
            }

            var dropPodLocations = new List<IntVec3>();
            foreach (Thing thing in toGive)
            {
                //Log.Message($"Give thing {thing.Label}");
                IntVec3 pos = DropCellFinder.TradeDropSpot(Find.CurrentMap);
                dropPodLocations.Add(pos);
                TradeUtility.SpawnDropPod(pos, Find.CurrentMap, thing);
            }

            if (dropPodLocations.Count == 0)
            {
                Find.LetterStack.ReceiveLetter($"Trade Success ({RimLink.Find().Client.GetName(IsForUs ? From : For)})", "Trade accepted.", LetterDefOf.PositiveEvent);
            }
            else
            {
                var averagePos = new IntVec3(0, 0, 0);
                foreach (IntVec3 pos in dropPodLocations)
                    averagePos += pos;
                averagePos = new IntVec3(averagePos.x / dropPodLocations.Count, averagePos.y / dropPodLocations.Count, averagePos.z / dropPodLocations.Count);
                Find.LetterStack.ReceiveLetter($"Trade Success ({RimLink.Find().Client.GetName(IsForUs ? From : For)})", "Trade accepted. Your items will arrive in pods momentarily.", LetterDefOf.PositiveEvent, new TargetInfo(averagePos, Find.CurrentMap));
            }
        }

        public bool CanFulfill(bool asReceiver)
        {
            foreach (TradeThing trade in Things)
            {
                if (trade.IsPawn)
                {
                    foreach (var thing in trade.AllThings)
                    {
                        if (!(thing is Pawn pawn))
                            continue;
                        if (pawn.health.Dead)
                            return false; // Pawn died
                    }

                    continue;
                }

                // Take requested things
                int countToTake = asReceiver ? -trade.CountOffered : trade.CountOffered;
                if (countToTake > 0)
                {
                    int taken = 0;
                    foreach (Thing thing in (asReceiver ? trade.RequestedThings : trade.OfferedThings))
                    {
                        taken += LaunchUtil.LaunchThing(Find.CurrentMap, thing, Mathf.Min(thing.stackCount, countToTake - taken), true); // dry run
                        if (taken >= countToTake)
                            break; // taken enough
                    }

                    if (taken < countToTake)
                        return false;
                }
            }

            return true;
        }

        private float CalculateMarketValue(bool offered)
        {
            float value = 0f;
            foreach (TradeThing trade in Things)
            {
                // If we want requested market value, negate the count offered
                int count = offered ? trade.CountOffered : -trade.CountOffered;

                if (count > 0)
                {
                    value += trade.MarketValue;
                }
            }

            return value;
        }

        private List<Thing> GetQuantityFromThings(List<Thing> things, int count, bool exceptionIfNotEnough)
        {
            var result = new List<Thing>();

            int given = 0;
            while (given < count)
            {
                // Get first thing that isn't empty and isn't already given
                Thing thing = things.FirstOrDefault(t => (t.stackCount > 0 && !result.Contains(t)));
                if (thing == null)
                {
                    if (exceptionIfNotEnough)
                        throw new Exception("Not enough things to get desired quantity");
                    break;
                }

                int countToSplit = Mathf.Min(thing.stackCount, count - given);
                if (countToSplit == thing.stackCount)
                {
                    // Give entire thing
                    result.Add(thing);
                }
                else
                {
                    // Need to split
                    result.Add(thing.SplitOff(countToSplit));
                }

                given += countToSplit;
            }

            return result;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref Guid, "guid");
            Scribe_Values.Look(ref From, "from");
            Scribe_Values.Look(ref For, "for");
            Scribe_Collections.Look(ref Things, "things");
        }

        public class TradeThing : IExposable
        {
            public List<Thing> OfferedThings;
            public List<Thing> RequestedThings;

            /// <summary>
            /// Amount requested. If negative, then amount offered.
            /// </summary>
            public int CountOffered;

            private float _cachedMarketValue = -1f;

            public float MarketValue
            {
                get
                {
                    if (_cachedMarketValue < 0f)
                        _cachedMarketValue = CalculateMarketValue();
                    return _cachedMarketValue;
                }
            }

            public bool IsPawn
            {
                get
                {
                    var offered = OfferedThings.FirstOrDefault();
                    if (offered is Pawn)
                        return true;
                    var requested = RequestedThings.FirstOrDefault();
                    return requested is Pawn;
                }
            }

            public TradeThing() {}

            public TradeThing(List<Thing> offeredThings, List<Thing> requestedThings, int countOffered)
            {
                OfferedThings = offeredThings;
                RequestedThings = requestedThings;
                CountOffered = countOffered;
            }

            public IEnumerable<Thing> AllThings
            {
                get
                {
                    foreach (var thing in OfferedThings)
                        yield return thing;
                    foreach (var thing in RequestedThings)
                        yield return thing;
                }
            }

            public void ExposeData()
            {
                Scribe_Collections.Look(ref OfferedThings, "offered_things", LookMode.Deep);
                Scribe_Collections.Look(ref RequestedThings, "requested_things", LookMode.Deep);
                Scribe_Values.Look(ref CountOffered, "count");
            }

            private float CalculateMarketValue()
            {
                if (CountOffered == 0)
                    return 0;

                Thing thing = AllThings.FirstOrDefault();
                if (thing == null)
                    return 0;

                return thing.MarketValue * Mathf.Abs(CountOffered);
            }
        }
    }
}
