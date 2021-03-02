using RimWorld;
using Verse;

namespace PlayerTrade.Labor
{
    public class LentColonistEscapeDef : Def
    {
        public string reason;

        public ArrivalMethod arrival_method = ArrivalMethod.WalkIn;

        public bool tired = false;
        public bool hungry = false;
        public bool mugged = false;

        public MentalStateDef mental_state = null;
        public MentalStateDef fallback_mental_state = null;

        public bool damage;
        public int damage_max_parts_to_damage = 3;
        public float damage_max_damage_per_part = 0.66f;
        public float damage_max_bleed_rate = 4f;
        public bool damage_blunt = false;

        public enum ArrivalMethod
        {
            WalkIn,
            Shuttle,
            DropPod,
        }
    }
}
