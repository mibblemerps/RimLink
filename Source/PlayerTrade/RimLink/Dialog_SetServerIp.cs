using UnityEngine;
using Verse;

namespace RimLink
{
    public class Dialog_SetServerIp : Window
    {
        private string _ipInput;

        public override Vector2 InitialSize => new Vector2(600f, 200f);

        public Dialog_SetServerIp()
        {
            doCloseX = true;
            doCloseButton = true;
            forcePause = true;
            closeOnAccept = false;

            _ipInput = RimLinkMod.Instance.Settings.ServerIp;
            if (RimLinkMod.Instance.Settings.ServerPort != 35562 && RimLinkMod.Instance.Settings.ServerPort > 0)
                _ipInput += ":" + RimLinkMod.Instance.Settings.ServerPort;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(0, 0, inRect.width, 50f);
            Widgets.Label(titleRect, "Set Server IP");
            Text.Font = GameFont.Small;

            Rect ipRect = new Rect(0, titleRect.yMax + 10f, inRect.width, 35f);
            Rect labelRect = ipRect.LeftPart(0.75f).LeftPartPixels(100);
            Rect ipInputRect = ipRect.LeftPart(0.75f);
            ipInputRect.xMin = labelRect.xMax + 5f;
            ipInputRect.xMax -= 5f; // add some margin for connect button

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, "Server IP");
            Text.Anchor = TextAnchor.UpperLeft;
            _ipInput = Widgets.TextField(ipInputRect, _ipInput);

            Rect connectBtnRect = ipRect.RightPart(0.25f);

            if (Widgets.ButtonText(connectBtnRect, "Save"))
                Save();
        }

        private void Save()
        {
            string ip = _ipInput;
            int port = 35562;
            if (ip.Contains(":"))
            {
                ip = _ipInput.Split(':')[0];
                int.TryParse(_ipInput.Split(':')[1], out port);
            }

            Log.Message($"Selected IP = {ip} Port = {port}");
            
            // Set values and connect
            RimLinkMod.Instance.Settings.ServerIp = ip;
            RimLinkMod.Instance.Settings.ServerPort = port;
            RimLinkMod.Instance.WriteSettings();
            if (Current.ProgramState == ProgramState.Playing && RimLink.Instance != null)
                RimLink.Instance.QueueConnect();
            Close();
        }
    }
}
