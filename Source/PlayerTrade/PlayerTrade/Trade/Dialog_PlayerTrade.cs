using System;
using System.Collections.Generic;
using System.Linq;
using PlayerTrade.Patches;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PlayerTrade.Trade
{
    [StaticConstructorOnStartup]
    public class Dialog_PlayerTrade : Window
    {
        private bool giftsOnly;
        private TransferableSorterDef sorter1;
        private TransferableSorterDef sorter2;
        private Vector2 scrollPosition = Vector2.zero;
        public static float lastCurrencyFlashTime = -100f;
        private List<Tradeable> cachedTradeables;
        private Tradeable cachedCurrencyTradeable;
        private const float TitleAreaHeight = 45f;
        private const float TopAreaHeight = 58f;
        private const float ColumnWidth = 120f;
        private const float FirstCommodityY = 6f;
        private const float RowInterval = 30f;
        private const float SpaceBetweenTraderNameAndTraderKind = 27f;
        private const float ShowSellableItemsIconSize = 32f;
        private const float GiftModeIconSize = 32f;
        private const float TradeModeIconSize = 32f;
        protected static readonly Vector2 AcceptButtonSize = new Vector2(160f, 40f);
        protected static readonly Vector2 OtherBottomButtonSize = new Vector2(160f, 40f);
        private static readonly Texture2D ShowSellableItemsIcon = ContentFinder<Texture2D>.Get("UI/Commands/SellableItems");
        private static readonly Texture2D GiftModeIcon = ContentFinder<Texture2D>.Get("UI/Buttons/GiftMode");
        private static readonly Texture2D TradeModeIcon = ContentFinder<Texture2D>.Get("UI/Buttons/TradeMode");

        public override Vector2 InitialSize => new Vector2(1024f, (float)UI.screenHeight);

        public Dialog_PlayerTrade(Pawn playerNegotiator, ITrader trader, bool giftsOnly = false)
        {
            Patch_TradeUtility_EverPlayerSellable.ForceEnable = true;
            TradeSession.SetupWith(trader, playerNegotiator, giftsOnly);
            forcePause = true;
            absorbInputAroundWindow = true;
            soundAppear = SoundDefOf.CommsWindow_Open;
            soundClose = SoundDefOf.CommsWindow_Close;
            soundAmbient = SoundDefOf.RadioComms_Ambience;

            sorter1 = TransferableSorterDefOf.Category;
            sorter2 = TransferableSorterDefOf.MarketValue;
        }

        public override void PostOpen()
        {
            base.PostOpen();
            CacheTradeables();
        }

        public override void DoWindowContents(Rect inRect)
        {
            GUI.BeginGroup(inRect);
            inRect = inRect.AtZero();
            TransferableUIUtility.DoTransferableSorters(sorter1, sorter2, (x =>
            {
                sorter1 = x;
                CacheTradeables();
            }), x =>
            {
                sorter2 = x;
                CacheTradeables();
            });
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            // Hide negotiator info (irrelevant to player trades, maybe one day that'll change)
            //Widgets.Label(new Rect(0.0f, 27f, inRect.width / 2f, inRect.height / 2f), "NegotiatorTradeDialogInfo".Translate((NamedArgument)TradeSession.playerNegotiator.Name.ToStringFull, (NamedArgument)TradeSession.playerNegotiator.GetStatValue(StatDefOf.TradePriceImprovement).ToStringPercent()));
            float x1 = inRect.width - 590f;
            Rect position = new Rect(x1, 0.0f, inRect.width - x1, 58f);
            GUI.BeginGroup(position);
            Text.Font = GameFont.Medium;
            Rect rect1 = new Rect(0.0f, 0.0f, position.width / 2f, position.height);
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(rect1, Faction.OfPlayer.Name.Truncate(rect1.width));
            Rect rect2 = new Rect(position.width / 2f, 0.0f, position.width / 2f, position.height);
            Text.Anchor = TextAnchor.UpperRight;
            string str = TradeSession.trader.TraderName;
            if (Text.CalcSize(str).x > (double)rect2.width)
            {
                Text.Font = GameFont.Small;
                str = str.Truncate(rect2.width);
            }
            Widgets.Label(rect2, str);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(new Rect(position.width / 2f, 27f, position.width / 2f, position.height / 2f), TradeSession.trader.TraderKind.LabelCap);
            Text.Anchor = TextAnchor.UpperLeft;
            if (!TradeSession.giftMode)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.6f);
                Text.Font = GameFont.Tiny;
                Rect rect3 = new Rect((float)((double)position.width / 2.0 - 100.0 - 30.0), 0.0f, 200f, position.height);
                Text.Anchor = TextAnchor.LowerCenter;
                TaggedString label = "PositiveBuysNegativeSells".Translate();
                Widgets.Label(rect3, label);
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
            GUI.EndGroup();
            float num1 = 0.0f;
            if (cachedCurrencyTradeable != null)
            {
                float num2 = inRect.width - 16f;
                TradeUI.DrawTradeableRow(new Rect(0.0f, 58f, num2, 30f), this.cachedCurrencyTradeable, 1);
                GUI.color = Color.gray;
                Widgets.DrawLineHorizontal(0.0f, 87f, num2);
                GUI.color = Color.white;
                num1 = 30f;
            }
            FillMainRect(new Rect(0.0f, 58f + num1, inRect.width, (float)((double)inRect.height - 58.0 - 38.0 - (double)num1 - 20.0)));
            Rect rect4 = new Rect((float)((double)inRect.width / 2.0 - (double)AcceptButtonSize.x / 2.0), inRect.height - 55f, AcceptButtonSize.x, AcceptButtonSize.y);
            if (Widgets.ButtonText(rect4, (TradeSession.giftMode ? "OfferGifts".Translate() : "AcceptButton".Translate())))
            {
                Find.WindowStack.Add(new Dialog_ConfirmTrade(TradeUtil.FormTradeOffer(), this));
            }
            if (Widgets.ButtonText(new Rect(rect4.x - 10f - OtherBottomButtonSize.x, rect4.y, OtherBottomButtonSize.x, OtherBottomButtonSize.y), (string)"ResetButton".Translate()))
            {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                TradeSession.deal.Reset();
                CacheTradeables();
                CountToTransferChanged();
            }
            if (Widgets.ButtonText(new Rect(rect4.xMax + 10f, rect4.y, OtherBottomButtonSize.x, OtherBottomButtonSize.y), (string)"CancelButton".Translate()))
            {
                Close(true);
                Event.current.Use();
            }
            float y = OtherBottomButtonSize.y;
            Rect rect5 = new Rect(inRect.width - y, rect4.y, y, y);
            if (Widgets.ButtonImageWithBG(rect5, ShowSellableItemsIcon, new Vector2(32f, 32f)))
                Find.WindowStack.Add((Window)new Dialog_SellableItems(TradeSession.trader));
            TooltipHandler.TipRegionByKey(rect5, "CommandShowSellableItemsDesc");
            Faction faction = TradeSession.trader.Faction;
            if (faction != null && !this.giftsOnly && !faction.def.permanentEnemy)
            {
                Rect rect3 = new Rect((float)((double)rect5.x - (double)y - 4.0), rect4.y, y, y);
                if (TradeSession.giftMode)
                {
                    if (Widgets.ButtonImageWithBG(rect3, TradeModeIcon, new Vector2?(new Vector2(32f, 32f))))
                    {
                        TradeSession.giftMode = false;
                        TradeSession.deal.Reset();
                        CacheTradeables();
                        CountToTransferChanged();
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    }
                    TooltipHandler.TipRegionByKey(rect3, "TradeModeTip");
                }
                else
                {
                    if (Widgets.ButtonImageWithBG(rect3, GiftModeIcon, new Vector2?(new Vector2(32f, 32f))))
                    {
                        TradeSession.giftMode = true;
                        TradeSession.deal.Reset();
                        CacheTradeables();
                        CountToTransferChanged();
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    }
                    TooltipHandler.TipRegionByKey(rect3, "GiftModeTip", (NamedArgument)faction.Name);
                }
            }
            GUI.EndGroup();
        }

        private void FillMainRect(Rect mainRect)
        {
            Text.Font = GameFont.Small;
            float height = (float)(6.0 + cachedTradeables.Count * 30.0);
            Rect viewRect = new Rect(0.0f, 0.0f, mainRect.width - 16f, height);
            Widgets.BeginScrollView(mainRect, ref scrollPosition, viewRect);
            float y = 6f;
            float num1 = scrollPosition.y - 30f;
            float num2 = scrollPosition.y + mainRect.height;
            int num3 = 0;
            for (int index1 = 0; index1 < cachedTradeables.Count; ++index1)
            {
                if ((double)y > num1 && (double)y < num2)
                {
                    Rect rect = new Rect(0.0f, y, viewRect.width, 30f);
                    int countToTransfer = cachedTradeables[index1].CountToTransfer;
                    Tradeable cachedTradeable = cachedTradeables[index1];
                    int index2 = num3;
                    try
                    {
                        TradeUI.DrawTradeableRow(rect, cachedTradeable, index2);
                    }
                    catch (Exception e)
                    {
                        Log.Error("Exception drawing tradeable row (why??)", e);
                    }

                    if (countToTransfer != cachedTradeables[index1].CountToTransfer)
                        CountToTransferChanged();
                }
                y += 30f;
                num3++;
            }
            Widgets.EndScrollView();
        }

        private void CacheTradeables()
        {
            Patch_TradeUtility_EverPlayerSellable.ForceEnable = true;
            QualityCategory qc;
            cachedTradeables = TradeSession.deal.AllTradeables.Where(tr =>
            {
                // if (tr.IsCurrency)
                //     return false;
                //return tr.TraderWillTrade || !TradeSession.trader.TraderKind.hideThingsNotWillingToTrade;
                return true;
            }).OrderByDescending(tr => !tr.TraderWillTrade ? -1 : 0)
                .ThenBy(tr => tr, sorter1.Comparer)
                .ThenBy((tr => tr), sorter2.Comparer)
                .ThenBy(tr => TransferableUIUtility.DefaultListOrderPriority(tr))
                .ThenBy(tr => tr.ThingDef.label)
                .ThenBy(tr => tr.AnyThing.TryGetQuality(out qc) ? (int)qc : -1)
                .ThenBy(tr => tr.AnyThing.HitPoints).ToList();
            Patch_TradeUtility_EverPlayerSellable.ForceEnable = false;
        }

        private void CountToTransferChanged() {}

        internal class TradeableInteractive : Tradeable
        {
            public TradeableInteractive() {}

            public TradeableInteractive(Thing thingColony, Thing thingTrader) : base(thingColony, thingTrader) {}

            public override bool Interactive => true;
        }
    }
}
