using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net;
using RimWorld;
using Verse;

namespace PlayerTrade.Labor
{
    public class ChoiceLetter_LaborOffer : ChoiceLetter
    {
        public LaborOffer LaborOffer;

        public ChoiceLetter_LaborOffer(LaborOffer laborOffer)
        {
            LaborOffer = laborOffer;

            def = DefDatabase<LetterDef>.GetNamed("LaborOffer");
            label = $"Labor Offer ({RimLinkComp.Find().Client.GetName(LaborOffer.From)})";
            text = laborOffer.GenerateOfferText();
        }

        public ChoiceLetter_LaborOffer() {}

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                var accept = new DiaOption("Accept");
                accept.action = Accept;
                accept.resolveTree = true;
                if (!LaborOffer.Fresh)
                    accept.Disable("Offer expired");
                if (!LaborOffer.CanFulfill())
                    accept.Disable("Missing resources");
                yield return accept;

                var reject = new DiaOption("RejectLetter".Translate());
                reject.resolveTree = true;
                reject.action = Reject;
                if (!LaborOffer.Fresh)
                    reject.Disable("Offer expired");
                yield return reject;

                foreach (Pawn pawn in LaborOffer.Colonists)
                {
                    var inspect = new DiaOption("View: " + pawn.NameFullColored);
                    inspect.action = () =>
                    {
                        Find.WindowStack.Add(new Dialog_InfoCard(pawn));
                    };
                    yield return inspect;
                }

                yield return Option_Postpone;
            }
        }

        private async void Accept()
        {
            if (!LaborOffer.Fresh)
                return;
            LaborOffer.Fresh = false;
            Client client = RimLinkComp.Find().Client;

            // Send acceptance
            _ = client.SendPacket(new PacketAcceptLaborOffer
            {
                For = LaborOffer.From,
                Accept = true,
                Guid = LaborOffer.Guid
            });

            // Await confirmation of deal
            Log.Message($"Awaiting confirmation of labor offer {LaborOffer.Guid}...");
            PacketConfirmLaborOffer packetConfirm = (PacketConfirmLaborOffer) await client.AwaitPacket(p =>
            {
                if (p is PacketConfirmLaborOffer pc)
                    return pc.Guid == LaborOffer.Guid;
                return false;
            });
            Log.Message($"Labor offer {LaborOffer.Guid} confirmed");

            if (packetConfirm.Confirm)
            {
                LaborOffer.FulfillAsReceiver(Find.CurrentMap);
            }
            else
            {
                Find.LetterStack.ReceiveLetter($"Labor Offer Aborted ({LaborOffer.From})",
                    "The offer was aborted.\n" +
                    "The other parties pawns were not in the same condition they were when the offer was put forward.",
                    LetterDefOf.NeutralEvent);
            }
        }

        private void Reject()
        {
            if (!LaborOffer.Fresh)
                return;
            LaborOffer.Fresh = false;

            // Send rejection
            _ = RimLinkComp.Find().Client.SendPacket(new PacketAcceptLaborOffer
            {
                For = LaborOffer.From,
                Accept = false, // reject
                Guid = LaborOffer.Guid
            });
        }
    }
}
