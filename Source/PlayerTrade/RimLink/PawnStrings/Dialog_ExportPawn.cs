using RimWorld;
using UnityEngine;
using Verse;

namespace RimLink.PawnStrings
{
    public class Dialog_ExportPawn : Page
    {
        public override string PageTitle => "Export " + Pawn.NameFullColored;
        
        public override Vector2 InitialSize => new Vector2(764, 480);

        protected Pawn Pawn;

        private string _string;

        public Dialog_ExportPawn(Pawn pawn)
        {
            Pawn = pawn;
            doCloseButton = true;
            doCloseX = true;

            _string = PawnStringifyer.Export(Pawn);
        }

        public override void DoWindowContents(Rect rect)
        {
            DrawPageTitle(rect);
            Rect main = GetMainRect(rect);

            Widgets.TextArea(main, _string);
        }
    }
}