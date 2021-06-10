using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimLink.PawnStrings
{
    public class Dialog_ImportPawn : Page
    {
        public override string PageTitle => "Import Pawn";

        private string _text;

        public override Vector2 InitialSize => new Vector2(764, 480);

        protected Action<Pawn> ImportedPawnAction;
        
        public Dialog_ImportPawn(Action<Pawn> importedPawnAction)
        {
            ImportedPawnAction = importedPawnAction;
            doCloseButton = false;
            doCloseX = true;
        }

        public override void DoWindowContents(Rect rect)
        {
            DrawPageTitle(rect);
            Rect main = GetMainRect(rect);

            _text = Widgets.TextArea(main, _text);

            Rect bottomButtons = rect.BottomPartPixels(38);
            
            if (Widgets.ButtonText(bottomButtons.LeftPart(0.4f), "Close"))
                Close();
            
            if (Widgets.ButtonText(bottomButtons.RightPart(0.4f), "Import"))
            {
                try
                {
                    Pawn pawn = PawnStringifyer.Import(_text);
                    if (pawn != null)
                    {
                        ImportedPawnAction(pawn);
                        Close();
                    }
                }
                catch (Exception e)
                {
                    Log.Error("Failed to import pawn!", e);
                    Find.WindowStack.Add(new Dialog_MessageBox("Rl_FailedToImportPawn".Translate(e.Message)));
                }
            }
        }
    }
}