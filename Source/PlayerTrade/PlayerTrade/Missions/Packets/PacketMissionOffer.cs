using System;
using System.Collections.Generic;
using PlayerTrade.Missions.MissionWorkers;
using PlayerTrade.Net;
using PlayerTrade.Net.Packets;

namespace PlayerTrade.Missions.Packets
{
    [Packet]
    public class PacketMissionOffer : PacketForPlayer
    {
        public string Guid;
        public string From;
        public int Payment;
        public int Bond;
        public float Days;
        public List<NetHuman> Colonists;
        public string MissionDefName;

        public string WorkerClassName;
        public MissionWorkers.MissionWorker Worker;

        public override bool ShouldQueue => false;

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

            buffer.WriteString(MissionDefName);

            buffer.WriteMarker("MissionWorker");
            buffer.WriteString(WorkerClassName);
            Worker.Write(buffer);
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

            MissionDefName = buffer.ReadString();

            // We read worker very "manually" because it can be multiple types
            buffer.ReadMarker("MissionWorker");
            WorkerClassName = buffer.ReadString();
            Worker = (MissionWorkers.MissionWorker) Activator.CreateInstance(Type.GetType(WorkerClassName));
            Worker.Read(buffer);
        }
    }
}
