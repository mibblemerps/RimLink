using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace PlayerTrade.Trade
{
    public class Dialog_TradeOffer : Dialog_NodeTreeWithFactionInfo
    {
        public TradeOffer Offer;

        private Vector2 _scrollPosition = Vector2.zero;

        // Make window wider
        public override Vector2 InitialSize => new Vector2(780, 540);

        public Dialog_TradeOffer(DiaNode nodeRoot, TradeOffer offer, bool delayInteractivity = false, bool radioMode = false, string title = null) : base(nodeRoot, null, delayInteractivity, radioMode, title)
        {
            Offer = offer;
        }

        public override void DoWindowContents(Rect inRect)
        {
            base.DoWindowContents(inRect);
            
            Rect tradeItemsRect = new Rect(0, 0, inRect.width, 320f);
            DrawTradeItems(tradeItemsRect);
        }

        private void DrawTradeItems(Rect rect)
        {
            rect.yMin += 30f;
            GUI.BeginGroup(rect);
            rect = rect.AtZero();

            // Draw headings
            Rect headings = rect.TopPartPixels(30f);
            rect.yMin += 30f;
            Text.Font = GameFont.Medium;
            Widgets.Label(headings.LeftHalf(), "Offering...");
            Widgets.Label(headings.RightHalf(), "In Exchange For...");
            Text.Font = GameFont.Small;

            // Calculate view height
            float offeredHeight = Offer.Things.Count(thing => thing.CountOffered > 0) * 30f;
            float requestedHeight = Offer.Things.Count(thing => thing.CountOffered < 0) * 30f;
            float viewHeight = Mathf.Max(offeredHeight, requestedHeight, 20f);

            // Draw scroll view with trade things
            Rect viewRect = new Rect(0, 0, rect.width - 20f, viewHeight);
            _scrollPosition = GUI.BeginScrollView(new Rect(0, 35f, rect.width - 16f, rect.height - 60f), _scrollPosition, viewRect);
            DrawTradeThingsColumn(true, viewRect.LeftHalf());
            DrawTradeThingsColumn(false, viewRect.RightHalf());
            GUI.EndScrollView();

            // Market value info
            Rect infoRect = rect.BottomPartPixels(50f);
            infoRect.xMin += 30f;
            infoRect.xMax -= 30f;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(infoRect.TopHalf().LeftHalf(), $"Market value: {("$" + Offer.OfferedMarketValue).Colorize(ColoredText.CurrencyColor)}");
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(infoRect.TopHalf().RightHalf(), $"Market value: {("$" + Offer.RequestedMarketValue).Colorize(ColoredText.CurrencyColor)}");
            Text.Anchor = TextAnchor.LowerCenter;
            float tradeValueFactor = Offer.OfferedMarketValue / Offer.RequestedMarketValue; // 1 = fair, 1 > better value for us, 1 < worse value for us
            Rect tradeSideRect = infoRect.BottomHalf();
            if (tradeValueFactor > 0.9f && tradeValueFactor < 1.1f)
                Widgets.Label(tradeSideRect, "This trade is roughly equal.");
            else if (tradeValueFactor > 2f)
                Widgets.Label(tradeSideRect, "This trade is significantly in our favor");
            else if (tradeValueFactor < 0.5f)
                Widgets.Label(tradeSideRect, $"This trade is significantly in {Offer.From}'s favor");
            else if (tradeValueFactor > 1f)
                Widgets.Label(tradeSideRect, $"This trade is in our favor.");
            else if (tradeValueFactor < 1f)
                Widgets.Label(tradeSideRect, $"This trade is in {Offer.From}'s favor");

            Text.Anchor = TextAnchor.UpperLeft;

            GUI.EndGroup();
        }

        private void DrawTradeThingsColumn(bool offering, Rect rect)
        {

            GUI.BeginGroup(rect);
            rect.position = Vector2.zero;

            int i = 0;
            foreach (TradeOffer.TradeThing trade in Offer.Things)
            {
                i++;
                Rect thingRect = new Rect(0, (i - 1) * 30f, rect.width, 30f);
                if (!DrawTradeThingRow(trade, thingRect, i, offering))
                    i--; // Draw trade row failed - decrement index back down since row wasn't drawn
            }

            if (i == 0)
            {
                // None
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(rect, "<i>(nothing)</i>".Colorize(Color.gray));
                Text.Anchor = TextAnchor.UpperLeft;
            }

            GUI.EndGroup();
        }

        private bool DrawTradeThingRow(TradeOffer.TradeThing trade, Rect rect, int index, bool offering)
        {
            Thing thing = trade.AllThings.FirstOrDefault();
            if (thing == null)
                return false;
            int count = offering ? trade.CountOffered : -trade.CountOffered;
            if (count <= 0)
                return false;

            // Draw highlight
            if (index % 2 == 0)
                Widgets.DrawLightHighlight(rect);

            // Draw icon and label
            Rect iconRect = rect.LeftPartPixels(30f);
            iconRect = new Rect(iconRect.x + 2f, iconRect.y + 2f, iconRect.width - 4f, iconRect.height - 4f);
            Widgets.ThingIcon(iconRect, thing);
            rect.xMin += 35f;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            if (thing is Pawn pawn)
                Widgets.Label(rect, pawn.NameFullColored);
            else
                Widgets.Label(rect, $"{(count + "x").Colorize(Color.gray)} {thing.LabelCapNoCount}");
            Text.Anchor = TextAnchor.UpperLeft;

            // Draw market value
            Rect marketValueRect = rect.RightPartPixels(100f);
            marketValueRect.xMax -= 35f;
            Text.Anchor = TextAnchor.MiddleRight;
            Text.Font = GameFont.Tiny;
            Widgets.Label(marketValueRect, ("$" + trade.MarketValue).Colorize(Color.gray));
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            // Draw info icon
            Vector2 infoCardPos = rect.RightPartPixels(35f).position;
            Widgets.InfoCardButton(infoCardPos.x, infoCardPos.y, thing);

            return true;
        }
    }
}
