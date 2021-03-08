using HarmonyLib;
using PlayerTrade.Missions;
using PlayerTrade.Missions.MissionWorkers;
using PlayerTrade.Missions.Packets;
using PlayerTrade.Missions.Quest;
using PlayerTrade.Util;
using RimWorld;
using Verse;

namespace PlayerTrade.Patches
{
    /// <summary>
    /// Patch to that applies a research speed modifier based any applicable <see cref="QuestPart_ResearchSpeedModifier"/> quest parts.
    /// </summary>
    [HarmonyPatch(typeof(ResearchManager), "ResearchPerformed")]
    public class Patch_ResearchManager_ResearchPerformed
    {
        public static void Prefix(ref float amount, Pawn researcher)
        {
            if (researcher == null)
                return; // Don't do anything if there's no researcher

            float multiplier = 1f;

            foreach (var part in MissionUtil.GetQuestPart<QuestPart_ResearchSpeedModifier>(researcher,
                (part, pawn) => part.Pawns.Contains(pawn)))
            {
                multiplier *= part.Multiplier;
            }

            // Multiplier
            amount *= multiplier;

            LentColonistComp lent = researcher.TryGetComp<LentColonistComp>();
            if (lent.DoingJointResearch && RimLinkMod.Active)
            {
                // Joint research - queue points to be sent
                ((ResearchMissionWorker) lent.MissionOffer.MissionWorker).QueuedResearchToSend += ResearchUtil.ResearchWorkToPointsPreTechLevel(amount);
            }
        }
    }
}
