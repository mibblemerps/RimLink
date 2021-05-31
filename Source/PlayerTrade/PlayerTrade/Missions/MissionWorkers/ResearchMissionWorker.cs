using PlayerTrade.Missions.Packets;
using RimWorld;
using UnityEngine;
using Verse;

namespace PlayerTrade.Missions.MissionWorkers
{
    public class ResearchMissionWorker : MissionWorker
    {
        public const float ResearchUpdateInterval = 1f;

        /// <summary>
        /// Research points that are queued up to be sent.
        /// </summary>
        public float QueuedResearchToSend;

        private float _lastResearchUpdate;

        public override bool IsColonistEligible(Pawn pawn)
        {
            // Must be capable of intellectual
            return !pawn.skills.GetSkill(SkillDefOf.Intellectual).TotallyDisabled;
        }

        public override void FulfillAsReceiver(Map map)
        {
            base.FulfillAsReceiver(map);

            WorkTypeDef patient = DefDatabase<WorkTypeDef>.GetNamed("Patient");
            WorkTypeDef bedRest = DefDatabase<WorkTypeDef>.GetNamed("PatientBedRest");
            WorkTypeDef research = DefDatabase<WorkTypeDef>.GetNamed("Research");

            foreach (Pawn pawn in Offer.Colonists)
            {
                // Set as doing joint research
                pawn.TryGetComp<LentColonistComp>().DoingJointResearch = true;

                // Set research work priorities
                MaybeSetPriority(pawn, patient);
                MaybeSetPriority(pawn, bedRest);
                MaybeSetPriority(pawn, research);
            }

            void MaybeSetPriority(Pawn pawn, WorkTypeDef def)
            {
                if (pawn.workSettings.GetPriority(def) < 3)
                    pawn.workSettings.SetPriority(def, 3);
            }
        }

        public override void Update()
        {
            base.Update();

            if (Time.realtimeSinceStartup > _lastResearchUpdate + ResearchUpdateInterval && QueuedResearchToSend > 0f)
            {
                _lastResearchUpdate = Time.realtimeSinceStartup;

                RimLinkComp.Instance.Client.SendPacket(new PacketResearch
                {
                    For = Offer.From,
                    Research = QueuedResearchToSend
                });

                QueuedResearchToSend = 0f;
            }
        }
    }
}

