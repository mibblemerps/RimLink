using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimLink.Patches
{
    /// <summary>
    /// Draws custom unlockable info on research projects.
    /// </summary>
    [HarmonyPatch(typeof(MainTabWindow_Research), "DrawUnlockableHyperlinks")]
    public class Patch_MainTabWindow_Research_DrawUnlockableHyperlinks
    {
        public static Dictionary<ResearchProjectDef, List<Entry>> Unlockables = new Dictionary<ResearchProjectDef, List<Entry>>();
        
        private static bool Prefix(ref float __result, Rect rect, ResearchProjectDef project)
        {
            if (!Unlockables.ContainsKey(project)) return true;
            
            float yMin = rect.yMin;
            float x = rect.x;
            foreach (var entry in Unlockables[project])
            {
                rect.x = x;
                Widgets.LabelCacheHeight(ref rect, "Unlocks".Translate() + ":");
                rect.x += 6f;
                rect.yMin += rect.height;

                Rect entryRect = new Rect(rect.x, rect.yMin, rect.width, 24f);
                if (entry.Icon != null)
                    Widgets.DefIcon(entryRect.LeftPartPixels(24f), entry.Icon);
                Widgets.Label(entryRect.RightPartPixels(entryRect.width - 29f), entry.Key.Translate().CapitalizeFirst());
            }
            
            __result = rect.yMin - yMin;
            return false;
        }

        public class Entry
        {
            public Def Icon;
            public string Key;
        }
    }
}