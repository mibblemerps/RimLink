using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net;
using RimWorld;
using Verse;

namespace PlayerTrade.Chat
{
    public class ChatWorker
    {
        public static int ChatMessageLimit = 200;

        private static FieldInfo _cachedLabelCapField = typeof(Def).GetField("cachedLabelCap", BindingFlags.Instance | BindingFlags.NonPublic);

        public event EventHandler<ChatMessage> MessageReceived;

        public Client Client;
        public List<ChatMessage> Messages = new List<ChatMessage>(ChatMessageLimit);

        public int UnreadMessages
        {
            get => _unreadMessages;
            protected set
            {
                if (_unreadMessages == value)
                    return; // already set
                _unreadMessages = value;
                SetUnreadCountMainButtonLabel();
            }
        }

        private int _unreadMessages = 0;
        private MainButtonDef _mainTabDef;

        public ChatWorker(Client client)
        {
            Client = client;

            Client.PacketReceived += OnPacketReceived;
            _mainTabDef = DefDatabase<MainButtonDef>.GetNamed("Server");
        }

        public void AddMessage(ChatMessage message)
        {
            if (Messages.Count >= ChatMessageLimit)
                Messages.RemoveAt(0); // Remove oldest message

            Messages.Add(message);
            if (message.From != Client.Guid)
                UnreadMessages++; // increment unread messages if it's not from us

            MessageReceived?.Invoke(this, message);
        }

        public void ReadMessages()
        {
            UnreadMessages = 0;
        }

        private void SetUnreadCountMainButtonLabel()
        {
            _mainTabDef.label = _unreadMessages == 0 ? "RimLink" : $"RimLink <b>({UnreadMessages})</b>";
            _cachedLabelCapField.SetValue(_mainTabDef, null); // Clear cached LabelCap
        }

        private void OnPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            if (e.Id == Packet.ReceiveChatMessagePacketId)
            {
                PacketReceiveChatMessage receivePacket = (PacketReceiveChatMessage) e.Packet;
                foreach (var msg in receivePacket.Messages)
                {
                    if (msg.From != null && !Client.Players.ContainsKey(msg.From) && msg.From != RimLinkComp.Instance.Guid)
                    {
                        Log.Warn("Message from unknown player! " + msg.From);
                        continue;
                    }

                    AddMessage(new ChatMessage(msg.From, msg.Message));
                }
            }
        }
    }
}
