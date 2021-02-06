using Verse;

namespace PlayerTrade
{
    /// <summary>
    /// Component attached to pawns so they can be identified globally, including across different player's saves.
    /// </summary>
    public class PawnGuidThingComp : ThingComp
    {
        public string Guid = System.Guid.NewGuid().ToString();

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref Guid, "rimlinkGuid", Guid, true);
        }
    }
}
