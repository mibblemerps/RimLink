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
        public string Username;
        public bool LoggingEnabled;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref ServerIp, "server_ip");
            Scribe_Values.Look(ref Username, "username", Environment.UserName, true);
            Scribe_Values.Look(ref LoggingEnabled, "logging_enabled", false);
            base.ExposeData();
        }
    }
}
