using System.Collections.Generic;
using RimLink.Systems.Missions.Packets;
using RimWorld;
using Verse;

namespace RimLink.Systems.Missions
{
    public class LentColonistComp : ThingComp
    {
        public Pawn Pawn => (Pawn) parent;

        public Faction HomeFaction => RimLink.Instance.PlayerFactions.ContainsKey(MissionOffer.From)
            ? RimLink.Instance.PlayerFactions[MissionOffer.From]
            : null;

        public MissionOffer MissionOffer;

        /// <summary>
        /// Is pawn is leaving the map after not being sent home.
        /// </summary>
        public bool Leaving;
        /// <summary>
        /// Is the pawn arrested at the moment?
        /// </summary>
        public bool Arrested;
        /// <summary>
        /// The tick at which the colonist will "escape", that is, return home and pick a random escape def.
        /// </summary>
        public int TryEscapeHomeTick;

        /// <summary>
        /// Is the pawn currently doing joint research? This controls whether their research efforts are sent back to the home faction.
        /// </summary>
        public bool DoingJointResearch;

        /// <summary>
        /// Has the colonist gone back to their home faction?
        /// </summary>
        public bool GoneHome;

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);

            if (MissionOffer == null)
                return;

            if (mode == DestroyMode.KillFinalize)
            {
                MissionOffer.Notify_LentColonistEvent(Pawn, PacketLentColonistUpdate.ColonistEvent.Dead);
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            if (MissionOffer == null)
                return;

            // Is arrested
            if (Pawn.IsPrisoner)
            {
                if (!Arrested)
                {
                    // First we're hearing of colonist being arrested
                    MissionOffer.Notify_LentColonistEvent(Pawn, PacketLentColonistUpdate.ColonistEvent.Imprisoned);
                }

                Arrested = true;
            }
            else
            {
                Arrested = false;
            }
        }

        public override void PostDeSpawn(Map map)
        {
            if (Leaving && Pawn.CarriedBy == null && !Pawn.Dead)
                BeginEscape();

            base.PostDeSpawn(map);
        }

        public void BeginEscape()
        {
            if (TryEscapeHomeTick > 0)
                return; // Already escaping

            // Set a time the colonist will try to "escape" home
            //TryEscapeHomeTick = Find.TickManager.TicksGame + Mathf.RoundToInt(Rand.Range(1f, 3f) * 60000);
            TryEscapeHomeTick = Find.TickManager.TicksGame + 1800;
            RimLink.Instance.Get<MissionSystem>().EscapingLentColonists.Add(Pawn);
            MissionOffer.Notify_LentColonistEvent(Pawn, PacketLentColonistUpdate.ColonistEvent.Escaped);
        }

        public void TryEscape()
        {
            if (TryEscapeHomeTick > 0 && Find.TickManager.TicksGame >= TryEscapeHomeTick)
            {
                TryEscapeHomeTick = 0;

                if (!Pawn.IsPrisoner && !Pawn.Spawned &&
                    RimLink.Instance.PlayerFactions.ContainsKey(MissionOffer.From) && Pawn.Faction == RimLink.Instance.PlayerFactions[MissionOffer.From])
                {
                    // Colonist meets condition to "escape home"
                    Log.Message($"{Pawn} escaped home.");
                    MissionOffer.ReturnColonists(new List<Pawn> { Pawn }, false, true);
                }
                else
                {
                    // Failed to escape
                    Log.Message($"{Pawn} was unable to escape home.");
                }
            }
        }

        public void Notify_FailedToBeReturned(RimWorld.Quest quest)
        {
            Leaving = true;

            if (Pawn.IsPrisoner)
            {
                Pawn.SetFaction(HomeFaction);
                Pawn.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Guest);
            }
            else
            {
                // Leave map
                LeaveQuestPartUtility.MakePawnsLeave(new[] { Pawn }, true, quest);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref MissionOffer, "labor_offer");
            Scribe_Values.Look(ref Leaving, "leaving");
            Scribe_Values.Look(ref Arrested, "arrested");
            Scribe_Values.Look(ref TryEscapeHomeTick, "escape_home_tick");
            Scribe_Values.Look(ref DoingJointResearch, "doing_joint_research");
        }
    }
}
