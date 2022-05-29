using RimLink.Net;
using RimLink.Net.Packets;
using RimLink.Systems.World;
using RimLink.Util;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimLink.Core
{
    public class Colony : IPacketable
    {
        public Player Player
        {
            get
            {
                if (_cachedPlayer == null)
                    _cachedPlayer = RimLink.Instance.Client?.GetPlayer(OwnerGuid);
                return _cachedPlayer;
            }
        }

        public string Guid => OwnerGuid + "_" + Id;
        
        public string OwnerGuid;
        public int Id;
        public string Name;
        public string Seed;
        public int Tile;

        private Player _cachedPlayer;

        public PlayerColonyWorldObject MakeWorldObject()
        {
            var worldObject = (PlayerColonyWorldObject) WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("RimLinkColony"));
            worldObject.Name = Name;
            worldObject.Player = Player;
            worldObject.Tile = Tile;
            worldObject.SetFaction(Player.Faction);
            Find.World.worldObjects.Add(worldObject);

            return worldObject;
        }

        public void Write(PacketBuffer buffer)
        {
            buffer.WriteString(OwnerGuid);
            buffer.WriteInt(Id);
            buffer.WriteString(Name);
            buffer.WriteString(Seed);
            buffer.WriteInt(Tile);
        }

        public void Read(PacketBuffer buffer)
        {
            OwnerGuid = buffer.ReadString();
            Id = buffer.ReadInt();
            Name = buffer.ReadString();
            Seed = buffer.ReadString();
            Tile = buffer.ReadInt();
        }
    }
}