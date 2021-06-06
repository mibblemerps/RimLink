using System;
using System.IO;
using System.IO.Compression;
using PlayerTrade.Net;
using PlayerTrade.Net.Packets;
using PlayerTrade.Util;
using Verse;

namespace PlayerTrade.PawnStrings
{
    public static class PawnStringifyer
    {
        public static string Export(Pawn pawn)
        {
            byte[] data;
            using (var memoryStream = new MemoryStream())
            {
                using (var gzip = new GZipStream(memoryStream, CompressionLevel.Optimal))
                {
                    PacketBuffer buffer = new PacketBuffer(gzip);
                    buffer.WritePacketable(pawn.ToNetHuman(NetHumanUtil.Mode.StartingColonist));
                }
                data = memoryStream.ToArray();
            }

            return Convert.ToBase64String(data);
        }

        public static Pawn Import(string str)
        {
            byte[] data = Convert.FromBase64String(str);

            using (var memoryStream = new MemoryStream(data))
            {
                using (var gzip = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    PacketBuffer buffer = new PacketBuffer(gzip);
                    return buffer.ReadPacketable<NetHuman>().ToPawn();
                }
            }
        }
    }
}