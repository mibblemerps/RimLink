namespace PlayerTrade
{
    public static class NameUtil
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
    }
}
