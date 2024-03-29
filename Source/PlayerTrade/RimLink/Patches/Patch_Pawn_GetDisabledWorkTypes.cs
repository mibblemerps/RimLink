﻿using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimLink.Systems.Missions.Quest;
using Verse;

namespace RimLink.Patches
{
    /// <summary>
    /// Disables any work based on custom quest parts.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "GetDisabledWorkTypes")]
    public class Patch_Pawn_GetDisabledWorkTypes
    {
        private static FieldInfo _cachedDisabledWorkTypes =
            typeof(Pawn).GetField("cachedDisabledWorkTypes", BindingFlags.Instance | BindingFlags.NonPublic);

        private static void Prefix(bool permanentOnly, Pawn __instance, out bool __state)
        {
            // If we're getting temp work disables, and the cache is empty, set state to true so we know to add our stuff to the cache.
            __state = !permanentOnly && _cachedDisabledWorkTypes.GetValue(__instance) == null;
        }

        private static void Postfix(Pawn __instance, bool __state)
        {
            if (__state)
            {
                // Add our disabled work types by adding it into the cached list
                List<WorkTypeDef> disabled = (List<WorkTypeDef>) _cachedDisabledWorkTypes.GetValue(__instance);
                
                foreach (QuestPart_WorkTypeDefDisabled part in QuestPart_WorkTypeDefDisabled.GetWorkDefDisabledQuestPart(__instance))
                {
                    disabled.AddRange(part.DisabledWorkTypeDefs);
                }
            }
        }
    }
}
