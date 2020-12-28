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

        public override void ExposeData()
        {
            Scribe_Values.Look(ref ServerIp, "server_ip");
            Scribe_Values.Look(ref Username, "username", Environment.UserName, true);
            base.ExposeData();
        }
    }
}
