using System;
using System.Collections.Generic;
using System.Linq;
using RimLink.Util;
using RimLink.Core;
using RimLink.Systems.Chat;
using RimLink.Systems.SettingSync;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimLink.MainTab
{
    public class TabChat : ITab
    {
        private string _chatBoxContent = "";
        private Vector2 _playersScrollPos;
        private Vector2 _chatHistoryScrollPos;
        
        private float _lastHeight = -1f;

        public TabChat()
        {
            RimLink.Instance.Get<ChatSystem>().MessageReceived += (sender, message) =>
            {
                _chatHistoryScrollPos = new Vector2(0, _lastHeight * 2f);
            };
        }

        public void Draw(Rect mainRect)
        {
            Rect playersRect = mainRect.LeftPart(0.25f);
            DrawPlayerList(playersRect);

            Rect chatRect = mainRect.RightPart(0.75f);
            DrawChat(chatRect);
        }

        private void DrawPlayerList(Rect rect)
        {
            //Widgets.DrawBoxSolid(rect, new Color(0.179f, 0.214f, 0.246f));
            Widgets.DrawMenuSection(rect);

            List<Player> players = RimLink.Instance.Client.OnlinePlayers.Values.ToList();

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
                                                           $"Wealth: ${player.Wealth}\n" +
                                                           $"Day: {player.Day}", player.Guid.GetHashCode() * 93245);
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
            foreach (var msg in RimLink.Instance.Get<ChatSystem>().Messages)
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
                RimLink.Instance.Get<ChatSystem>().Clear();
                _chatBoxContent = "";
                return;
            }
            if (_chatBoxContent.Equals("/tcpclose", StringComparison.InvariantCultureIgnoreCase))
            {
                RimLink.Instance.Client.Tcp.Close();
                _chatBoxContent = "";
                return;
            }
            if (_chatBoxContent.Equals("/pushsettings", StringComparison.InvariantCultureIgnoreCase))
            {
                var settingSyncSystem = RimLink.Instance.Get<SettingSyncSystem>();
                if (settingSyncSystem == null) Log.Error("SettingSyncSystem null");
                if (settingSyncSystem?.Settings == null) Log.Error("Settings null");
                RimLink.Instance.Get<SettingSyncSystem>().Settings.Push();
                _chatBoxContent = "";
                return;
            }

            Log.Message($"Send message: {_chatBoxContent}");

            RimLink.Instance.Client.SendPacket(new PacketSendChatMessage
            {
                Message = _chatBoxContent
            });

            _chatBoxContent = "";
        }

        public void Update()
        {
            if (RimLinkMod.Active) // Causes messages to become "read"
                RimLink.Instance.Get<ChatSystem>().ReadMessages();
        }
    }
}