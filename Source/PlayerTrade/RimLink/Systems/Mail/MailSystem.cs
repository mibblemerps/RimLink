using RimLink.Util;
using RimLink.Net;
using RimLink.Net.Packets;
using RimLink.Patches;
using Verse;

namespace RimLink.Systems.Mail
{
    public class MailSystem : ISystem
    {
        public Client Client;

        public void OnConnected(Client client)
        {
            Client = client;
            client.PacketReceived += OnPacketReceived;
        }

        private void OnPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            if (e.Packet is PacketMail mailPacket)
            {
                // Mail packet received
                PresentMail(mailPacket);
            }
        }

        public static void PresentMail(PacketMail mail)
        {
            // Receive letter
            StandardLetter letter = (StandardLetter) LetterMaker.MakeLetter(DefDatabase<LetterDef>.GetNamed("PlayerMail"));
            letter.label = "Rl_Mail".Translate(mail.Title, mail.From.GuidToName());
            letter.title = mail.Title;
            letter.text = mail.Body;
            Find.LetterStack.ReceiveLetter(letter);

            if (mail.SoundDefName != null)
            {
                // Play sound
                Log.Message("Playing mail sound: " + mail.SoundDefName);
                Patch_CameraDriver_Update.PendingOneshots.Add(DefDatabase<SoundDef>.GetNamed(mail.SoundDefName));
            }
        }
        
        public void ExposeData() {}

        public void Update() {}
    }
}
