﻿using System;
using System.IO;
using PlayerTrade.Net.Packets;
using UnityEngine;

namespace PlayerTrade
{
    public static class BugReport
    {
        public static string LogFilePath => Application.consoleLogPath;

        public static void Send(string description)
        {
            Log.Message("Sending bug report...");

            try
            {
                // Read log with full file sharing so that game can continue writing to it
                string log;
                using (var reader = new StreamReader(new FileStream(LogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    log = reader.ReadToEnd();
                }

                RimLinkComp.Instance.Client.SendPacket(new PacketBugReport
                {
                    Log = log,
                    Description = description
                });
            }
            catch (Exception e)
            {
                Log.Error("Failed to send bug report!", e);
            }
        }
    }
}
