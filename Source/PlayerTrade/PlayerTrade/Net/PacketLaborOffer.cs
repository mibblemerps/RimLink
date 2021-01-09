﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PlayerTrade.Net
{
    public class PacketLaborOffer : PacketForPlayer
    {
        public string Guid;
        public string From;
        public int Payment;
        public int Bond;
        public float Days;
        public List<NetHuman> Colonists;

        public override void Write(PacketBuffer buffer)
        {
            base.Write(buffer);
            buffer.WriteString(Guid);
            buffer.WriteString(From);
            buffer.WriteInt(Payment);
            buffer.WriteInt(Bond);
            buffer.WriteFloat(Days);

            buffer.WriteInt(Colonists.Count);
            foreach (var colonist in Colonists)
                buffer.WritePacketable(colonist);
        }

        public override void Read(PacketBuffer buffer)
        {
            base.Read(buffer);
            Guid = buffer.ReadString();
            From = buffer.ReadString();
            Payment = buffer.ReadInt();
            Bond = buffer.ReadInt();
            Days = buffer.ReadFloat();

            int colonistCount = buffer.ReadInt();
            Colonists = new List<NetHuman>(colonistCount);
            for (int i = 0; i < colonistCount; i++)
                Colonists.Add(buffer.ReadPacketable<NetHuman>());
        }
    }
}
