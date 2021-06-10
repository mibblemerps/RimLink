using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RimLink.PawnStrings
{
    [HarmonyPatch(typeof(Page_ConfigureStartingPawns), "DrawPortraitArea")]
    public static class Patch_Page_ConfigureStartingPawns
    {
        private static FieldInfo CurPawn = typeof(Page_ConfigureStartingPawns)
            .GetField("curPawn", BindingFlags.Instance | BindingFlags.NonPublic);
        
        public static void Postfix(Page_ConfigureStartingPawns __instance, Rect rect)
        {
            if (!RimLinkMod.Instance.Settings.ImportExportPawn) return;
            
            Rect buttonsRect = rect.TopPartPixels(30f).RightPartPixels(205);
            buttonsRect.y += 40;

            // Get pawn
            Pawn pawn = (Pawn) CurPawn.GetValue(__instance);
            if (pawn == null) return;
            
            Rect importRect = buttonsRect.LeftPartPixels(100);
            Rect exportRect = buttonsRect.RightPartPixels(100);

            if (Widgets.ButtonText(importRect, "Import"))
            {
                Find.WindowStack.Add(new Dialog_ImportPawn(imported =>
                {
                    var startingPawns = Find.GameInitData.startingAndOptionalPawns;
                    int index = startingPawns.IndexOf(pawn);
                    PawnUtility.TryDestroyStartingColonistFamily(pawn);
                    pawn.relations.ClearAllRelations();
                    PawnComponentsUtility.RemoveComponentsOnDespawned(pawn);
                    Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);

                    foreach (var otherStartingPawns in startingPawns)
                    {
                        if (otherStartingPawns != null)
                            PawnUtility.TryDestroyStartingColonistFamily(otherStartingPawns);
                    }

                    startingPawns[index] = imported;
                    CurPawn.SetValue(__instance, imported);
                }));
            }
            if (Widgets.ButtonText(exportRect, "Export"))
            {
                Find.WindowStack.Add(new Dialog_ExportPawn(pawn));
            }
        }
    }
}