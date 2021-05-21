using System.Collections.Generic;
using Verse;

namespace PlayerTrade.Util
{
    public static class Scriber
    {
        public static void Collection<T>(ref List<T> list, string label, LookMode lookMode = LookMode.Undefined)
        {
            Scribe_Collections.Look(ref list, label, lookMode);

            // Not sure why RimWorld doesn't do this natively.
            // Here we initalize a blank list if the list didn't load (wasn't in the save file)
            if (Scribe.mode == LoadSaveMode.PostLoadInit && list == null)
                list = new List<T>();
        }
    }
}