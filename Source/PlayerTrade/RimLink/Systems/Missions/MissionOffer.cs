using System;
using System.Collections.Generic;
using System.Linq;
using RimLink.Anticheat;
using RimLink.Core;
using RimLink.Net;
using RimLink.Systems.Missions.Escape;
using RimLink.Systems.Missions.MissionWorkers;
using RimLink.Systems.Missions.Packets;
using RimLink.Util;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimLink.Systems.Missions
{
    public class MissionOffer : IExposable, ILoadReferenceable
    {
        public const float MarketValueVarianceAllowed = 50f;

        /// <summary>Uniquely identifies this labor offer</summary>
        public string Guid;
        /// <summary>List of colonists being offered to lend.</summary>
        public List<Pawn> Colonists;
        public float Days;
        public string From;
        public string For;

        public int FinishTick;

        public PlayerMissionDef MissionDef = DefDatabase<PlayerMissionDef>.GetNamed("Labor");

        public MissionWorker MissionWorker
        {
            get => _missionWorker ?? (_missionWorker = MissionDef?.CreateWorker(this));
            set => _missionWorker = value;
        }

        /// <summary>
        /// Is a fresh labor offer? This will lose it's value when saved/loaded from disk, allowing labor offers to be invalidated when that happens.
        /// </summary>
        public bool Fresh;

        /// <summary>
        /// A list to store any heirs. Tis is saved so the game can maintain a reference to them.
        /// </summary>
        public List<Pawn> Heirs = new List<Pawn>();

        /// <summary>
        /// List of the original colonists sent. This is deep-saved by the sender to keep an original copy of the pawn.
        /// </summary>
        public List<Pawn> OriginalColonists = new List<Pawn>();

        /// <summary>
        /// List of pawns that have been returned.
        /// </summary>
        public List<Pawn> ReturnedColonists = new List<Pawn>();

        public bool Active => FinishTick > 0 && Find.TickManager.TicksGame < FinishTick;

        public bool CanFulfillAsSender => MissionWorker.CanFulfillAsSender();
        public bool CanFulfillAsReceiver => MissionWorker.CanFulfillAsReceiver();

        private MissionWorker _missionWorker;

        public void OfferSend()
        {
            MissionWorker.OfferSend();
        }

        public async void Accept()
        {
            if (!Fresh)
                return;
            Fresh = false;
            RemoveOfferLetter();

            Client client = RimLink.Instance.Client;

            // Send acceptance
            client.SendPacket(new PacketAcceptMissionOffer
            {
                For = From,
                Accept = true,
                Guid = Guid
            });

            // Await confirmation of deal
            Log.Message($"Awaiting confirmation of labor offer {Guid}...");

            PacketConfirmMissionOffer packetConfirm = (PacketConfirmMissionOffer) await client.AwaitPacket(p =>
                {
                    if (p is PacketConfirmMissionOffer pc)
                        return pc.Guid == Guid;
                    return false;
                }, 3000);

            Log.Message($"Labor offer {Guid} confirmed");

            if (packetConfirm.Confirm)
            {
                FulfillAsReceiver(Find.AnyPlayerHomeMap);
            }
            else
            {
                Find.LetterStack.ReceiveLetter("Rl_MissionOfferAborted".Translate(From.GuidToName()),
                    "Rl_MissionOfferAbortedDesc".Translate(), LetterDefOf.NeutralEvent);
            }
        }

        public void Reject()
        {
            if (!Fresh)
                return;
            Fresh = false;
            RemoveOfferLetter();

            // Send rejection
            RimLink.Instance.Client.SendPacket(new PacketAcceptMissionOffer
            {
                For = From,
                Accept = false, // reject
                Guid = Guid
            });
        }

        public void FulfillAsSender(Map map)
        {
            Log.Message($"Fulfilling labor offer {Guid} as sender (removing pawns receiving payment)");

            // Remove offered colonists from our map
            foreach (var colonist in Colonists)
                colonist.DeSpawn();

            // Remember original colonists
            OriginalColonists.Clear();
            OriginalColonists.AddRange(Colonists);

            // Worker handles any payments or whatever
            MissionWorker.FulfillAsSender(map);
            
            // Autosave to prevent reverting mission
            AnticheatUtil.AnticheatAutosave(true);
        }

        public void FulfillAsReceiver(Map map)
        {
            Log.Message($"Fulfilling labor offer {Guid} as receiver (giving pawns making payment)");

            // Setup pawns
            foreach (Pawn pawn in Colonists)
            {
                // Set labor offer in comp
                pawn.TryGetComp<LentColonistComp>().MissionOffer = this;

                // Store heir
                Pawn heir = pawn.royalty?.GetHeir(Faction.OfEmpire);
                if (heir != null)
                    Heirs.Add(heir);
            }

            // Set finish tick. This also marks the offer as "active"
            FinishTick = Mathf.RoundToInt(Find.TickManager.TicksGame + Days * 60000);

            // Worker handles mission specific stuff, and the spawning in of the pawns
            Log.Verbose($"Passing off to mission worker to fulfill as receiver. {MissionWorker.GetType().Name}");
            try
            {
                MissionWorker.FulfillAsReceiver(map);
            }
            catch (Exception e)
            {
                Log.Error("Exception fulfilling mission as receiver.", e);
            }
            
            // Autosave to prevent reverting mission
            AnticheatUtil.AnticheatAutosave(true);
        }

        /// <summary>
        /// Return colonists.
        /// </summary>
        public void ReturnColonists(List<Pawn> pawns, bool mainGroup, bool escaped = false)
        {
            Client client = RimLink.Instance.Client;
            
            MissionWorker.ReturnColonists(pawns, mainGroup, escaped);

            var netPawns = new List<NetHuman>();
            foreach (Pawn pawn in pawns)
            {
                netPawns.Add(pawn.ToNetHuman());

                // Mark pawn as "gone home"
                pawn.TryGetComp<LentColonistComp>().GoneHome = true;
                
                // Remove pawn from colonists list - we no longer can save them since they're gonna be gone
                Colonists.Remove(pawn);
            }

            var packet = new PacketReturnLentColonists
            {
                For = From,
                Guid = Guid,
                ReturnedColonists = netPawns,
                MainGroup = mainGroup,
                Escaped = escaped
            };

            client.SendPacket(packet);
        }

        /// <summary>
        /// Called when a return colonists packet is received. This facilitates the giving back of the colonists and paying out and bond - if applicable. 
        /// </summary>
        public void ReturnedColonistsReceived(PacketReturnLentColonists packet)
        {
            Client client = RimLink.Instance.Client;

            Log.Message($"{packet.ReturnedColonists.Count} returned from labor deal.");

            if (From != client.Guid)
            {
                Log.Error($"Attempt to return colonists for a labor offer we didn't send!");
                return;
            }

            var pawns = new List<Pawn>();
            foreach (var colonist in packet.ReturnedColonists)
            {
                // Find the locally stored original pawn we sent - we use this as the basis for receiving the pawn from the network
                Pawn originalPawn =
                    OriginalColonists.FirstOrDefault(p => ThingCompUtility.TryGetComp<PawnGuidComp>(p).Guid == colonist.RimLinkGuid);
                if (originalPawn == null)
                    Log.Warn($"RimLink pawn GUID not found on returned pawn. The original pawn cannot be found, so the pawn may not be reproduced perfectly.");

                Pawn pawn = colonist.ToPawn(originalPawn);
                pawns.Add(pawn);
                ReturnedColonists.Add(pawn);

                // Remove original pawn from mission original colonists. We no longer need to deep save them since they've been respawned.
                OriginalColonists.Remove(originalPawn);

                // If they escaped, pass off to the esacpe util to do that
                if (packet.Escaped)
                    EscapeUtil.Escaped(pawn);
            }

            if (!packet.Escaped)
            {
                // Drop-off returned colonists
                // todo: could be problematic since this is based on the current map only
                ArrivalUtil.Arrive(Find.CurrentMap, ArrivalUtil.Method.Shuttle, pawns.ToArray());
            }

            bool moreLeft = Colonists.Any(pawn => !ReturnedColonists.Contains(pawn));
            MissionWorker.ReturnedColonistsReceived(pawns, moreLeft, packet.MainGroup, packet.Escaped);
        }

        public void Update()
        {
            MissionWorker.Update();
        }

        public void Notify_LentColonistEvent(Pawn pawn, PacketLentColonistUpdate.ColonistEvent colonistEvent)
        {
            Log.Message($"Lent colonist update. Pawn = {pawn.Name} Event = {colonistEvent}");

            RimLink.Instance.Client.SendPacket(new PacketLentColonistUpdate
            {
                For = From,
                What = colonistEvent,
                PawnGuid = pawn.TryGetComp<PawnGuidComp>().Guid
            });
        }

        /// <summary>
        /// Find any mission offer letters and remove them.
        /// </summary>
        public void RemoveOfferLetter()
        {
            var toRemove = new List<Letter>();
            foreach (Letter letter in Find.LetterStack.LettersListForReading)
            {
                if (letter is ChoiceLetter_MissionOffer missionOffer && missionOffer.MissionOffer == this)
                    toRemove.Add(letter);
            }

            foreach (Letter letter in toRemove)
                Find.LetterStack.RemoveLetter(letter);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref Guid, "guid");
            Scribe_Values.Look(ref FinishTick, "finish_tick");
            Scribe_Values.Look(ref From, "from");
            Scribe_Values.Look(ref For, "for");
            Scribe_Values.Look(ref Days, "days");
            Scribe_Collections.Look(ref OriginalColonists, "original_colonists", LookMode.Deep, Array.Empty<object>());
            Scribe_Collections.Look(ref ReturnedColonists, "returned_colonists", LookMode.Reference, Array.Empty<object>());
            Scribe_Collections.Look(ref Colonists, "colonists", LookMode.Reference, Array.Empty<object>());
            Scribe_Collections.Look(ref Heirs, "heirs", LookMode.Deep, Array.Empty<object>());
            
            Scribe_Defs.Look(ref MissionDef, "mission_def");
            Scribe.EnterNode("mission_worker");
            MissionWorker.ExposeData();
            Scribe.ExitNode();
        }

        public PacketMissionOffer ToPacket()
        {
            var packet = new PacketMissionOffer
            {
                Guid = Guid,
                For = For,
                From = From,
                Days = Days,
                Colonists = new List<NetHuman>(),
                MissionDefName = MissionDef.defName,

                WorkerClassName = MissionWorker.GetType().FullName,
                Worker = MissionWorker,
            };

            foreach (Pawn pawn in Colonists)
                packet.Colonists.Add(pawn.ToNetHuman());

            return packet;
        }

        public static MissionOffer FromPacket(PacketMissionOffer packet)
        {
            var offer = new MissionOffer
            {
                Guid = packet.Guid,
                For = packet.For,
                From = packet.From,
                Days = packet.Days,
                Fresh = true,
                Colonists = new List<Pawn>(),
                MissionDef = DefDatabase<PlayerMissionDef>.GetNamed(packet.MissionDefName)
            };

            offer.MissionWorker = packet.Worker;
            offer.MissionWorker.Offer = offer;

            foreach (NetHuman netHuman in packet.Colonists)
                offer.Colonists.Add(netHuman.ToPawn());

            // Set colonist faction
            Faction homeFaction = RimLink.Instance.PlayerFactions[offer.From];
            if (homeFaction == null)
            {
                Log.Error("Cannot find player faction");
            }

            foreach (Pawn colonist in offer.Colonists)
                colonist.SetFaction(Faction.OfPlayer);

            return offer;
        }

        public string GetUniqueLoadID()
        {
            return "RimLink_Mission_" + Guid;
        }
    }
}
