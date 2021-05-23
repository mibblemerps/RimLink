﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net;
using PlayerTrade.Net.Packets;

namespace PlayerTrade.Mechanoids.Designer
{
    public class MechCluster : IPacketable
    {
        public List<MechPartConfig> Parts = new List<MechPartConfig>();
        public bool HasWalls;
        public bool StartDormant = true;

        /// <summary>
        /// Total cobmat power of this mech cluster.
        /// </summary>
        public float CombatPower
        {
            get
            {
                float power = 0;
                foreach (MechPartConfig part in Parts)
                    power += part.CombatPower;
                return power;
            }
        }

        /// <summary>
        /// Total price of the mech cluster.
        /// </summary>
        public float Price
        {
            get
            {
                float price = 0;
                foreach (MechPartConfig part in Parts)
                    price += part.Price;
                return price;
            }
        }
        
        public void Write(PacketBuffer buffer)
        {
            buffer.WriteInt(Parts.Count);
            foreach (var part in Parts)
            {
                buffer.WriteString(part.GetType().FullName);
                buffer.WriteMarker("Marker_" + part.GetType().FullName);
                buffer.WritePacketable(part);
            }
            
            buffer.WriteBoolean(HasWalls);
            buffer.WriteBoolean(StartDormant);
        }

        public void Read(PacketBuffer buffer)
        {
            int partCount = buffer.ReadInt();
            Parts = new List<MechPartConfig>(partCount);
            for (int i = 0; i < partCount; i++)
            {
                Type type = Type.GetType(buffer.ReadString());
                buffer.ReadMarker("Marker_" + type.FullName);
                MechPartConfig part = (MechPartConfig) Activator.CreateInstance(type);
                part.Read(buffer);
                Parts.Add(part);
            }

            HasWalls = buffer.ReadBoolean();
            StartDormant = buffer.ReadBoolean();
        }
    }
}
