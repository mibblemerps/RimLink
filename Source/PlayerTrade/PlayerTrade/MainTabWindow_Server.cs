using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Chat;
using RimWorld;
using UnityEngine;
using Verse;

namespace PlayerTrade
{
    public class MainTabWindow_Server : MainTabWindow
    {
        public override Vector2 RequestedTabSize => new Vector2(1000f, 520f);
        public override MainTabWindowAnchor Anchor => MainTabWindowAnchor.Right;

        private string _chatBoxContent = "";
        private Vector2 _playersScrollPos;
        private Vector2 _chatHistoryScrollPos;
        private float _lastHeight = -1f;

        public MainTabWindow_Server()
        {
            closeOnAccept = false;

            if (!RimLinkMod.Active)
                return;

            RimLinkComp.Instance.Get<ChatSystem>().MessageReceived += (sender, message) =>
            {
                _chatHistoryScrollPos = new Vector2(0, _lastHeight * 2f);
            };
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
            base.DoWindowContents(inRect);

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

            Rect mainRect = new Rect(0, topBar.yMax, inRect.width, inRect.height - topBar.height);

            Rect playersRect = mainRect.LeftPart(0.25f);
            DrawPlayerList(playersRect);

            Rect chatRect = mainRect.RightPart(0.75f);
            DrawChat(chatRect);
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
            if (RimLinkComp.Instance.Connecting)
                Widgets.Label(reconnectingLabelRect, $"Reconnecting now...");
            else if (!float.IsNaN(RimLinkComp.Instance.TimeUntilReconnect))
                Widgets.Label(reconnectingLabelRect, $"Reconnecting in {Mathf.CeilToInt(Mathf.Max(0f, RimLinkComp.Instance.TimeUntilReconnect))} seconds...");
            Text.Anchor = TextAnchor.UpperLeft;

            Rect buttonRect = new Rect((rect.width / 2f - 75f), reconnectingLabelRect.yMax + 20f, 150f, 35f);
            if (!RimLinkComp.Instance.Connecting && Widgets.ButtonText(buttonRect, "Reconnect"))
                RimLinkComp.Instance.QueueConnect();

            GUI.EndGroup();
        }

        private void DrawPlayerList(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.179f, 0.214f, 0.246f));

            List<Player> players = RimLinkComp.Instance.Client.OnlinePlayers.Values.ToList();

            Rect viewRect = new Rect(0, 0, rect.width - 16f, 25f * players.Count);
            Widgets.BeginScrollView(rect, ref _playersScrollPos, viewRect);
            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                Rect playerRect = new Rect(0, i * 25f, viewRect.width, 25f);

                // Hover graphic
                if (Mouse.IsOver(playerRect))
                    Widgets.DrawBoxSolid(playerRect, new Color(1f, 1f, 1f, 0.1f));

                // Faction icon and player name
                GUI.color = player.Color.ToColor();
                Widgets.DrawTextureFitted(playerRect.LeftPartPixels(35f), FactionDefOf.PlayerColony.FactionIcon, 1f);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(playerRect.RightPartPixels(playerRect.width - 30f), player.Name.Colorize(player.Color.ToColor()));
                Text.Anchor = TextAnchor.UpperLeft;

                if (Widgets.ButtonInvisible(playerRect))
                {
                    Log.Message($"Player {player.Name} ({player.Guid}) clicked");
                }

                TooltipHandler.TipRegion(playerRect, () => $"Tradeable Now: {(player.TradeableNow ? "Yes" : "No")}\n" +
                                                           $"Wealth: ${player.Wealth}", player.Guid.GetHashCode() * 93245);
            }
            Widgets.EndScrollView();
        }

        private void DrawChat(Rect rect)
        {
            GUI.BeginGroup(rect);
            rect = new Rect(0, 0, rect.width, rect.height);

            Widgets.DrawBoxSolid(rect, new Color(0.23f, 0.23f, 0.23f));

            Rect chatHistoryRect = new Rect(0, 0, rect.width, rect.height - 35f);
            Rect viewRect = new Rect(0, 0, chatHistoryRect.width - 16f, _lastHeight < 0f ? chatHistoryRect.height : _lastHeight);
            Widgets.BeginScrollView(chatHistoryRect, ref _chatHistoryScrollPos, viewRect);
            _lastHeight = 0f;
            foreach (var msg in RimLinkComp.Instance.Get<ChatSystem>().Messages)
            {
                _lastHeight += DrawMessage(viewRect, _lastHeight, msg, false);
            }
            Widgets.EndScrollView();

            Rect chatBoxRect = new Rect(0, chatHistoryRect.yMax, rect.width, 35f);
            _chatBoxContent = Widgets.TextField(chatBoxRect.LeftPartPixels(chatBoxRect.width - 75f), _chatBoxContent);
            if (Widgets.ButtonText(chatBoxRect.RightPartPixels(75f), "Send"))
            {
                SendMessage();
            }

            if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Return)
            {
                SendMessage();
            }

            GUI.EndGroup();
        }

        private float DrawMessage(Rect viewRect, float currentHeight, ChatMessage message, bool dryRun)
        {
            float height = Mathf.Max(25f, Text.CalcHeight(message.Content, viewRect.width));
            if (dryRun)
                return height;

            Rect messageRect = new Rect(10f, currentHeight, viewRect.width - 10f, height);
            if (message.IsServer)
                Widgets.Label(messageRect, $"{message.Content}");
            else
                Widgets.Label(messageRect, $"<b>{message.Player.Name.Colorize(message.Player.Color.ToColor())}</b>  {message.Content}");

            return height;
        }

        private void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(_chatBoxContent))
                return; // Empty

            // Check for special client-side commands
            if (_chatBoxContent.Equals("/log", StringComparison.InvariantCultureIgnoreCase))
            {
                // Try to hide log window, if that fails (not open), show log window. (Toggle)
                if (!Find.WindowStack.TryRemove(typeof(EditWindow_Log), true))
                    Find.WindowStack.Add(new EditWindow_Log());
                _chatBoxContent = "";
                return;
            }
            if (_chatBoxContent.Equals("/clear", StringComparison.InvariantCultureIgnoreCase))
            {
                RimLinkComp.Instance.Get<ChatSystem>().Clear();
                _chatBoxContent = "";
                return;
            }
            if (_chatBoxContent.Equals("/tcpclose"))
            {
                RimLinkComp.Instance.Client.Tcp.Close();
                _chatBoxContent = "";
                return;
            }

            Log.Message($"Send message: {_chatBoxContent}");

            RimLinkComp.Instance.Client.SendPacket(new PacketSendChatMessage
            {
                Message = _chatBoxContent
            });

            _chatBoxContent = "";
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();

            if (IsOpen && RimLinkMod.Active) // Causes messages to become "read"
                RimLinkComp.Instance.Get<ChatSystem>().ReadMessages();
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
