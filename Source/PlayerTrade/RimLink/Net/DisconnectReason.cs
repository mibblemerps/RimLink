namespace RimLink.Net
{
    public enum DisconnectReason
    {
        /// <summary>
        /// The disconnect is being triggered due an issue with a packet or an illegal action.
        /// <b>Do auto-reconnect, do send disconnect packet</b> 
        /// </summary>
        Error,
        
        /// <summary>
        /// The disconnect is being triggered by a network issue. The network stream is likely incapable of sending a disconnect packet.
        /// <b>Do auto-reconnect, don't send disconnect packet</b> 
        /// </summary>
        Network,
        
        /// <summary>
        /// The user requested to disconnect from the server.
        /// <b>Don't auto-reconnect, do send disconnect packet</b> 
        /// </summary>
        User,
        
        /// <summary>
        /// The server has kicked the user for the server.
        /// <b>Don't auto-reconnect, don't send disconnect packet</b>
        /// </summary>
        Kicked
    }
}