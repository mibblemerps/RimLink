using System.Collections.Generic;
using System.Text;
using PlayerTrade.Util;
using Verse;

namespace PlayerTrade.Missions
{
    public class ChoiceLetter_MissionOffer : ChoiceLetter
    {
        public MissionOffer MissionOffer;

        public ChoiceLetter_MissionOffer(MissionOffer missionOffer, bool generateText)
        {
            MissionOffer = missionOffer;

            def = DefDatabase<LetterDef>.GetNamed("MissionOffer");
            label = $"{MissionOffer.MissionDef.LabelCap} ({MissionOffer.From.GuidToName()})";

            if (!generateText)
                return;

            // Generate letter text
            var sb = new StringBuilder();
            sb.AppendLine($"{MissionOffer.From.GuidToName(true)} has made a offer for you to complete a {missionOffer.MissionDef.LabelCap.ToString().Colorize(ColoredText.DateTimeColor)} mission.\n" +
                          $"{MissionOffer.Colonists.Count} colonist{MissionOffer.Colonists.Count.MaybeS()} will stay at your colony for {MissionOffer.Days} day{MissionOffer.Days.MaybeS()}.");
            sb.AppendLine();
            foreach (Pawn pawn in MissionOffer.Colonists)
                sb.AppendLine($"    {pawn.NameFullColored}, {pawn.story.TitleCap}");
            text = sb.ToString();
        }

        public ChoiceLetter_MissionOffer() {}

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                var accept = new DiaOption("Accept");
                accept.action = MissionOffer.Accept;
                accept.resolveTree = true;
                if (MissionOffer == null || !MissionOffer.Fresh)
                    accept.Disable("Offer expired");
                else if (!MissionOffer.CanFulfillAsReceiver)
                    accept.Disable("Missing resources");
                yield return accept;

                var reject = new DiaOption("RejectLetter".Translate());
                reject.resolveTree = true;
                reject.action = MissionOffer.Reject;
                if (MissionOffer == null || !MissionOffer.Fresh)
                    reject.Disable("Offer expired");
                yield return reject;

                if (MissionOffer?.Colonists != null)
                {
                    foreach (Pawn pawn in MissionOffer.Colonists)
                    {
                        var inspect = new DiaOption(new Dialog_InfoCard.Hyperlink(pawn));
                        //var inspect = new DiaOption("View: " + pawn.NameFullColored);
                        //inspect.action = () => { Find.WindowStack.Add(new Dialog_InfoCard(pawn)); };
                        yield return inspect;
                    }
                }

                yield return Option_Postpone;
            }
        }
    }
}
