using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net;
using UnityEngine;
using Verse;

namespace PlayerTrade.Mechanoids.Designer
{
    [StaticConstructorOnStartup]
    public class MechPartConfig : IPacketable
    {
        public static readonly Texture2D DeleteX = ContentFinder<Texture2D>.Get("UI/Buttons/Delete", true);

        public MechPart MechPart;

        public virtual float Price => MechPart.BasePrice;

        /// <summary>
        /// When set to true, this part should be removed when possible. This is done instead of directly removing to prevent issues when enumerating parts.
        /// </summary>
        public bool Remove;

        public MechPartConfig()
        {
        }

        public virtual Rect Draw(Rect rect) // Assumed height of 35
        {
            // Icon
            Rect iconRect = rect.LeftPartPixels(35f);
            Widgets.DrawTextureFitted(iconRect, MechPart.Icon, 0.9f);
            rect.xMin += 40f + 45f; // leave space for quantity

            // Label
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(rect, MechPart.ThingDef.LabelCap);
            float labelWidth = Text.CalcSize(MechPart.ThingDef.LabelCap).x;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            // Price
            Rect priceRect = rect.RightPartPixels(100f);
            priceRect.xMax -= 37f; // move to left of remove button
            Text.Anchor = TextAnchor.MiddleRight;
            Text.Font = GameFont.Tiny;
            Widgets.Label(priceRect, ("$" + Mathf.RoundToInt(Price)).Colorize(Color.gray));
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            // Remove button
            Rect removeButtonRect = rect.RightPartPixels(35f);
            if (Widgets.ButtonImage(removeButtonRect, DeleteX, Color.white, GenUI.SubtleMouseoverColor))
                Remove = true;

            Rect remainingRect = rect.LeftPartPixels(rect.width - 65f); // price and remove button removed
            remainingRect.xMin = rect.x + labelWidth + 10f;
            return remainingRect;
        }

        public virtual void Write(PacketBuffer buffer)
        {
            buffer.WriteInt(Dialog_DesignMechCluster.AvailableParts.IndexOf(MechPart));
        }

        public virtual void Read(PacketBuffer buffer)
        {
            MechPart = Dialog_DesignMechCluster.AvailableParts[buffer.ReadInt()];
        }
    }
}
