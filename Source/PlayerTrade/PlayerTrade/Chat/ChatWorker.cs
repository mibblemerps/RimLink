using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net;

namespace PlayerTrade.Chat
{
    public class ChatWorker
    {
        public static int ChatMessageLimit = 200;

        public event EventHandler<ChatMessage> MessageReceived;

        public Client Client;
        public List<ChatMessage> Messages = new List<ChatMessage>(ChatMessageLimit);

        public ChatWorker(Client client)
        {
            Client = client;

            Client.PacketReceived += OnPacketReceived;
        }

        public void AddMessage(ChatMessage message)
        {
            if (Messages.Count >= ChatMessageLimit)
                Messages.RemoveAt(0); // Remove oldest message

            Messages.Add(message);
            MessageReceived?.Invoke(this, message);
        }

        private void OnPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            if (e.Id == Packet.ReceiveChatMessagePacketId)
            {
                PacketReceiveChatMessage receivePacket = (PacketReceiveChatMessage) e.Packet;
                foreach (var msg in receivePacket.Messages)
                {
                    if (!Client.Players.ContainsKey(msg.From) && msg.From != RimLinkComp.Instance.Guid)
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
