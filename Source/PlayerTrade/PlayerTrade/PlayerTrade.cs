using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using PlayerTrade.Net;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace PlayerTrade
{
    [StaticConstructorOnStartup]
    public static class PlayerTrade
    {
        static PlayerTrade()
        {
            // // Initialize harmony
            // var harmony = new Harmony("net.mitchfizz05.PlayerTrade");
            // harmony.PatchAll(Assembly.GetExecutingAssembly());
            //
            // ConnectToServer();
        }
    }
}
