using Verse;

namespace PlayerTrade.Missions.Escape
{
    public class LentColonistEscapeDef : Def
    {
        public string reason;

        public ArrivalMethod arrivalMethod = ArrivalMethod.WalkIn;

        public bool tired = false;
        public bool hungry = false;
        public bool mugged = false;

        public MentalStateDef mentalState = null;
        public MentalStateDef fallbackMentalState = null;

        public bool damage;
        public int damageMaxPartsToDamage = 3;
        public float damageMaxDamagePercentagePerPart = 0.66f;
        public float damageMaxBleedRate = 4f;
        public bool damageBlunt = false;

        public enum ArrivalMethod
        {
            WalkIn,
            Shuttle,
            DropPod,
        }
    }
}
