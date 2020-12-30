using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace PlayerTrade
{
    public class Dialog_PlayerComms : Dialog_NodeTree
    {
        public Pawn Negotiator;
        public string Player;

        public Dialog_PlayerComms(Pawn negotiator, string player) : base(RootNodeForPlayer(negotiator, player), true)
        {
            Player = player;
            Negotiator = negotiator;
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
            Widgets.Label(rect3, Player);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = new Color(1f, 1f, 1f, 0.7f);
            Widgets.Label(rect2, ""); // todo: add info text for our player
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(rect4, ""); // todo: add info text for other player
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            GUI.EndGroup();

            // Draw dialog node
            float rootNodeY = 147f;
            DrawNode(new Rect(0.0f, rootNodeY, inRect.width, inRect.height - rootNodeY));
        }

        public static DiaNode RootNodeForPlayer(Pawn negotiator, string player)
        {
            bool canDoSocial = !negotiator.skills.GetSkill(SkillDefOf.Social).TotallyDisabled;

            var node = new DiaNode($"{negotiator.NameShortColored} greets {player.Colorize(ColoredText.FactionColor_Neutral)} over the comms console.");

            var tradeOption = new DiaOption("Trade")
            {
                resolveTree = true,
                action = async () => { await TradeUtil.InitiateTrade(negotiator, player); }
            };
            if (!canDoSocial)
                tradeOption.Disable("WorkTypeDisablesOption".Translate((NamedArgument)SkillDefOf.Social.label));
            node.options.Add(tradeOption);

            var closeOption = new DiaOption($"({"Disconnect".Translate()})") {resolveTree = true};
            node.options.Add(closeOption);

            return node;
        }
    }
}
