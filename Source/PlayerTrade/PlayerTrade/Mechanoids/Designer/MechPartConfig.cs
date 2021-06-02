using System.Collections.Generic;
using PlayerTrade.Net;
using PlayerTrade.Net.Packets;
using UnityEngine;
using Verse;

namespace PlayerTrade.Mechanoids.Designer
{
    [StaticConstructorOnStartup]
    public class MechPartConfig : IPacketable
    {
        public static Texture2D DeleteX;

        public MechPart MechPart;

        public float DiscountPercent = 0f;

        public virtual float Price => MechPart.BasePrice * (1f - DiscountPercent);

        public virtual float CombatPower
        {
            get
            {
                switch (MechPart.Type)
                {
                    case MechPart.PartType.Building:
                        return MechPart.ThingDef.building.combatPower;
                    case MechPart.PartType.Pawn:
                        return MechPart.PawnKindDef.combatPower;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// When set to true, this part should be removed when possible. This is done instead of directly removing to prevent issues when enumerating parts.
        /// </summary>
        public bool Remove;

        public MechPartConfig()
        {
            if (RimLinkMod.Instance != null) // ensure we are running in RimWorld (Not the server)
            {
                DeleteX = ContentFinder<Texture2D>.Get("UI/Buttons/Delete", true);
            }
        }

        public virtual IEnumerable<ThingDef> GetThingDefs()
        {
            yield return MechPart.ThingDef;
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
            string label = MechPart.Type == MechPart.PartType.Pawn
                ? MechPart.PawnKindDef.LabelCap
                : MechPart.ThingDef.LabelCap;
            Widgets.Label(rect, label);
            float labelWidth = Text.CalcSize(label).x;
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

        public virtual void Configure(Thing thing) {}
        
        public virtual void PostWrite(PacketBuffer buffer) {}
        public virtual void PostRead(PacketBuffer buffer) {}
        
        public void Write(PacketBuffer buffer)
        {
            buffer.WriteInt(MechParts.Parts.IndexOf(MechPart));
            buffer.WriteFloat(DiscountPercent);
            PostWrite(buffer);
        }

        public void Read(PacketBuffer buffer)
        {
            MechPart = MechParts.Parts[buffer.ReadInt()];
            DiscountPercent = buffer.ReadFloat();
            PostRead(buffer);
        }
    }
}
