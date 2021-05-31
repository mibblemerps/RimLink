using System;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using PlayerTrade.Net.Packets;
using UnityEngine;
using Verse;

namespace PlayerTrade
{
    public static class MainMenuWidget
    {
        public static bool Enabled = true;
        public static PacketPingResponse LastPing;
        public static Exception LastPingError;

        private static PingClient _pingClient = new PingClient();

        private static bool ShouldDoWidget => (bool) _shouldDoMainMenuPropertyInfo.GetValue(Find.UIRoot) && RimLinkMod.Instance.Settings.MainMenuWidgetEnabled;

        private static PropertyInfo _shouldDoMainMenuPropertyInfo =
            typeof(UIRoot_Entry).GetProperty("ShouldDoMainMenu", BindingFlags.NonPublic | BindingFlags.Instance);

        private static string _lastPingIp;
        private static Vector2 _scrollPos = Vector2.zero;

        public static void Init()
        {
            _ = DoPingLoop();
        }

        public static void OnGUI()
        {
            if (!(Enabled && ShouldDoWidget) && !string.IsNullOrWhiteSpace(RimLinkMod.Instance.Settings.ServerIp))
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
                OnGUINoServerIp(widgetRect);
            }
            else if (LastPing == null)
            {
                if (LastPingError == null)
                    OnGUIConnecting(widgetRect);
                else
                    OnGUICannotConnect(widgetRect);
            }
            else
            {
                OnGUIMain(widgetRect);
            }

            GUI.EndGroup();
        }

        private static void OnGUIMain(Rect rect)
        {
            Rect titleRect = rect.TopPartPixels(35f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(titleRect, LastPing.ServerName);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            if (LastPing.ProtocolVersion != RimLinkMod.ProtocolVersion)
            {
                // Incorrect version
                Widgets.Label(new Rect(0, titleRect.yMax + 20f, rect.width, 20f), "Wrong version");
                return;
            }

            // Players online text
            Text.Anchor = TextAnchor.MiddleRight;
            Rect playerCountRect = titleRect.RightHalf();
            playerCountRect.xMax -= 5f;
            Widgets.Label(playerCountRect, LastPing.PlayersOnline.Count + "/" + LastPing.MaxPlayers);
            Text.Anchor = TextAnchor.UpperLeft;

            // Player list
            Rect playerList = new Rect(0, 35f, rect.width, rect.height - 70f);
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
            if (Widgets.ButtonText(rect.BottomPartPixels(25f).RightPartPixels(130f), "Settings"))
            {
                RimLinkMod.ShowModSettings();
            }
        }

        private static void OnGUINoServerIp(Rect rect)
        {
            Text.Anchor = TextAnchor.UpperCenter;
            Rect textRect = rect.TopPartPixels(35f);
            textRect.y += 10f;
            Widgets.Label(textRect, "No server IP set");
            Text.Anchor = TextAnchor.UpperLeft;
            
            Rect buttonRect = rect.BottomPartPixels(35f);
            buttonRect.y -= 10f;
            buttonRect.width = 180f;
            buttonRect = buttonRect.CenteredOnXIn(rect);
            if (Widgets.ButtonText(buttonRect, "Set Server IP"))
                Find.WindowStack.Add(new Dialog_SetServerIp());
        }

        private static void OnGUIConnecting(Rect rect)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, "Connecting to server\n" + GenText.MarchingEllipsis(2));
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static void OnGUICannotConnect(Rect rect)
        {
            Text.Anchor = TextAnchor.UpperCenter;
            Rect textRect = rect.TopPartPixels(35f);
            textRect.y += 10f;
            Widgets.Label(rect.TopPartPixels(150f), "<b>RimLink Server Error</b>\n\n".Colorize(ColoredText.RedReadable) + LastPingError?.Message);
            Text.Anchor = TextAnchor.UpperLeft;

            Rect buttonRect = rect.BottomPartPixels(35f);
            buttonRect.y -= 10f;
            buttonRect.width = 180f;
            buttonRect = buttonRect.CenteredOnXIn(rect);
            if (Widgets.ButtonText(buttonRect, "Set Server IP"))
                Find.WindowStack.Add(new Dialog_SetServerIp());
        }

        private static async Task DoPingLoop()
        {
            while (true)
            {
                if (ShouldDoWidget && !string.IsNullOrWhiteSpace(RimLinkMod.Instance.Settings.ServerIp))
                {
                    string pingIpStr = RimLinkMod.Instance.Settings.ServerIp + ":" +
                                       RimLinkMod.Instance.Settings.ServerPort;
                    if (pingIpStr != _lastPingIp)
                    {
                        // Trying new server - clear last error
                        LastPingError = null;
                        _lastPingIp = pingIpStr;
                    }
                    
                    // Do ping
                    try
                    {
                        if (_pingClient.Tcp == null || !_pingClient.Tcp.Connected)
                        {
                            // Connect
                            _pingClient.Tcp = new TcpClient(AddressFamily.InterNetworkV6);
                            _pingClient.Tcp.Client.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                            await _pingClient.Tcp.ConnectAsync(RimLinkMod.Instance.Settings.ServerIp, RimLinkMod.Instance.Settings.ServerPort);
                            _pingClient.Stream = _pingClient.Tcp.GetStream();
                        }

                        LastPing = await _pingClient.Ping();
                        LastPingError = null;
                    }
                    catch (Exception e)
                    {
                        Log.Warn("Error doing server ping " + e.Message + "\n" + e.StackTrace);
                        LastPingError = e;
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
