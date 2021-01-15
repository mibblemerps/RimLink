using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace PlayerTrade.Anticheat
{
    public static class AnticheatUtil
    {
        public static void ShowEnableAnticheatDialog()
        {
            var msgBox = new Dialog_MessageBox("This server uses anticheat.\n\n" +
                                               "This will enable commitment mode and permanently disable developer mode on this save.\n\n" +
                                               "This cannot be undone.");

            msgBox.buttonAText = "Confirm";
            msgBox.buttonAAction = () =>
            {
                EnableAnticheat();
                ApplyAnticheat();
                msgBox.Close();
            };

            msgBox.buttonBText = "Quit to Main Menu";
            msgBox.buttonBAction = GenScene.GoToMainMenu;

            msgBox.forcePause = true;
            msgBox.closeOnAccept = true;
            msgBox.closeOnCancel = false;
            msgBox.closeOnClickedOutside = false;
            msgBox.doCloseX = false;

            Find.WindowStack.Add(msgBox);
        }

        public static void EnableAnticheat()
        {
            RimLinkComp.Find().Anticheat = true;

            Messages.Message("Anticheat enabled", MessageTypeDefOf.NeutralEvent, false);
        }

        public static void ApplyAnticheat()
        {
            Log.Message("Applying anticheat...");

            // Enable permadeath if it's not on
            if (!Current.Game.Info.permadeathMode)
            {
                Current.Game.Info.permadeathMode = true;
                Current.Game.Info.permadeathModeUniqueName = PermadeathModeUtility.GeneratePermadeathSaveName();

                Find.Autosaver.DoAutosave();
            }

            // Disable dev mode
            SetDevModeDisabled(true);

            Log.Message("Anticheat applied.");
        }

        public static void ShutdownAnticheat()
        {
            ResetDevModeDisabled();

            Log.Message("Anticheat shutdown");
        }

        private static void SetDevModeDisabled(bool disabled)
        {
            try
            {
                // ReSharper disable once PossibleNullReferenceException
                typeof(DevModePermanentlyDisabledUtility).GetField("disabled",
                    BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, disabled);
            }
            catch (Exception e)
            {
                Log.Error("(Anticheat) Failed to disable dev mode!", e);
            }

            if (disabled)
                Prefs.DevMode = false;
        }

        private static void ResetDevModeDisabled()
        {
            try
            {
                // By setting initialized to false - next time the game tries to check if dev mode is enabled, it'll re-read from disk
                // ReSharper disable once PossibleNullReferenceException
                typeof(DevModePermanentlyDisabledUtility).GetField("initialized",
                    BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, false);
            }
            catch (Exception e)
            {
                Log.Error("(Anticheat) Failed to reset dev mode!", e);
            }
        }
    }
}
