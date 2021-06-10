using RimLink.Net.Packets;
using RimLink.Util;
using Verse;

namespace RimLink.Net
{
    /// <summary>
    /// A networkable object capable of sending a IExposable object
    /// </summary>
    public class SerializedScribe<T> : IPacketable where T : IExposable
    {
        public byte[] Bytes;

        private T _cached;

        /// <summary>
        /// <p>Get or set the value stored in this serialized Scribe.</p>
        /// <p>Getting or setting invokes <see cref="Save"/>/<see cref="Load"/>. Getting is cached.</p>
        /// </summary>
        public T Value
        {
            get
            {
                if (_cached == null)
                    _cached = Load();
                return _cached;
            }
            set
            {
                Save(value);
                _cached = value;
            }
        }

        public SerializedScribe(T value)
        {
            Value = value;
        }

        public SerializedScribe(byte[] bytes)
        {
            Bytes = bytes;
        }

        public SerializedScribe()
        {
        }

        public void Save(T exposable, string rootElementName = null)
        {
            rootElementName = rootElementName ?? "RimLink";
            
            Bytes = Scriber.Save(rootElementName, () =>
            {
                Scribe_Deep.Look<T>(ref exposable, "object");
            });
        }

        public T Load()
        {
            T obj = default(T);
            Scriber.Load(Bytes, () =>
            {
                Scribe_Deep.Look(ref obj, "object");
            });
            return obj;
        }

        public void Write(PacketBuffer buffer)
        {
            buffer.WriteByteArray(Bytes);
        }

        public void Read(PacketBuffer buffer)
        {
            Bytes = buffer.ReadByteArray();
        }
    }
}