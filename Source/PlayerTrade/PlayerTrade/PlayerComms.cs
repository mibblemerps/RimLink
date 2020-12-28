using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace PlayerTrade
{
    public class PlayerComms : ICommunicable
    {
        public readonly string Username;

        public string GetCallLabel() => Username;

        public string GetInfoText() => Username + " Info";

        public PlayerComms(string username)
        {
            Username = username;
        }

        public async void TryOpenComms(Pawn negotiator)
        {
            PacketColonyResources packet = await PlayerTradeMod.Instance.Client.GetColonyResources(Username);

            Log.Message($"Other colony has {packet.Resources.Things.Count} things.");

            var playerTrader = new PlayerTrader(Username, packet.Resources);

            Find.WindowStack.Add(new Dialog_PlayerTrade(negotiator, playerTrader));
        }

        public Faction GetFaction()
        {
            return Faction.OfPlayer;
        }

        public FloatMenuOption CommFloatMenuOption(Building_CommsConsole console, Pawn negotiator)
        {
            return new FloatMenuOption("Trade with " + Username, () =>
            {
                console.GiveUseCommsJob(negotiator, this);
            });
        }
    }
}
