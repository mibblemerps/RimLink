using System;
using System.IO;
using System.Text;
using RimLink;
using RimLink.Core;
using RimLink.Net.Packets;
using UnityEngine;

namespace TradeServer
{
    public static class BugReportFiler
    {
        public static string BugReportDir = "bug-reports";

        public static void FileReport(Player sender, PacketBugReport report)
        {
            string name = $"BugReport" +
                          $"-{sender.Guid.Substring(0, Mathf.Min(8, sender.Guid.Length)).SanitizeFileName()}" +
                          $"-{sender.Name.Substring(0, Mathf.Min(16, sender.Name.Length)).SanitizeFileName()}" +
                          $"-";
            int i;
            for (i = 0; i < 2500; i++)
            {
                if (!File.Exists(BugReportDir + Path.DirectorySeparatorChar + name + i + ".txt"))
                    break;
            }

            name += i + ".txt";

            try
            {
                Directory.CreateDirectory(BugReportDir);

                var reportText = new StringBuilder();
                reportText.AppendLine($"*** RimLink Bug Report ***\n" +
                              $"Filed by {sender.Name} ({sender.Guid}) at {DateTime.Now}");

                // Report description
                if (!string.IsNullOrWhiteSpace(report.Description))
                    reportText.AppendLine("Description:\n" + report.Description);
                else
                    reportText.AppendLine("No description provided.");
                reportText.AppendLine();

                // Body
                reportText.AppendLine("Log:");
                reportText.AppendLine(report.Log);

                // Save
                string path = BugReportDir + Path.DirectorySeparatorChar + name;
                File.WriteAllText(path, reportText.ToString());

                Log.Message($"Bug report from {sender.Name} ({sender.Guid}) filed to: {path}");
                
            }
            catch (Exception e)
            {
                Log.Error("Error filing bug report.", e);
            }
        }
    }
}
