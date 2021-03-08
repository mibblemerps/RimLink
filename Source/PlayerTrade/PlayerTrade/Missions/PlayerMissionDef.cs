using System;
using System.Collections.Generic;
using PlayerTrade.Missions.MissionWorkers;
using RimWorld;
using Verse;

namespace PlayerTrade.Missions
{
    public class PlayerMissionDef : Def
    {
        public TaggedString LabelCap
        {
            get
            {
                if (label.NullOrEmpty())
                    return null;
                if (_cachedLabelCap.NullOrEmpty())
                    _cachedLabelCap = label.CapitalizeFirst();
                return _cachedLabelCap;
            }
        }

        public string label;
        public string workerClass = "PlayerTrade.Missions.MissionWorkers.MissionWorker";
        public string questScriptDefName;
        public string configDialogClass = "PlayerTrade.Missions.ConfigDialogs.BasicConfigDialog";

        public FloatRange days = new FloatRange(0.1f, 9999f); // 4 years
        public IntRange colonists = new IntRange(1, 1);

        public string allReturnedLetterTitle = "All Colonists Returned ({player})";
        public string allReturnedLetterBody = "The {mission} has concluded. {player} has returned all your {colonists}.";

        public string notReturnedLetterTitle = "Colonists Not Returned ({player})";
        public string notReturnedLetterBody = "The {mission} has concluded and {player} hasn't returned all your colonists!\n\nThey will attempt to escape and maybe one day they'll come home.";

        public string returnedEarlyLetterTitle = "{colonist_name_short} Returned Early ({player})";
        public string returnedEarlyLetterBody = "{colonist_name_long} has been returned from {player} early.";

        private TaggedString _cachedLabelCap;

        public QuestScriptDef QuestScriptDef => DefDatabase<RimWorld.QuestScriptDef>.GetNamed(questScriptDefName);

        public MissionWorkers.MissionWorker CreateWorker(MissionOffer offer)
        {
            MissionWorkers.MissionWorker worker = (MissionWorkers.MissionWorker) Activator.CreateInstance(Type.GetType(workerClass));
            worker.Offer = offer;
            return worker;
        }

        public bool ShowConfigDialog(Player player, IEnumerable<Pawn> selectedPawns)
        {
            if (configDialogClass == null)
                return false;

            Find.WindowStack.Add((Window)Activator.CreateInstance(Type.GetType(configDialogClass), this, player, selectedPawns));
            return true;
        }
    }
}
