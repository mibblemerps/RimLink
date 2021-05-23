using System.Collections.Generic;
using System.Linq;
using PlayerTrade.Trade;
using PlayerTrade.Util;
using RimWorld;
using UnityEngine;
using Verse;

namespace PlayerTrade.MainTab
{
    public class TabTrades : ITab
    {
        private static Texture2D SendIcon = ContentFinder<Texture2D>.Get("UI/Send");
        private static Texture2D ReceiveIcon = ContentFinder<Texture2D>.Get("UI/Receive");
        
        private const float RowHeight = 40;
        
        private Vector2 _scrollPosition = Vector2.zero;
        
        public void Draw(Rect mainRect)
        {
            Widgets.DrawMenuSection(mainRect);

            List<TradeOffer> offers = RimLinkComp.Instance.Get<TradeSystem>().ActiveTradeOffers;

            TradeOffer pendingRetract = null;
            TradeOffer pendingReject = null;

            if (offers.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(mainRect, "Use the Comms Console to initiate trades.");
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }
            
            GUI.BeginGroup(mainRect);
            mainRect = mainRect.AtZero();
            
            Rect viewRect = new Rect(0, 0, mainRect.width, offers.Count * RowHeight);
            Widgets.BeginScrollView(mainRect, ref _scrollPosition, viewRect, true);
            int i = 0;
            foreach (TradeOffer offer in offers)
            {
                Rect rect = new Rect(0, RowHeight * i++, viewRect.width, RowHeight);
                
                // Row highlight
                if (Mouse.IsOver(rect))
                    Widgets.DrawHighlightSelected(rect);
                else if (i % 2 == 0)
                    Widgets.DrawHighlight(rect);

                bool isForUs = offer.IsForUs;

                float x = 5;
                
                // Draw send/receive icon
                Widgets.DrawTextureFitted(new Rect(x, rect.y, RowHeight, RowHeight), isForUs ? ReceiveIcon : SendIcon, 0.6f);
                x += RowHeight + 5;

                // Draw some icons
                int thingCount = 0;
                float thingX = x;
                foreach (TradeOffer.TradeThing trade in offer.Things)
                {
                    if ((isForUs && trade.CountOffered <= 0) || (!isForUs && trade.CountOffered >= 0)) continue;
                    Thing thing = trade.AllThings.FirstOrDefault();
                    if (thing == null) continue; // null thing?
                    Widgets.ThingIcon(new Rect(thingX, rect.y, RowHeight * 0.8f, RowHeight * 0.8f).CenteredOnYIn(new Rect(0, rect.y, 1, RowHeight)), thing);
                    thingX += RowHeight;
                    
                    if (++thingCount >= 3) break; // limit things
                }

                x += RowHeight * 3;

                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(x, rect.y, rect.width - x, rect.height), isForUs
                    ? "<b>" + "Received".Colorize(ColoredText.RedReadable) + "</b> from " + offer.From.GuidToName(true)
                    : "<b>" + "Sent".Colorize(new Color(0, 0.66f, 0)) + "</b> to " + offer.From.GuidToName(true));
                Text.Anchor = TextAnchor.UpperLeft;

                Rect buttons = rect.RightPartPixels(300).TopPartPixels(30).CenteredOnYIn(rect);
                buttons.x -= 8;
                bool clicked = false;
                if (offer.Fresh)
                {
                    // View button
                    if (Widgets.ButtonText(buttons.LeftHalf(), "View"))
                    {
                        ViewOffer(offer);
                        clicked = true;
                    }

                    if (isForUs)
                    {
                        // Reject button
                        if (Widgets.ButtonText(buttons.RightHalf(), "Reject"))
                        {
                            pendingReject = offer;
                            clicked = true;
                        }
                    }
                    else
                    {
                        // Retract button
                        if (Widgets.ButtonText(buttons.RightHalf(), "Retract"))
                        {
                            pendingRetract = offer;
                            clicked = true;
                        }
                    }
                }
                
                if (!clicked && Widgets.ButtonInvisible(rect))
                {
                    ViewOffer(offer);
                }
            }
            Widgets.EndScrollView();
            
            GUI.EndGroup();
            
            // Call pending actions (we do this outside the loop to prevent collection modified excepetions)
            pendingReject?.Reject();
            if (pendingRetract != null)
                TradeUtil.RetractOffer(pendingRetract);
        }

        public void Update() {}

        private void ViewOffer(TradeOffer offer)
        {
            if (!offer.Fresh)
            {
                Messages.Message("This trade offer is no longer available.", MessageTypeDefOf.RejectInput, false);
                return;
            }
            
            // Try to find existing offer letter, otherwise just create one.
            ChoiceLetter_TradeOffer letter = (ChoiceLetter_TradeOffer) Find.LetterStack.LettersListForReading
                .FirstOrFallback(l => l is ChoiceLetter_TradeOffer letterTradeOffer && letterTradeOffer.Offer.Guid == offer.Guid,
                    new ChoiceLetter_TradeOffer(offer));
            letter.OpenLetter();
        }
    }
}