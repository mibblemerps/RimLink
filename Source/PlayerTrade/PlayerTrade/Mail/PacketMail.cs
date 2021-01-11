using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net;

namespace PlayerTrade.Mail
{
    public class PacketMail : PacketForPlayer
    {
        public string From;
        public string Title;
        public string Body;
        public string SoundDefName;

        public override bool ShouldQueue => true;

        public override void Write(PacketBuffer buffer)
        {
            base.Write(buffer);
            buffer.WriteString(From);
            buffer.WriteString(Title);
            buffer.WriteString(Body);
            buffer.WriteString(SoundDefName, true);
        }

        public override void Read(PacketBuffer buffer)
        {
            base.Read(buffer);
            From = buffer.ReadString();
            Title = buffer.ReadString();
            Body = buffer.ReadString();
            SoundDefName = buffer.ReadString(true);
        }
    }
}
