using System.Linq;
using UnityEngine;
using Verse;

namespace PlayerTrade.Trade
{
    public static class TradeOfferUI
    {
        public static void DrawTradeItems(Rect rect, ref Vector2 scrollPosition, TradeOffer offer)
        {
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
            float offeredHeight = offer.Things.Count(thing => thing.CountOffered > 0) * 30f;
            float requestedHeight = offer.Things.Count(thing => thing.CountOffered < 0) * 30f;
            float viewHeight = Mathf.Max(offeredHeight, requestedHeight, 20f);

            // Draw scroll view with trade things
            Rect viewRect = new Rect(0, 0, rect.width - 20f, viewHeight);
            scrollPosition = GUI.BeginScrollView(new Rect(0, 35f, rect.width - 16f, rect.height - 60f), scrollPosition, viewRect);
            DrawTradeThingsColumn(true, viewRect.LeftHalf(), offer);
            DrawTradeThingsColumn(false, viewRect.RightHalf(), offer);
            GUI.EndScrollView();

            // Market value info
            Rect infoRect = rect.BottomPartPixels(50f);
            infoRect.xMin += 30f;
            infoRect.xMax -= 30f;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(infoRect.TopHalf().LeftHalf(), $"Market value: {("$" + offer.OfferedMarketValue).Colorize(ColoredText.CurrencyColor)}");
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(infoRect.TopHalf().RightHalf(), $"Market value: {("$" + offer.RequestedMarketValue).Colorize(ColoredText.CurrencyColor)}");
            Text.Anchor = TextAnchor.LowerCenter;

            float tradeValueFactor = offer.OfferedMarketValue / offer.RequestedMarketValue; // 1 = fair, 1 > better value for us, 1 < worse value for us
            if (!offer.IsForUs)
                tradeValueFactor = offer.RequestedMarketValue / offer.OfferedMarketValue; // Flip values is the trade offer is not for us (from us)

            string otherPartyName = RimLinkComp.Instance.Client.GetName(offer.IsForUs ? offer.From : offer.For, true);

            Rect tradeSideRect = infoRect.BottomHalf();
            if (tradeValueFactor > 0.9f && tradeValueFactor < 1.1f)
                Widgets.Label(tradeSideRect, "This trade is roughly equal.");
            else if (tradeValueFactor > 2f)
                Widgets.Label(tradeSideRect, "This trade is significantly in our favor");
            else if (tradeValueFactor < 0.5f)
                Widgets.Label(tradeSideRect, $"This trade is significantly in {otherPartyName}'s favor");
            else if (tradeValueFactor > 1f)
                Widgets.Label(tradeSideRect, $"This trade is in our favor.");
            else if (tradeValueFactor < 1f)
                Widgets.Label(tradeSideRect, $"This trade is in {otherPartyName}'s favor");

            Text.Anchor = TextAnchor.UpperLeft;

            GUI.EndGroup();
        }

        private static void DrawTradeThingsColumn(bool offering, Rect rect, TradeOffer offer)
        {
            GUI.BeginGroup(rect);
            rect.position = Vector2.zero;

            int i = 0;
            foreach (TradeOffer.TradeThing trade in offer.Things)
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

        private static bool DrawTradeThingRow(TradeOffer.TradeThing trade, Rect rect, int index, bool offering)
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
