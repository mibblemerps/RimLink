using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net;
using Verse;
using Log = PlayerTrade.Log;

namespace TradeServer
{
    public class QueuedPacketStorage
    {
        public const int FormatVersion = 2;
        public const int MaxPacketsStoredPerPlayer = 1000;

        public string SaveFileName = "queued_packets.dat";
        public Dictionary<string, List<Packet>> Storage = new Dictionary<string, List<Packet>>();

        public void StorePacket(string client, Packet packet)
        {
            if (!Storage.ContainsKey(client))
                Storage.Add(client, new List<Packet>());

            if (Storage[client].Count >= MaxPacketsStoredPerPlayer)
            {
                Log.Warn($"Player ({client}) has exceeded their maximum queuable packets ({MaxPacketsStoredPerPlayer})! Further packets will be thrown out.");
                return;
            }

            Storage[client].Add(packet);

            Save();
        }

        public IEnumerable<Packet> GetQueuedPackets(string client, bool removeFromQueue)
        {
            if (!Storage.ContainsKey(client))
                yield break;

            foreach (Packet packet in Storage[client])
                yield return packet;

            if (removeFromQueue)
            {
                Storage.Remove(client);
                Save();
            }
        }

        public void Save()
        {
            using (var stream = File.OpenWrite(SaveFileName))
            {
                PacketBuffer buffer = new PacketBuffer(stream);
                buffer.WriteInt(FormatVersion);

                buffer.WriteInt(Storage.Count);
                foreach (var kv in Storage)
                {
                    buffer.WriteString(kv.Key);
                    buffer.WriteInt(kv.Value.Count);
                    foreach (var packet in kv.Value)
                    {
                        buffer.WriteInt(Packet.Packets.First(p => p.Value == packet.GetType()).Key); // (packet ID)
                        buffer.WritePacketable(packet);
                    }
                }
            }
        }

        public void Load()
        {
            if (!File.Exists(SaveFileName))
            {
                // File doesn't exist
                Log.Message("Queued packet data file doesn't exist - a new one will be created.");
                Storage = new Dictionary<string, List<Packet>>();
                return;
            }

            using (var stream = File.OpenRead(SaveFileName))
            {
                PacketBuffer buffer = new PacketBuffer(stream);
                if (buffer.ReadInt() != FormatVersion)
                {
                    // Mismatched format version (or possibly corrupt)! Load blank storage.
                    Log.Error("Mismatched queued packet storage format! All queued packets lost. A backup of the old file has been made.");
                    File.Copy(SaveFileName, SaveFileName + ".backup", true);
                    Storage = new Dictionary<string, List<Packet>>();
                    return;
                }

                int storageCount = buffer.ReadInt();
                Storage = new Dictionary<string, List<Packet>>(storageCount);
                for (int i = 0; i < storageCount; i++)
                {
                    string guid = buffer.ReadString();
                    int queuedCount = buffer.ReadInt();
                    var queued = new List<Packet>(queuedCount);
                    for (int j = 0; j < queuedCount; j++)
                    {
                        Type packetType = Packet.Packets[buffer.ReadInt()];
                        Packet packet = (Packet) Activator.CreateInstance(packetType);
                        packet.Read(buffer);
                        queued.Add(packet);
                    }

                    Storage.Add(guid, queued);
                }
            }
            
        }
    }
}
