using System;
using PlayerTrade.Raids;

namespace PlayerTrade
{
    [Serializable]
    public class GameSettings
    {
        /// <summary>
        /// Disables developer mode, enables commitment mode. Permanent to save.
        /// </summary>
        public bool Anticheat;

        // todo: implement raid settings
        public float RaidBasePrice = Dialog_PlaceBounty.DefaultBasePrice;
        public int RaidMaxStrengthPercent = Dialog_PlaceBounty.DefaultMaxStrengthPercent;
    }
}
