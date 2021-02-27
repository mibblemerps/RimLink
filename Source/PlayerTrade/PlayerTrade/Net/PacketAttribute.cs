using System;

namespace PlayerTrade.Net
{
    public class PacketAttribute : Attribute
    {
        /// <summary>
        /// Packet ID 0 means the ID will be auto-assigned.
        /// </summary>
        public int Id = 0;

        /// <summary>
        /// Very frequent or regular packets should be hidden from the log to prevent log spam.
        /// </summary>
        public bool HideFromLog;
    }
}
