using System.Text;
using RimLink.Util;
using RimLink.Systems.Missions.MissionWorkers;
using Verse;

namespace RimLink.Systems.Missions
{
    public class ChoiceLetter_LaborOffer : ChoiceLetter_MissionOffer
    {
        protected LaborMissionWorker LaborWorker;

        public ChoiceLetter_LaborOffer(MissionOffer missionOffer) : base(missionOffer, false)
        {
            LaborWorker = (LaborMissionWorker) MissionOffer.MissionWorker;

            // Generate letter text
            var sb = new StringBuilder();
            sb.AppendLine($"{MissionOffer.From.GuidToName(true)} has made an offer to lend you {(MissionOffer.Colonists.Count == 1 ? "a colonist" : $"{MissionOffer.Colonists.Count} colonists")} for {MissionOffer.Days} days.\n");
            sb.AppendLine($"They are requesting {LaborWorker.Payment.ToString().Colorize(ColoredText.CurrencyColor)} silver as payment.");
            if (LaborWorker.Bond > 0)
                sb.AppendLine($"\nThey are requiring a bond of {LaborWorker.Bond.ToString().Colorize(ColoredText.CurrencyColor)} silver. This will be paid and then returned to you if you return the colonists safely.");
            sb.AppendLine();
            foreach (Pawn pawn in MissionOffer.Colonists)
                sb.AppendLine($"    {pawn.NameFullColored}, {pawn.story.TitleCap}");
            sb.AppendLine($"\nAmount payable now: {("$" + LaborWorker.TotalAmountPayable).Colorize(ColoredText.CurrencyColor)}");
            text = sb.ToString();
        }

        public ChoiceLetter_LaborOffer() {}
    }
}
