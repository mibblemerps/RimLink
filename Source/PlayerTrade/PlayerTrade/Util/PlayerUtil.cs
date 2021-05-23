namespace PlayerTrade.Util
{
    public static class PlayerUtil
    {
        /// <summary>
        /// Resolve a GUID to a name in a safe way.
        /// </summary>
        /// <param name="guid">GUID</param>
        /// <param name="colored">Use player color?</param>
        /// <returns>Player name, or GUID fallback if necessary.</returns>
        public static string GuidToName(this string guid, bool colored = false)
        {
            if (!RimLinkMod.Active)
                return "{" + guid + "}";

            return RimLinkComp.Instance.Client.GetName(guid, colored);
        }

        /// <summary>
        /// Get player from their GUID.
        /// </summary>
        /// <param name="guid">GUID</param>
        /// <returns>Player instance.</returns>
        public static Player GuidToPlayer(this string guid)
        {
            return RimLinkMod.Active ? RimLinkComp.Instance.Client.GetPlayer(guid) : null;
        }
    }
}
