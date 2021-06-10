using RimWorld;
using UnityEngine;
using Verse;

namespace RimLink.Core
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
            return new FloatMenuOption("Contact " + Player.Name.Colorize(ColoredText.FactionColor_Neutral) + (Player.IsOnline ? "" : " <i>(offline)</i>".Colorize(Color.gray)), () =>
            {
                console.GiveUseCommsJob(negotiator, this);
            });
        }
    }
}
