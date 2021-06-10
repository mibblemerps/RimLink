using RimLink.Systems.SettingSync;
using UnityEngine;
using Verse;

namespace RimLink.MainTab
{
    public class TabAdmin : ITab
    {
        public void Draw(Rect mainRect)
        {
            Widgets.DrawMenuSection(mainRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(mainRect.LeftHalf().ContractedBy(10f));
            if (listing.ButtonText("Change Server Storyteller"))
            {
                Find.WindowStack.Add(new Dialog_SelectServerStoryteller());
            }
            listing.End();
        }

        public void Update()
        {
            
        }
    }
}