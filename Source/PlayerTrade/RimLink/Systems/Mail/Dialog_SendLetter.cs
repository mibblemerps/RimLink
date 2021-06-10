using System.Collections.Generic;
using System.Threading.Tasks;
using RimLink.Core;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimLink.Systems.Mail
{
    public class Dialog_SendLetter : Window
    {
        public Player Player;

        public override Vector2 InitialSize => new Vector2(512f, 512f);

        private string _title = "";
        private string _body = "";
        private Sound _sound;

        private Vector2 _bodyScrollbarPos = Vector2.zero;

        public Dialog_SendLetter(Player player)
        {
            Player = player;

            doCloseX = true;
            forcePause = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            GUI.BeginGroup(inRect);

            // Header
            Text.Font = GameFont.Medium;
            string label1 = $"Send Letter to {(Player.Name.Colorize(ColoredText.FactionColor_Neutral))}";
            Rect rect1 = new Rect(0, 0, Text.CalcSize(label1).x, 45f);
            Widgets.Label(rect1, label1);

            // Letter title
            Rect titleRect = new Rect(0, rect1.yMax, inRect.width - 180f, 32f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            string titleLabelText = "Title:";
            Vector2 titleLabelSize = Text.CalcSize(titleLabelText);
            Rect titleLabelRect = titleRect.LeftPartPixels(Mathf.RoundToInt(titleLabelSize.x + 10f));
            Widgets.Label(titleLabelRect, titleLabelText);
            _title = Widgets.TextField(titleRect.RightPartPixels(titleRect.width - titleLabelRect.width), _title);

            // Sound button
            Rect soundRect = new Rect(titleRect.xMax + 5f, titleRect.y, 160f, 32f);
            if (Widgets.ButtonText(soundRect, _sound == null ? "Sound: (none)" : $"Sound: {_sound.Label}"))
            {
                var options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption("(none)", () => { _sound = null; }));
                foreach (Sound sound in GetSounds())
                {
                    options.Add(new FloatMenuOption(sound.Label, () =>
                    {
                        _sound = sound;
                        sound.Def.PlayOneShotOnCamera();
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            
            // Letter body
            Rect bodyRect = new Rect(0f, titleRect.yMax + 10f, inRect.width, 0);
            bodyRect.yMax = inRect.yMax - 45f;
            _body = Widgets.TextAreaScrollable(bodyRect, _body, ref _bodyScrollbarPos);

            // Send button
            if (Widgets.ButtonText(inRect.BottomPartPixels(35f).RightPart(0.25f), "Send Letter"))
            {
                Close();
                SendLetter().ContinueWith((t) =>
                {
                    Messages.Message($"Letter \"{_title}\" sent to {Player.Name.Colorize(ColoredText.FactionColor_Neutral)}", MessageTypeDefOf.PositiveEvent);
                });
            }

            Text.Anchor = TextAnchor.UpperLeft;
            GUI.EndGroup();
        }

        public override void OnAcceptKeyPressed() {}

        private IEnumerable<Sound> GetSounds()
        {
            yield return new Sound(SoundDefOf.PsychicPulseGlobal, "Psychic Pulse");
            yield return new Sound(SoundDefOf.GameStartSting, "RimWorld Riff");
            yield return new Sound(SoundDefOf.Quest_Failed, "Failure");
            yield return new Sound(SoundDef.Named("TornadoSiren"), "Tornado Siren");
        }

        private async Task SendLetter()
        {
            PacketMail mail = new PacketMail
            {
                From = RimLink.Find().Guid,
                For = Player.Guid,
                Title = _title,
                Body = _body
            };

            if (_sound != null)
                mail.SoundDefName = _sound.Def.defName;
            Log.Message("Selected sound: " + mail.SoundDefName);

            RimLink.Instance.Client.SendPacket(mail);
        }

        public class Sound
        {
            public SoundDef Def;
            public string Label;

            public Sound(SoundDef def, string label)
            {
                Def = def;
                Label = label;
            }
        }
    }
}
