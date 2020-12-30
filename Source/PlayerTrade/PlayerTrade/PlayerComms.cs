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

        public string GetInfoText() => Username;

        public PlayerComms(string username)
        {
            Username = username;
        }

        public void TryOpenComms(Pawn negotiator)
        {
            Find.WindowStack.Add(new Dialog_PlayerComms(negotiator, Username));
        }

        public Faction GetFaction()
        {
            return Faction.OfPlayer;
        }

        public FloatMenuOption CommFloatMenuOption(Building_CommsConsole console, Pawn negotiator)
        {
            return new FloatMenuOption("Trade with " + Username.Colorize(ColoredText.FactionColor_Neutral), () =>
            {
                console.GiveUseCommsJob(negotiator, this);
            });
        }
    }
}
