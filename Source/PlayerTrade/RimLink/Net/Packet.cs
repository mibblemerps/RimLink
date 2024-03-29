﻿using System;
using System.Collections.Generic;
using System.Reflection;
using RimLink.Net.Packets;
using RimLink.Util;
using Verse;

namespace RimLink.Net
{
    public abstract class Packet : IPacketable
    {
        public const int ConnectId = 1;
        public const int ConnectResponseId = 2;
        public const int HeartbeatId = 3;
        public const int DisconnectId = 4;

        static Packet()
        {
            AutoRegisterPackets();
        }

        public static Dictionary<int, Type> Packets = new Dictionary<int, Type>();

        public static void AutoRegisterPackets(Assembly assembly = null)
        {
            if (assembly == null)
                assembly = Assembly.GetExecutingAssembly();

            foreach (Type type in assembly.GetTypes())
            {
                PacketAttribute packetAttribute = type.GetCustomAttribute<PacketAttribute>(false);
                if (packetAttribute != null)
                {
                    int id = packetAttribute.Id;
                    if (id <= 0)
                        id = GetUnusedPacketId(type);
                    Packets.Add(id, type);
                }
            }
        }

        public static int GetUnusedPacketId(Type type = null)
        {
            // Try to derive packet ID from packet class name, otherwise start auto-assigning from ID 9999
            int i = (type == null || type.FullName == null) ? 9999 : type.FullName.GenerateStableHashCode(true) + 9999; 
            while (Packets.ContainsKey(i))
                i++;
            return i;
        }

        public PacketAttribute Attribute
        {
            get
            {
                if (_attribute == null)
                    _attribute = GetType().TryGetAttribute<PacketAttribute>();
                return _attribute;
            }
        }

        private PacketAttribute _attribute;

        public abstract void Write(PacketBuffer buffer);
        public abstract void Read(PacketBuffer buffer);
    }
}
