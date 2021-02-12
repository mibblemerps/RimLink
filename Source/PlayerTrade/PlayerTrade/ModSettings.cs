using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PlayerTrade
{
    public class ModSettings : Verse.ModSettings
    {
        public string ServerIp;
        public int ServerPort;
        public bool LoggingEnabled;
        public bool MainMenuWidgetEnabled;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref ServerIp, "server_ip");
            Scribe_Values.Look(ref ServerPort, "server_port", 35562);
            Scribe_Values.Look(ref LoggingEnabled, "logging_enabled", false);
            Scribe_Values.Look(ref MainMenuWidgetEnabled, "main_menu_widget_enabled", true);
            base.ExposeData();
        }
    }
}
