using HarmonyLib;
using Verse;

namespace RimLink.Anticheat
{
    /// <summary>
    /// Performs an autosave whenever a bad letter is received.
    /// </summary>
    [HarmonyPatch(typeof(LetterStack), "ReceiveLetter", typeof(Letter), typeof(string))]
    public static class Patch_LetterStack_ReceiveLetter
    {
        public static void Postfix(Letter let, string debugInfo = null)
        {
            if (!AnticheatUtil.IsEnabled) return;
            
            if (AnticheatUtil.AutosaveOnLetterDefs.Contains(let.def))
                AnticheatUtil.AnticheatAutosave();
        }
    }
}