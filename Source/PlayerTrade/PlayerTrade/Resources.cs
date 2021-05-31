using System.Collections.Generic;
using System.Linq;
using PlayerTrade.Net;
using PlayerTrade.Net.Packets;
using PlayerTrade.Patches;
using RimWorld;
using Verse;

namespace PlayerTrade
{
    /// <summary>
    /// Represents a colonies resources.
    /// </summary>
    public class Resources : IPacketable
    {
        public List<NetThing> Things = new List<NetThing>();
        public List<NetHuman> Pawns = new List<NetHuman>();

        public void Write(PacketBuffer buffer)
        {
            // Write things
            buffer.WriteInt(Things.Count);
            foreach (NetThing netThing in Things)
                netThing.Write(buffer);

            // Write pawns
            buffer.WriteInt(Pawns.Count);
            foreach (NetHuman netHuman in Pawns)
                buffer.WritePacketable(netHuman);
        }

        public void Read(PacketBuffer buffer)
        {
            Things.Clear();

            // Read things
            int thingsCount = buffer.ReadInt();
            Things = new List<NetThing>(thingsCount);
            for (int i = 0; i < thingsCount; i++)
                Things.Add(buffer.ReadPacketable<NetThing>());

            // Read pawns
            int pawnsCount = buffer.ReadInt();
            Pawns = new List<NetHuman>(pawnsCount);
            for (int i = 0; i < pawnsCount; i++)
                Pawns.Add(buffer.ReadPacketable<NetHuman>());
        }

        public void Update(Map map)
        {
            Things.Clear();

            Patch_TradeUtility_EverPlayerSellable.ForceEnable = true;
            foreach (Thing thing in TradeUtility.AllLaunchableThingsForTrade(map))
                Things.Add(NetThing.FromThing(thing));
            Patch_TradeUtility_EverPlayerSellable.ForceEnable = false;

            foreach (Pawn pawn in TradeUtility.AllSellableColonyPawns(map).Where(p => p.RaceProps.Humanlike))
                Pawns.Add(pawn.ToNetHuman());
        }

        public static Pawn FindSellablePawn(string guid)
        {
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn pawn in TradeUtility.AllSellableColonyPawns(map))
                {
                    PawnGuidThingComp pawnGuidComp = pawn.TryGetComp<PawnGuidThingComp>();
                    if (pawnGuidComp.Guid == guid)
                        return pawn;
                }
            }

            return null; // Pawn not found
        }
    }
}
