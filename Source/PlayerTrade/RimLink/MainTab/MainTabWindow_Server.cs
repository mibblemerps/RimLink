using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimLink.MainTab
{
    public class MainTabWindow_RimLink : MainTabWindow
    {
        public override Vector2 RequestedTabSize => new Vector2(1000f, 520f);
        public override MainTabWindowAnchor Anchor => MainTabWindowAnchor.Right;

        private List<TabRecord> _tabs;
        private List<TabRecord> _adminTabs;
        private ITab _selectedTab;
        private TabChat _chat;
        private TabTrades _trades;
        private TabAdmin _admin;

        public MainTabWindow_RimLink()
        {
            closeOnAccept = false;

            _tabs = new List<TabRecord>
            {
                new TabRecord("Chat", () => { _selectedTab = _chat; }, () => _selectedTab == _chat),
                new TabRecord("Trades", () => { _selectedTab = _trades; }, () => _selectedTab == _trades),
            };

            _adminTabs = new List<TabRecord>(_tabs);
            _adminTabs.Add(new TabRecord("Admin", () => { _selectedTab = _admin; }, () => _selectedTab == _admin));
            
            _chat = new TabChat();
            _trades = new TabTrades();
            _admin = new TabAdmin();

            // Default tab
            _selectedTab = _chat;
        }

        public override void PostOpen()
        {
            base.PostOpen();

            if (!RimLinkMod.Active && string.IsNullOrWhiteSpace(RimLinkMod.Instance.Settings.ServerIp))
            {
                // Not connected, offer to connect to server
                Find.WindowStack.Add(new Dialog_SetServerIp());
                Close(false);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (!RimLinkMod.Active)
            {
                DrawDisconnectedFromServer(inRect);
                return;
            }

            Rect topBar = new Rect(0, 0, inRect.width, 35f);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Medium;
            Widgets.Label(topBar, Faction.OfPlayer.Name);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            if (Widgets.ButtonText(topBar.RightPartPixels(110f).TopPartPixels(20f), "Change Name"))
                ChangeFactionName();

            Rect mainRect = new Rect(0, topBar.yMax + 32, inRect.width, inRect.height - topBar.height - 32);

            _selectedTab?.Draw(mainRect);

            if (!RimLink.Instance.IsAdmin && _selectedTab == _admin)
                _selectedTab = _chat; // Reset selected tab if it was admin but we're no longer admin

            TabDrawer.DrawTabs(mainRect, RimLink.Instance.IsAdmin ? _adminTabs : _tabs);
        }

        private void DrawDisconnectedFromServer(Rect rect)
        {
            GUI.BeginGroup(rect);

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Rect mainLabelRect = new Rect(0f, 20f, rect.width, 35f);
            Widgets.Label(mainLabelRect, "Disconnected from server");
            Text.Font = GameFont.Small;

            Rect reconnectingLabelRect = new Rect(0, mainLabelRect.yMax + 20f, rect.width, 25f);
            if (RimLink.Instance.ConnectionManager.Connecting)
                Widgets.Label(reconnectingLabelRect, $"Reconnecting now...");
            else if (!float.IsNaN(RimLink.Instance.ConnectionManager.TimeUntilReconnect))
                Widgets.Label(reconnectingLabelRect, $"Reconnecting in {Mathf.CeilToInt(Mathf.Max(0f, RimLink.Instance.ConnectionManager.TimeUntilReconnect))} seconds...");
            Text.Anchor = TextAnchor.UpperLeft;

            Rect buttonRect = new Rect((rect.width / 2f - 75f), reconnectingLabelRect.yMax + 20f, 150f, 35f);
            if (!RimLink.Instance.ConnectionManager.Connecting && Widgets.ButtonText(buttonRect, "Reconnect"))
                RimLink.Instance.ConnectionManager.QueueConnect();

            GUI.EndGroup();
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();

            if (IsOpen)
                _selectedTab?.Update();
        }

        private static void ChangeFactionName()
        {
            Find.WindowStack.Add(new Dialog_NamePlayerFaction
            {
                doCloseX = true
            });
        }
    }
}
