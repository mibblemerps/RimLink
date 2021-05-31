using Verse;

namespace PlayerTrade
{
    public class ModSettings : Verse.ModSettings
    {
        public string ServerIp;
        public int ServerPort = 35562;
        public bool LoggingEnabled = false;
        public bool MainMenuWidgetEnabled = true;
        public bool ChatNotificationsEnabled = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref ServerIp, "server_ip");
            Scribe_Values.Look(ref ServerPort, "server_port");
            Scribe_Values.Look(ref LoggingEnabled, "logging_enabled");
            Scribe_Values.Look(ref MainMenuWidgetEnabled, "main_menu_widget_enabled");
            Scribe_Values.Look(ref ChatNotificationsEnabled, "chat_notifications");
            base.ExposeData();
        }
    }
}
