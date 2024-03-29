﻿using System.Collections.Generic;
using RimLink.Net;
using RimLink.Net.Packets;

namespace RimLink.Systems.Chat
{
    [Packet]
    public class PacketReceiveChatMessage : Packet
    {
        public List<NetMessage> Messages = new List<NetMessage>();

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteInt(Messages.Count);
            foreach (var msg in Messages)
            {
                buffer.WriteString(msg.From, true);
                buffer.WriteString(msg.Message);
            }
        }

        public override void Read(PacketBuffer buffer)
        {
            int msgCount = buffer.ReadInt();
            Messages = new List<NetMessage>(msgCount);
            for (int i = 0; i < msgCount; i++)
            {
                Messages.Add(new NetMessage
                {
                    From = buffer.ReadString(true),
                    Message = buffer.ReadString()
                });
            }
        }

        public class NetMessage
        {
            public string Message;
            public string From;
        }
    }
}
