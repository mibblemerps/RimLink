using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net;
using UnityEngine;
using Verse;

namespace PlayerTrade
{
    public static class MainMenuWidget
    {
        public static bool Enabled = true;
        public static PacketPingResponse LastPing;

        private static PingClient _pingClient = new PingClient();

        private static bool ShouldDoWidget => (bool) _shouldDoMainMenuPropertyInfo.GetValue(Find.UIRoot) && RimLinkMod.Instance.Settings.MainMenuWidgetEnabled;

        private static PropertyInfo _shouldDoMainMenuPropertyInfo =
            typeof(UIRoot_Entry).GetProperty("ShouldDoMainMenu", BindingFlags.NonPublic | BindingFlags.Instance);

        private static Vector2 _scrollPos = Vector2.zero;

        public static void Init()
        {
            _ = DoPingLoop();
        }

        public static void OnGUI()
        {
            if (!(Enabled && ShouldDoWidget && LastPing != null) && !string.IsNullOrWhiteSpace(RimLinkMod.Instance.Settings.ServerIp))
                return;

            Rect widgetRect = new Rect(20, 0, 300, Mathf.Max(UI.screenHeight / 3f, 410f));
            widgetRect = widgetRect.CenteredOnYIn(new Rect(0, 0, UI.screenWidth, UI.screenHeight));

            if (UI.screenWidth < 800)
                return; // Width too small

            Widgets.DrawBoxSolid(widgetRect, new Color(0.1f, 0.1f, 0.1f, 0.9f));
            widgetRect = widgetRect.ContractedBy(5f);

            GUI.BeginGroup(widgetRect);
            widgetRect = widgetRect.AtZero();

            if (string.IsNullOrWhiteSpace(RimLinkMod.Instance.Settings.ServerIp))
            {
                // No IP set
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(widgetRect.TopPartPixels(35f), "No server IP set");
                Text.Anchor = TextAnchor.UpperLeft;
                if (Widgets.ButtonText(new Rect(0, 35f, 180f, 35f).CenteredOnXIn(widgetRect), "Set Server IP"))
                    Find.WindowStack.Add(new Dialog_SetServerIp());
                return;
            }

            Rect titleRect = widgetRect.TopPartPixels(35f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(titleRect, LastPing.ServerName);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            if (LastPing.ProtocolVersion != RimLinkMod.ProtocolVersion)
            {
                // Incorrect version
                Widgets.Label(new Rect(0, titleRect.yMax + 20f, widgetRect.width, 20f), "Wrong version");
                return;
            }

            // Players online text
            Text.Anchor = TextAnchor.MiddleRight;
            Rect playerCountRect = titleRect.RightHalf();
            playerCountRect.xMax -= 5f;
            Widgets.Label(playerCountRect, LastPing.PlayersOnline.Count + "/" + LastPing.MaxPlayers);
            Text.Anchor = TextAnchor.UpperLeft;

            // Player list
            Rect playerList = new Rect(0, 35f, widgetRect.width, widgetRect.height - 70f);
            if (LastPing.PlayersOnline.Count > 0)
            {
                Rect viewRect = new Rect(0, 0, playerList.width - 16f, LastPing.PlayersOnline.Count * 20f);
                _scrollPos = GUI.BeginScrollView(playerList, _scrollPos, viewRect);
                int i = 0;
                foreach (Player player in LastPing.PlayersOnline)
                {
                    Rect playerRect = new Rect(0, i++ * 20f, viewRect.width, 20f);
                    Widgets.Label(playerRect, player.Name.Colorize(player.Color.ToColor()));
                    Widgets.DrawHighlightIfMouseover(playerRect);
                    TooltipHandler.TipRegion(playerRect, $"Day {player.Day}, Wealth ${player.Wealth}");
                }

                GUI.EndScrollView();
            }
            else
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(playerList, "No players online".Colorize(Color.gray));
                Text.Anchor = TextAnchor.UpperLeft;
            }

            // Set server button
            if (Widgets.ButtonText(widgetRect.BottomPartPixels(25f).RightPartPixels(130f), "Settings"))
            {
                RimLinkMod.ShowModSettings();
            }

            GUI.EndGroup();
        }

        private static async Task DoPingLoop()
        {
            while (true)
            {
                if (ShouldDoWidget && !string.IsNullOrWhiteSpace(RimLinkMod.Instance.Settings.ServerIp))
                {
                    // Do ping
                    try
                    {
                        if (_pingClient.Tcp == null || !_pingClient.Tcp.Connected)
                        {
                            // Connect
                            _pingClient.Tcp = new TcpClient();
                            await _pingClient.Tcp.ConnectAsync(RimLinkMod.Instance.Settings.ServerIp, RimLinkMod.Instance.Settings.ServerPort);
                            _pingClient.Stream = _pingClient.Tcp.GetStream();
                        }

                        LastPing = await _pingClient.Ping();
                    }
                    catch (Exception e)
                    {
                        Log.Warn("Error doing server ping " + e.Message + "\n" + e.StackTrace);
                        LastPing = null;
                        _pingClient.Tcp?.Close();
                        _pingClient.Tcp = null;
                    }
                }

                await Task.Delay(2000);
            }
        }
    }
}
