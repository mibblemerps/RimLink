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
        public readonly Player Player;

        public string GetCallLabel() => Player.Name;

        public string GetInfoText() => Player.Name;

        public PlayerComms(Player player)
        {
            Player = player;
        }

        public void TryOpenComms(Pawn negotiator)
        {
            Find.WindowStack.Add(new Dialog_PlayerComms(negotiator, Player));
        }

        public Faction GetFaction()
        {
            return Faction.OfPlayer;
        }

        public FloatMenuOption CommFloatMenuOption(Building_CommsConsole console, Pawn negotiator)
        {
            return new FloatMenuOption("Trade with " + Player.Name.Colorize(ColoredText.FactionColor_Neutral), () =>
            {
                console.GiveUseCommsJob(negotiator, this);
            });
        }
    }
}
