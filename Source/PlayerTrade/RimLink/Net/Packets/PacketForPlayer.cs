namespace RimLink.Net.Packets
{
    [Packet]
    public abstract class PacketForPlayer : Packet
    {
        public string For;

        /// <summary>
        /// Should this packet be saved and queued up if the target player is offline, and be delivered to them next time they connect?
        /// </summary>
        public abstract bool ShouldQueue { get; }

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteString(For);
        }

        public override void Read(PacketBuffer buffer)
        {
            For = buffer.ReadString();
        }

        /// <summary>
        /// This is called on a new incoming packet if there is already a packet of this type queued for the same target player.<br />
        /// This gives the chance for the new incoming packet to "merge" itself with the previous packet.
        /// </summary>
        /// <param name="packet">Existing packet</param>
        /// <returns>Was merged in previous packet?</returns>
        public virtual bool MergeWithExistingPacket(PacketForPlayer packet)
        {
            return false;
        }
    }
}
