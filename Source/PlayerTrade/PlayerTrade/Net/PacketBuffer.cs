using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace PlayerTrade.Net
{
    /// <summary>
    /// A buffer helper for reading/writing packet data to a byte buffer. These bytes can then be sent over the wire.
    /// </summary>
    public class PacketBuffer
    {
        public static BinaryFormatter BinaryFormatter = new BinaryFormatter();

        public Stream Stream;

        /// <summary>
        /// Stream to use for buffer. If null is provided a memory stream is created.
        /// </summary>
        /// <param name="stream"></param>
        public PacketBuffer(Stream stream = null)
        {
            Stream = stream ?? new MemoryStream();
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
            Stream.Read(buffer, 0, 4);
            return BitConverter.ToInt32(buffer, 0);
        }

        public void WriteLong(long l)
        {
            Stream.Write(BitConverter.GetBytes(l), 0, 8);
        }

        public long ReadLong()
        {
            byte[] buffer = new byte[8];
            Stream.Read(buffer, 0, 8);
            return BitConverter.ToInt64(buffer, 0);
        }

        public void WriteDouble(double d)
        {
            Stream.Write(BitConverter.GetBytes(d), 0, 8);
        }

        public double ReadDouble()
        {
            byte[] buffer = new byte[8];
            Stream.Read(buffer, 0, 8);
            return BitConverter.ToDouble(buffer, 0);
        }

        public void WriteBoolean(bool b)
        {
            Stream.WriteByte(b ? (byte)1 : (byte)0);
        }

        public bool ReadBoolean()
        {
            byte[] buffer = new byte[1];
            Stream.Read(buffer, 0, 1);
            return buffer[0] == 1;
        }

        public void WriteString(string str)
        {
            if (str == null)
                throw new ArgumentException("String cannot be null", nameof(str));

            byte[] strBytes = Encoding.UTF32.GetBytes(str);

            // Write string length
            WriteInt(strBytes.Length);
            // Write string
            Stream.Write(strBytes, 0, strBytes.Length);
        }

        public string ReadString()
        {
            int length = ReadInt();
            byte[] buffer = new byte[length];
            Stream.Read(buffer, 0, length);
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
            Stream.Read(buffer, 0, buffer.Length);
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

        public void WritePacketable(IPacketable packetable)
        {
            packetable.Write(this);
        }

        public T ReadPacketable<T>() where T : IPacketable
        {
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

        #endregion
    }
}
