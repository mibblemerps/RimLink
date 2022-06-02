using System.Collections.Generic;
using System.Linq;
using RimLink.Util;
using RimLink.Net;
using RimLink.Net.Packets;
using RimLink.Patches;
using RimLink.Systems.Trade.Patches;
using RimWorld;
using Verse;

namespace RimLink.Core
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
            {
                if (thing.TryGetComp<CompBladelinkWeapon>()?.Biocoded == true) continue; // Don't allow bonded bladelink weapons to be traded

                Things.Add(NetThing.FromThing(thing));
            }
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
                    PawnGuidComp pawnGuidComp = pawn.TryGetComp<PawnGuidComp>();
                    if (pawnGuidComp.Guid == guid)
                        return pawn;
                }
            }

            return null; // Pawn not found
        }
    }
}
