using Ionic.Zlib;
using PlayerTrade.Net.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PlayerTrade.Net
{
    public class Connection
    {
        private const int SendQueueMaxSize = 128;

        public event EventHandler<PacketReceivedEventArgs> PacketReceived;
        public event EventHandler Disconnected;

        public TcpClient Tcp;
        public NetworkStream Stream;

        public bool IsConnected { get; protected set; }

        private readonly Queue<Packet> _sendQueue = new Queue<Packet>(SendQueueMaxSize);
        private TaskCompletionSource<bool> _packetQueuedCompletionSource = new TaskCompletionSource<bool>();

        /// <summary>
        /// Adds a packet to the send queue to be sent shortly.
        /// </summary>
        /// <param name="packet">Packet to send</param>
        public void SendPacket(Packet packet)
        {
            if (_sendQueue.Count >= SendQueueMaxSize)
            {
                Log.Error($"Reached max packet send queue size ({SendQueueMaxSize}). Closing connection...");
                Tcp.Close();
                _sendQueue.Clear();
                return;
            }
            
            _sendQueue.Enqueue(packet);
            _packetQueuedCompletionSource.TrySetResult(true);
        }

        /// <summary>
        /// <b>Caution:</b> this is used internally to directly send packets. Please use <see cref="SendPacket"/> instead, as this uses the send queue.
        /// </summary>
        /// <param name="packet">Packet to send</param>
        public async Task SendPacketDirect(Packet packet)
        {
            var pair = Packet.Packets.FirstOrDefault(p => p.Value == packet.GetType());
            if (pair.Value == null)
                throw new Exception($"Packet {packet.GetType().FullName} isn't registered.");

            byte[] buffer;
            using (var stream = new MemoryStream())
            {
                using (var gzip = new GZipStream(stream, CompressionMode.Compress))
                {
                    var packetBuffer = new PacketBuffer(gzip);
                    try
                    {
                        // Write packet data
                        packet.Write(packetBuffer);
                    }
                    catch (Exception e)
                    {
                        Log.Error("Exception writing packet", e);
                        if (!(packet is PacketDisconnect)) // don't trigger a disconnect if that's what we're already doing
                            Disconnect();
                        throw;
                    }
                }
                buffer = stream.ToArray();
            }

            try
            {
                // Send packet ID and length
                await Stream.WriteAsync(
                    BitConverter.GetBytes(pair.Key).Concat(BitConverter.GetBytes(buffer.Length)).ToArray(), 0, 8);
                // Send packet content
                await Stream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception e)
            {
                // Failed to send packet
                Log.Warn("Failed to send packet. " + e.Message);
                if (!(packet is PacketDisconnect)) // don't trigger a disconnect if that's what we're already doing
                    Disconnect();
                throw;
            }
        }

        /// <summary>
        /// Send off all packets in the send queue sequentially.
        /// </summary>
        public async Task SendQueuedPackets()
        {
            // Wait for new packets to get queued
            await _packetQueuedCompletionSource.Task;

            // Send queued packets
            while (_sendQueue.Count > 0)
            {
                Packet packet = _sendQueue.Dequeue();
                try
                {
                    await SendPacketDirect(packet);
                }
                catch (Exception e)
                {
                    Log.Error("Exception trying to send packet from send queue", e);
                    Disconnect(false);
                }
            }

            // Reset completion source so we know when new packets are queued
            _packetQueuedCompletionSource = new TaskCompletionSource<bool>();
        }

        /// <summary>
        /// <p>Disconnect from server/client.</p>
        /// <p>By default this will send a packet to the other party indicating that we're disconnecting, and that they should close the connection.</p>
        /// <p>This also invokes the Disconnected event, allowing the disconnection to be handled by the rest of the code.</p>
        /// </summary>
        /// <param name="sendDisconnectPacket">Should the disconnect packet be sent.</param>
        public async void Disconnect(bool sendDisconnectPacket = true)
        {
            if (!IsConnected)
                return; // Already disconnected
            IsConnected = false;

            Disconnected?.Invoke(this, EventArgs.Empty);
            if (!Tcp.Connected || !sendDisconnectPacket)
                return;

            // Try to send a disconnect packet. This is more a courtesy than anything, it just ensures the connection is immediately and cleanly closed on both ends.
            try
            {
                SendPacketDirect(new PacketDisconnect()).Wait();
            } catch (Exception) {}
            Tcp?.Close();
        }

        public async Task<Packet> ReceivePacket()
        {
            int readByteCount = 0;

            byte[] buffer = new byte[8];
            try
            {
                // Read packet ID and length
                readByteCount = await Stream.ReadAsync(buffer, 0, 8);
            }
            catch (Exception) {}

            if (readByteCount == 0)
            {
                // 0 bytes read means end of stream.
                Disconnect();
                return null;
            }

            int packetId = BitConverter.ToInt32(buffer, 0);
            int packetLength = BitConverter.ToInt32(buffer, 4);

            if (!Packet.Packets.ContainsKey(packetId))
            {
                Disconnect();
                throw new Exception("Invalid packet ID: " + packetId);
            }

            // Do some sanity checking
            if (packetLength > 3145728) // 3MiB max size
            {
                Disconnect();
                throw new Exception("Packet over max size (3MiB) - possible packet overflow");
            }
            if (!Packet.Packets.ContainsKey(packetId))
            {
                Disconnect();
                throw new Exception($"Packet ID {packetId} doesn't exist");
            }

            // Read packet content
            byte[] packetContentBuffer = new byte[packetLength];
            if (packetLength > 0)
            {
                readByteCount = 0;
                try
                {
                    readByteCount = await Stream.ReadAsync(packetContentBuffer, 0, packetLength);
                }
                catch (Exception) {}
                if (readByteCount == 0)
                {
                    Disconnect();
                    throw new Exception("Unexpected end of stream reading packet content.");
                }
            }
            
            // Instantiate packet
            Packet packet = (Packet) Activator.CreateInstance(Packet.Packets[packetId]);

            if (packetLength > 0)
            {
                var packetBuffer = new PacketBuffer();
                try
                {
                    // Parse packet data
                    using (var gzip = new GZipStream(new MemoryStream(packetContentBuffer), CompressionMode.Decompress))
                    {
                        packetBuffer.Stream = gzip;
                        packet.Read(packetBuffer);
                    }
                }
                catch (Exception e)
                {
                    Disconnect();
                    throw new Exception($"Exception reading packet {packet.GetType().Name} (Last Marker = {packetBuffer.LastMarker})", e);
                }
            }

            PacketReceived?.Invoke(this, new PacketReceivedEventArgs(packetId, packet));

            return packet;
        }
    }
}
