using System;

namespace PlayerTrade.Chat
{
    public class ChatMessage
    {
        public string From;
        public string Content;
        public DateTime Received = DateTime.Now;

        public bool IsServer => From == null;

        public Player Player
        {
            get
            {
                if (From == null)
                    return null;
                return RimLinkComp.Instance.Client.GetPlayer(From);
            }
        }

        public ChatMessage(string from, string content)
        {
            From = from;
            Content = content;
        }
    }
}
