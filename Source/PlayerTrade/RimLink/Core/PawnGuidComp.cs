using System.Linq;
using RimWorld;
using Verse;

namespace RimLink.Core
{
    /// <summary>
    /// Component attached to pawns so they can be identified globally, including across different player's saves.
    /// </summary>
    public class PawnGuidComp : ThingComp
    {
        public string Guid = System.Guid.NewGuid().ToString();

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref Guid, "rimlinkGuid", Guid, true);
        }

        public static Pawn FindByGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            
            return PawnsFinder.AllMaps.FirstOrDefault(pawn => ThingCompUtility.TryGetComp<PawnGuidComp>(pawn)?.Guid == guid);
        }
    }
}
