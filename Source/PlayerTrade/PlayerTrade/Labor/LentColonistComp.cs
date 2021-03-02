using System.Collections.Generic;
using PlayerTrade.Labor.Packets;
using RimWorld;
using UnityEngine;
using Verse;

namespace PlayerTrade.Labor
{
    public class LentColonistComp : ThingComp
    {
        public Pawn Pawn => (Pawn) parent;

        public Faction HomeFaction => RimLinkComp.Instance.PlayerFactions.ContainsKey(LaborOffer.From)
            ? RimLinkComp.Instance.PlayerFactions[LaborOffer.From]
            : null;

        public LaborOffer LaborOffer;

        public bool Leaving;
        public bool Arrested;
        public int TryEscapeHomeTick;

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);

            if (LaborOffer == null)
                return;

            if (mode == DestroyMode.KillFinalize)
            {
                LaborOffer.Notify_LentColonistEvent(Pawn, PacketLentColonistUpdate.ColonistEvent.Dead);
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            if (LaborOffer == null)
                return;

            // Is arrested
            if (Pawn.IsPrisoner)
            {
                if (!Arrested)
                {
                    // First we're hearing of colonist being arrested
                    LaborOffer.Notify_LentColonistEvent(Pawn, PacketLentColonistUpdate.ColonistEvent.Imprisoned);
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
            if (Leaving)
                BeginEscape();

            base.PostDeSpawn(map);
        }

        public void BeginEscape()
        {
            if (TryEscapeHomeTick > 0)
                return; // Already escaping

            // Set a time the colonist will try to "escape" home
            TryEscapeHomeTick = Find.TickManager.TicksGame + 1800;
            RimLinkComp.Instance.EscapingLentColonists.Add(Pawn);
            LaborOffer.Notify_LentColonistEvent(Pawn, PacketLentColonistUpdate.ColonistEvent.Escaped);
        }

        public void TryEscape()
        {
            if (TryEscapeHomeTick > 0 && Find.TickManager.TicksGame >= TryEscapeHomeTick)
            {
                TryEscapeHomeTick = 0;

                if (!Pawn.IsPrisoner && !Pawn.Spawned &&
                    RimLinkComp.Instance.PlayerFactions.ContainsKey(LaborOffer.From) && Pawn.Faction == RimLinkComp.Instance.PlayerFactions[LaborOffer.From])
                {
                    // Colonist meets condition to "escape home"
                    Log.Message($"{Pawn} escaped home.");
                    LaborOffer.ReturnColonists(new List<Pawn> { Pawn }, true);
                }
                else
                {
                    // Failed to escape
                    Log.Message($"{Pawn} was unable to escape home.");
                }
            }
        }

        public void Notify_FailedToBeReturned(Quest quest)
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
            Scribe_References.Look(ref LaborOffer, "labor_offer");
            Scribe_Values.Look(ref Leaving, "leaving");
            Scribe_Values.Look(ref Arrested, "arrested");
            Scribe_Values.Look(ref TryEscapeHomeTick, "escape_home_tick");
        }
    }
}
