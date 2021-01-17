using System;

namespace PlayerTrade.Chat
{
    public class ChatMessage
    {
        public string From;
        public string Content;
        public DateTime Received = DateTime.Now;

        public Player Player
        {
            get
            {
                if (!RimLinkComp.Instance.Client.Players.ContainsKey(From))
                    return null;
                return RimLinkComp.Instance.Client.Players[From];
            }
        }

        public ChatMessage(string from, string content)
        {
            From = from;
            Content = content;
        }
    }
}
