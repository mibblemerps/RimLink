using System.Collections.Generic;
using System.Reflection;
using RimLink.Patches;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimLink.Util
{
    /*
     * Research work:   This value input into ResearchManager.ResearchPerformed - before any tech levels, difficulty values, work per tick is accounted for.
     *
     * Research points: Research points has the current project tech level, difficulty values, and work per tick accounted for.
     *                  The conversion from research work to research points depends on what project is currently selected.
     */
    public static class ResearchUtil
    {
        private static FieldInfo _researchWorkPerTick = typeof(ResearchManager).GetField("ResearchPointsPerWorkTick", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo _researchProgress = typeof(ResearchManager).GetField("progress", BindingFlags.NonPublic | BindingFlags.Instance);

        public static Dictionary<ResearchProjectDef, float> ProgressDictionary => (Dictionary<ResearchProjectDef, float>) _researchProgress.GetValue(Find.ResearchManager);

        public static float ResearchWorkToPointsPreTechLevel(float work)
        {
            ResearchManager research = Find.ResearchManager;
            work *= (float)_researchWorkPerTick.GetValue(research);
            work *= Find.Storyteller.difficultyValues.researchSpeedFactor;
            if (DebugSettings.fastResearch)
                work *= 500f;
            return work;
        }

        public static float AddResearchPoints(float points)
        {
            ResearchManager research = Find.ResearchManager;

            if (research.currentProj == null)
            {
                Log.Error("Researched without having an active project.");
                return 0f;
            }

            float costFactor = research.currentProj.CostFactor(Find.FactionManager.OfPlayer.def.techLevel);

            // How much work is needed to finish this research
            float pointsNeededToFinish = (research.currentProj.baseCost - research.GetProgress(research.currentProj)) * costFactor;

            // Don't spend more work than what's needed to finish the research
            float pointsToSpend = Mathf.Min(pointsNeededToFinish, points);

            ProgressDictionary[research.currentProj] = research.GetProgress(research.currentProj) + (pointsToSpend / costFactor);
            if (research.currentProj.IsFinished)
                research.FinishProject(research.currentProj, true);

            return pointsToSpend;
        }

        public static void AddUnlockableInfo(ResearchProjectDef project, string key, Def icon)
        {
            if (!Patch_MainTabWindow_Research_DrawUnlockableHyperlinks.Unlockables.ContainsKey(project))
                Patch_MainTabWindow_Research_DrawUnlockableHyperlinks.Unlockables.Add(project, new List<Patch_MainTabWindow_Research_DrawUnlockableHyperlinks.Entry>());
            
            Patch_MainTabWindow_Research_DrawUnlockableHyperlinks.Unlockables[project].Add(new Patch_MainTabWindow_Research_DrawUnlockableHyperlinks.Entry
            {
                Key = key,
                Icon = icon
            });
        }
    }
}
