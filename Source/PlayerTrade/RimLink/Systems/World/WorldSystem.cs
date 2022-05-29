using System.Collections.Generic;
using RimLink.Core;
using RimLink.Net;
using RimLink.Net.Packets;
using RimLink.Util;
using RimWorld.Planet;
using Verse;

namespace RimLink.Systems.World
{
    public class WorldSystem : ISystem
    {
        public Dictionary<string, PlayerColonyWorldObject> ColonyWorldObjects = new Dictionary<string, PlayerColonyWorldObject>();

        private List<string> _colonyWorldObjectsKeys = new List<string>(); // for Scribe
        private List<PlayerColonyWorldObject> _colonyWorldObjectsValues = new List<PlayerColonyWorldObject>();  // for Scribe
        
        public void OnConnected(Client client)
        {
            client.PlayerUpdated += OnPlayerUpdated;
        }

        private void OnPlayerUpdated(object sender, Client.PlayerUpdateEventArgs e)
        {
            if (e.Player.Colonies == null) return;
            
            foreach (Colony colony in e.Player.Colonies)
            {
                if (!ColonyWorldObjects.ContainsKey(colony.Guid))
                {
                    // Create colony world object
                    PlayerColonyWorldObject worldObject = colony.MakeWorldObject();
                    ColonyWorldObjects.Add(colony.Guid, worldObject);
                    Log.Message($"Created world object for {e.Player}'s colony: {worldObject}");
                }
                else
                {
                    // Update existing world object
                    ColonyWorldObjects[colony.Guid].Name = colony.Name;
                }
            }
        }

        public void Update()
        {
            
        }
        
        public void ExposeData()
        {
            Scribe_Collections.Look(ref ColonyWorldObjects, "colonyWorldObjects",
                LookMode.Value, LookMode.Reference, ref _colonyWorldObjectsKeys, ref _colonyWorldObjectsValues);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (ColonyWorldObjects == null) ColonyWorldObjects = new Dictionary<string, PlayerColonyWorldObject>();
            }
        }
    }
}