using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Labor;
using PlayerTrade.Net;
using PlayerTrade.Raids;
using PlayerTrade.Trade;
using RimWorld;
using UnityEngine;
using Verse;

namespace PlayerTrade
{
    public class Dialog_PlayerComms : Dialog_NodeTree
    {
        public Pawn Negotiator;
        public Player Player;

        private Player _self;

        public Dialog_PlayerComms(Pawn negotiator, Player player) : base(RootNodeForPlayer(negotiator, player), true)
        {
            Player = player;
            Negotiator = negotiator;

            // Mark dirty so we can have up-to-date info about ourselves in the comms window
            RimLinkComp.Find().Client.MarkDirty();
            _self = RimLinkComp.Find().Client.Player;

            forcePause = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            GUI.BeginGroup(inRect);
            Rect rect1 = new Rect(0.0f, 0.0f, inRect.width / 2f, 70f);
            Rect rect2 = new Rect(0.0f, rect1.yMax, rect1.width, 60f);
            Rect rect3 = new Rect(inRect.width / 2f, 0.0f, inRect.width / 2f, 70f);
            Rect rect4 = new Rect(inRect.width / 2f, rect1.yMax, rect1.width, 60f);
            Text.Font = GameFont.Medium;
            Widgets.Label(rect1, Negotiator.LabelCap);
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(rect3, Player.Name);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = new Color(1f, 1f, 1f, 0.7f);
            Widgets.Label(rect2, GetInfoText(_self));
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(rect4, GetInfoText(Player));
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            GUI.EndGroup();

            // Draw dialog node
            float rootNodeY = 147f;
            DrawNode(new Rect(0.0f, rootNodeY, inRect.width, inRect.height - rootNodeY));
        }

        private string GetInfoText(Player player)
        {
            return $"{player.Name.Colorize(ColoredText.FactionColor_Neutral)}\n" +
                   $"Day {player.Day}, {(WeatherDef.Named(player.Weather)).LabelCap}, {GenText.ToStringTemperature(player.Temperature)}\n" +
                   $"Wealth: {("$" + Mathf.RoundToInt(player.Wealth)).Colorize(Color.green)}";
        }

        public static DiaNode RootNodeForPlayer(Pawn negotiator, Player player)
        {
            bool canDoSocial = !negotiator.skills.GetSkill(SkillDefOf.Social).TotallyDisabled;

            var node = new DiaNode($"{negotiator.NameShortColored} greets {player.Name.Colorize(ColoredText.FactionColor_Neutral)} over the comms console.");

            var tradeOption = new DiaOption("Trade")
            {
                resolveTree = true,
                action = async () => { await TradeUtil.InitiateTrade(negotiator, player); }
            };
            if (!canDoSocial)
                tradeOption.Disable("WorkTypeDisablesOption".Translate((NamedArgument)SkillDefOf.Social.label));
            if (!player.TradeableNow)
                tradeOption.Disable("not tradeable currently");
            node.options.Add(tradeOption);

            var lendColonist = new DiaOption("Lend Colonist")
            {
                resolveTree = true,
                action = () => { Find.WindowStack.Add(new Dialog_LendColonist(player)); }
            };
            if (!Dialog_LendColonist.HasLendableColonist)
                lendColonist.Disable("no lendable colonists");
            if (!canDoSocial)
                lendColonist.Disable("WorkTypeDisablesOption".Translate((NamedArgument)SkillDefOf.Social.label));
            node.options.Add(lendColonist);

            var bountyOption = new DiaOption("Place Bounty")
            {
                resolveTree = true,
                action = () => { Find.WindowStack.Add(new Dialog_PlaceBounty(player)); }
            };
            if (!canDoSocial)
                bountyOption.Disable("WorkTypeDisablesOption".Translate((NamedArgument)SkillDefOf.Social.label));
            node.options.Add(bountyOption);

            var closeOption = new DiaOption($"({"Disconnect".Translate()})") {resolveTree = true};
            node.options.Add(closeOption);

            return node;
        }
    }
}
