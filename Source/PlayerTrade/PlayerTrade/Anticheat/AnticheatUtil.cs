using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace PlayerTrade.Anticheat
{
    public static class AnticheatUtil
    {
        public static bool IsEnabled { get; private set; }
        
        /// <summary>
        /// Letter defs that will trigger an anticheat autosave.
        /// </summary>
        public static List<LetterDef> AutosaveOnLetterDefs = new List<LetterDef>
        {
            LetterDefOf.ThreatBig,
            LetterDefOf.ThreatSmall,
            LetterDefOf.Death,
            LetterDefOf.BetrayVisitors,
        };

        private static FieldInfo TicksSinceLastSaveField =
            typeof(Autosaver).GetField("ticksSinceSave", BindingFlags.Instance | BindingFlags.NonPublic);
        
        public static void ShowEnableAnticheatDialog()
        {
            var msgBox = new Dialog_MessageBox("This server uses anticheat.\n\n" +
                                               " - Commitment mode will be enabled.\n" +
                                               " - Developer tools will be disabled on this save.\n" +
                                               " - Certain events will trigger an autosave (death, raids, etc.)\n\n" +
                                               "This is specific to this save and cannot be undone.\n" +
                                               "This does not prevent cheating using client modifications.", title: "Anticheat");

            msgBox.buttonAText = "Confirm";
            msgBox.buttonADestructive = true;
            msgBox.buttonAAction = () =>
            {
                EnableAnticheat();
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
            RimLinkComp.Instance.Anticheat = true; // this value will be saved into the save file, making this game permanently anticheat
            Messages.Message("Anticheat enabled", MessageTypeDefOf.NeutralEvent, false);

            ApplyAnticheat();
        }

        public static void ApplyAnticheat()
        {
            if (IsEnabled)
            {
                Log.Warn("Attempt to apply anticheat while it's already applied!");
                return;
            }
            
            Log.Message("Applying anticheat...");

            // Enable permadeath if it's not on
            if (!Current.Game.Info.permadeathMode)
            {
                Current.Game.Info.permadeathMode = true;
                Current.Game.Info.permadeathModeUniqueName = PermadeathModeUtility.GeneratePermadeathSaveName();

                Find.Autosaver.DoAutosave();
            }

            Application.wantsToQuit += OnApplicationWantsToQuit;

            // Disable dev mode
            SetDevModeDisabled(true);

            IsEnabled = true;
            Log.Message("Anticheat applied.");
        }

        public static void ShutdownAnticheat()
        {
            if (!IsEnabled)
            {
                Log.Warn("Attempt to shutdown anticheat when it's not enabled!");
                return;
            }
            
            ResetDevModeDisabled();
            
            Application.wantsToQuit -= OnApplicationWantsToQuit;

            IsEnabled = false;
            Log.Message("Anticheat shutdown");
        }

        /// <summary>
        /// Autosave the game for the purposes of anticheat. Won't do anything if anticheat is disabled.
        /// </summary>
        /// <param name="evenIfAnticheatDisabled">Autosave even if anticheat is disabled. Good for things where a reload would be detrimental to other players (i.e. lend colonists)</param>
        public static void AnticheatAutosave(bool evenIfAnticheatDisabled = false)
        {
            if (!IsEnabled && !evenIfAnticheatDisabled) return; // Only anticheat autosave if anticheat is enabled.

            Autosaver autosaver = Current.Game.autosaver;

            // Don't force autosave if we already did a few ticks ago
            // Good for situations that generate multiple letters (eg. bounty placed letter, then immediate raid letter)
            if ((int) TicksSinceLastSaveField.GetValue(autosaver) < 5) return;

            TicksSinceLastSaveField.SetValue(autosaver, 0);
            LongEventHandler.QueueLongEvent(autosaver.DoAutosave, "Autosaving", false, null);
        }

        private static void SetDevModeDisabled(bool disabled)
        {
            try
            {
                // ReSharper disable once PossibleNullReferenceException
                typeof(DevModePermanentlyDisabledUtility).GetField("disabled",
                    BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, disabled);

                // ReSharper disable once PossibleNullReferenceException
                typeof(DevModePermanentlyDisabledUtility).GetField("initialized",
                    BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, true);
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

        /// <summary>
        /// Intercept application quit when anticheat is enabled and trigger a save and quit to OS.<br />
        /// Note: The <see cref="Root.Shutdown()"/> method is patched so anticheat is disabled when the game is actually shutting down.
        /// </summary>
        private static bool OnApplicationWantsToQuit()
        {
            // Save and quit to OS
            LongEventHandler.QueueLongEvent(() => {
                // Save game
                GameDataSaveLoader.SaveGame(Current.Game.Info.permadeathModeUniqueName);

                // Shutdown game
                LongEventHandler.ExecuteWhenFinished(() => Root.Shutdown());
            }, "SavingLongEvent", false, null, false);

            return false;
        }

        [DebugAction("RimLink", "ApplyAnticheat", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
        private static void DebugApplyAnticheat()
        {
            ApplyAnticheat();
        }

        [DebugAction("RimLink", "EnableAnticheat", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
        private static void DebugEnableAnticheat()
        {
            if (RimLinkComp.Instance.Anticheat)
            {
                Messages.Message("Anticheat is already enabled.", MessageTypeDefOf.NeutralEvent, false);
                return;
            }

            ShowEnableAnticheatDialog();
        }
    }
}
