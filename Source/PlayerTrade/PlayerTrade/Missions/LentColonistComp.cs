using System.Collections.Generic;
using PlayerTrade.Missions.Packets;
using RimWorld;
using UnityEngine;
using Verse;

namespace PlayerTrade.Missions
{
    public class LentColonistComp : ThingComp
    {
        public Pawn Pawn => (Pawn) parent;

        public Faction HomeFaction => RimLinkComp.Instance.PlayerFactions.ContainsKey(MissionOffer.From)
            ? RimLinkComp.Instance.PlayerFactions[MissionOffer.From]
            : null;

        public MissionOffer MissionOffer;

        public bool Leaving;
        public bool Arrested;
        public int TryEscapeHomeTick;

        public bool DoingJointResearch;

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
            if (Leaving && Pawn.CarriedBy == null)
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
            RimLinkComp.Instance.Get<MissionSystem>().EscapingLentColonists.Add(Pawn);
            MissionOffer.Notify_LentColonistEvent(Pawn, PacketLentColonistUpdate.ColonistEvent.Escaped);
        }

        public void TryEscape()
        {
            if (TryEscapeHomeTick > 0 && Find.TickManager.TicksGame >= TryEscapeHomeTick)
            {
                TryEscapeHomeTick = 0;

                if (!Pawn.IsPrisoner && !Pawn.Spawned &&
                    RimLinkComp.Instance.PlayerFactions.ContainsKey(MissionOffer.From) && Pawn.Faction == RimLinkComp.Instance.PlayerFactions[MissionOffer.From])
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
                Pawn.guest.SetGuestStatus(Faction.OfPlayer, true);
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
