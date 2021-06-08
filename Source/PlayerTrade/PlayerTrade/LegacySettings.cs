﻿using System;
using PlayerTrade.Raids;

namespace PlayerTrade
{
    [Obsolete]
    [Serializable]
    public class LegacySettings
    {
        public string ServerName = "RimLink Server";

        /// <summary>
        /// Disables developer mode, enables commitment mode. Permanent to save.
        /// </summary>
        public bool Anticheat;

        public float RaidBasePrice = Dialog_PlaceBounty.DefaultBasePrice;
        public int RaidMaxStrengthPercent = Dialog_PlaceBounty.DefaultMaxStrengthPercent;
    }
}