using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;

namespace PlayerTrade.Net.Packets
{
    /// <summary>
    /// A buffer helper for reading/writing packet data to a byte buffer. These bytes can then be sent over the wire.
    /// </summary>
    public class PacketBuffer
    {
        public static BinaryFormatter BinaryFormatter = new BinaryFormatter();

        public Stream Stream;

        public string LastMarker { get; private set; }

        /// <summary>
        /// Stream to use for buffer. If null is provided a memory stream is created.
        /// </summary>
        /// <param name="stream"></param>
        public PacketBuffer(Stream stream = null)
        {
            Stream = stream ?? new MemoryStream();
        }

        public void StreamRead(byte[] buffer, int offset, int count)
        {
            int bytesRead = Stream.Read(buffer, offset, count);
            if (bytesRead < count)
            {
                throw new Exception("Unexpected end of stream!");
            }
        }

        #region Read/Write Datatypes

        public void WriteByte(byte b)
        {
            Stream.WriteByte(b);
        }

        public byte ReadByte()
        {
            return (byte) Stream.ReadByte();
        }

        public void WriteInt(int i)
        {
            Stream.Write(BitConverter.GetBytes(i), 0, 4);
        }

        public int ReadInt()
        {
            byte[] buffer = new byte[4];
            StreamRead(buffer, 0, 4);
            return BitConverter.ToInt32(buffer, 0);
        }

        public void WriteLong(long l)
        {
            Stream.Write(BitConverter.GetBytes(l), 0, 8);
        }

        public long ReadLong()
        {
            byte[] buffer = new byte[8];
            StreamRead(buffer, 0, 8);
            return BitConverter.ToInt64(buffer, 0);
        }

        public void WriteDouble(double d)
        {
            Stream.Write(BitConverter.GetBytes(d), 0, 8);
        }

        public double ReadDouble()
        {
            byte[] buffer = new byte[8];
            StreamRead(buffer, 0, 8);
            return BitConverter.ToDouble(buffer, 0);
        }

        public void WriteFloat(float f)
        {
            WriteDouble(f);
        }

        public float ReadFloat()
        {
            return (float) ReadDouble();
        }

        public void WriteBoolean(bool b)
        {
            Stream.WriteByte(b ? (byte)1 : (byte)0);
        }

        public bool ReadBoolean()
        {
            byte[] buffer = new byte[1];
            StreamRead(buffer, 0, 1);
            return buffer[0] == 1;
        }

        public void WriteString(string str, bool allowNull = false)
        {
            if (str == null && !allowNull)
                throw new ArgumentException("String cannot be null", nameof(str));

            if (str == null)
            {
                // Write null boolean (false = null)
                WriteBoolean(false);
                return;
            } else if (allowNull)
            {
                // Write null boolean (true = not null)
                WriteBoolean(true);
            }

            byte[] strBytes = Encoding.UTF32.GetBytes(str);

            // Write string length
            WriteInt(strBytes.Length);
            // Write string
            Stream.Write(strBytes, 0, strBytes.Length);
        }

        public string ReadString(bool allowNull = false)
        {
            // If nulls allowed - read null check boolean first
            if (allowNull && !ReadBoolean())
                return null;

            int length = ReadInt();
            byte[] buffer = new byte[length];
            StreamRead(buffer, 0, length);
            return Encoding.UTF32.GetString(buffer);
        }

        public void WriteByteArray(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentException("Bytes cannot be null", nameof(bytes));

            // Write array length
            WriteInt(bytes.Length);
            // Write bytes
            Stream.Write(bytes, 0, bytes.Length);
        }

        public byte[] ReadByteArray()
        {
            int length = ReadInt();
            byte[] buffer = new byte[length];
            StreamRead(buffer, 0, buffer.Length);
            return buffer;
        }

        public void WriteGuid(Guid guid)
        {
            WriteByteArray(guid.ToByteArray());
        }

        public Guid ReadGuid()
        {
            return new Guid(ReadByteArray());
        }

        public void WriteColor(Color color, bool withAlpha = true)
        {
            WriteFloat(color.r);
            WriteFloat(color.g);
            WriteFloat(color.b);
            if (withAlpha)
                WriteFloat(color.a);
        }

        public Color ReadColor(bool withAlpha = true)
        {
            if (withAlpha)
                return new Color(ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat());
            else
                return new Color(ReadFloat(), ReadFloat(), ReadFloat());
        }

        public delegate object ReadListItem(PacketBuffer buffer);
        public List<T> ReadList<T>(ReadListItem readListItem)
        {
            int count = ReadInt();
            var list = new List<T>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add((T) readListItem(this));
            }

            return list;
        }

        public delegate void WriteListItem<T>(PacketBuffer buffer, T item);
        public void WriteList<T>(List<T> list, WriteListItem<T> writeListItem)
        {
            WriteInt(list.Count);
            foreach (var item in list)
            {
                writeListItem(this, item);
            }
        }

        public void WritePacketable(IPacketable packetable, bool allowNull = false)
        {
            if (!allowNull && packetable == null)
                throw new ArgumentException("Packetable cannot be null. (Set allowNull = true to allow nulls)", nameof(packetable));

            if (packetable == null)
            {
                WriteBoolean(false);
                return;
            }

            if (allowNull)
                WriteBoolean(true);
            
            packetable.Write(this);
        }

        public T ReadPacketable<T>(bool allowNull = false) where T : IPacketable
        {
            if (allowNull && !ReadBoolean())
                return default;

            T obj = Activator.CreateInstance<T>();
            obj.Read(this);
            return obj;
        }

        /// <summary>
        /// Write a serializable object using <see cref="BinaryFormatter"/>.
        /// </summary>
        /// <param name="serializable">Serializable object</param>
        public void Write(object serializable)
        {
            if (serializable == null)
                throw new ArgumentException("Object cannot be null", nameof(serializable));

            BinaryFormatter.Serialize(Stream, serializable);
        }

        /// <summary>
        /// Read a serializable object using <see cref="BinaryFormatter"/>.<br />
        /// The read object will be automatically cast to <see cref="T"/>.
        /// </summary>
        /// <typeparam name="T">Original object type</typeparam>
        /// <returns>Deserialized object</returns>
        public T Read<T>()
        {
            return (T)BinaryFormatter.Deserialize(Stream);
        }

        public void WriteMarker(string marker)
        {
            WriteString(marker);
        }

        public void ReadMarker(string marker)
        {
            if (ReadString() != marker)
                throw new Exception($"Marker \"{marker}\" lost. Packet corrupt.");
            LastMarker = marker;
        }

        #endregion
    }
}
