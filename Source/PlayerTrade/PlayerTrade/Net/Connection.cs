using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Ionic.Zlib;
using PlayerTrade.Net.Packets;
using UnityEngine;

namespace PlayerTrade.Net
{
    public abstract class Connection
    {
        private const int SendQueueMaxSize = 128;

        public event EventHandler Connected;
        public event EventHandler<DisconnectedEventArgs> Disconnected;
        public event EventHandler<PacketReceivedEventArgs> PacketReceived;

        public ConnectionState State = ConnectionState.Disconnected;

        public DateTime ConnectedAt;
        
        public TcpClient Tcp;
        public NetworkStream Stream;

        /// <summary>
        /// Artificial delay to add when sending packets. Used for testing.
        /// </summary>
        public float ArtificialSendDelay = 0;

        /// <summary>
        /// Is connected and/or authenticated?
        /// </summary>
        public bool IsConnected => State != ConnectionState.Disconnected;
        
        /// <summary>
        /// Is the connection allowed to auto-reconnect at this time?
        /// When certain disconnect types occur, an auto reconnect might not be appropriate.
        /// </summary>
        public bool AllowReconnect { get; protected set; }

        private readonly Queue<Packet> _sendQueue = new Queue<Packet>(SendQueueMaxSize);
        private TaskCompletionSource<bool> _packetQueuedCompletionSource = new TaskCompletionSource<bool>();

        /// <summary>
        /// Connect to a server as a client. Automatically resolves the given IP.
        /// </summary>
        /// <exception cref="ConnectionFailedException"></exception>
        public async Task Connect(string ip, int port = 35562)
        {
            IPAddress resolved = (await Dns.GetHostAddressesAsync(ip)).FirstOrDefault();
            if (resolved == null)
                throw new Exception("Cannot resolve hostname: " + ip);
            await Connect(new IPEndPoint(resolved, port));
        }
        
        /// <summary>
        /// Connect to a server as a client.
        /// </summary>
        /// <exception cref="ConnectionFailedException"></exception>
        public async Task Connect(IPEndPoint endpoint)
        {
            Tcp?.Close();
            Tcp = new TcpClient(endpoint.AddressFamily);

            try
            {
                Log.Message("Connecting to: " + endpoint);
                await Tcp.ConnectAsync(endpoint.Address, endpoint.Port);
            }
            catch (Exception e)
            {
                throw new ConnectionFailedException(e.Message, true, e);
            }
            
            // We now have a TCP connection
            State = ConnectionState.Connected;
            Stream = Tcp.GetStream();
            Log.Message("TCP connection established.");

            try
            {
                // Perform handshake process
                await Handshake();
            }
            catch (ConnectionFailedException connectionFailedException)
            {
                Disconnect(DisconnectReason.Kicked, connectionFailedException.Message);
                throw;
            }
            catch (Exception e)
            {
                Disconnect(DisconnectReason.Error, e.Message); // todo: this could cause rapid disconnect/reconnect loops
                throw;
            }

            // Successfully connected and completed handshake
            State = ConnectionState.Authenticated;
            AllowReconnect = true;
            ConnectedAt = DateTime.Now;
            Connected?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Adopt an existing connect as a connected client.
        /// </summary>
        /// <param name="connection">Remote connection</param>
        public async Task Serve(TcpClient connection)
        {
            Tcp?.Close();
            Tcp = connection;
            Stream = Tcp.GetStream();

            State = ConnectionState.Connected;

            try
            {
                // Perform handshake process
                await Handshake();
            }
            catch (ConnectionFailedException connectionFailedException)
            {
                Disconnect(DisconnectReason.Kicked, connectionFailedException.Message);
                throw;
            }
            catch (Exception e)
            {
                Disconnect(DisconnectReason.Error, e.Message);
                throw;
            }
            
            // Handshake successful at this point
            State = ConnectionState.Authenticated;
            AllowReconnect = true;
            Connected?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Where connection implementations can perform their custom handshaking process.
        /// Throw a <see cref="ConnectionFailedException"/> if the handshake is unsuccessful.
        /// </summary>
        /// <returns></returns>
        public abstract Task Handshake();

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

            // Wait artificial delay (testing) if set.
            if (ArtificialSendDelay > 0)
                await Task.Delay(Mathf.RoundToInt(ArtificialSendDelay * 100));

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
                            Disconnect(DisconnectReason.Error);
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
                Log.Warn($"Failed to send packet ({packet.GetType().Name})! {e}");
                if (!(packet is PacketDisconnect)) // don't trigger a disconnect if that's what we're already doing
                    Disconnect(DisconnectReason.Error);
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
                await SendPacketDirect(packet);
            }

            // Reset completion source so we know when new packets are queued
            _packetQueuedCompletionSource = new TaskCompletionSource<bool>();
        }

        /// <summary>
        /// Disconnect from server/client.
        /// </summary>
        /// <param name="reason">Reason for disconnection.</param>
        /// <param name="reasonMessage">Optional message describing disconnection.</param>
        public void Disconnect(DisconnectReason reason, string reasonMessage = null)
        {
            if (State == ConnectionState.Disconnected) return; // Already disconnected

            State = ConnectionState.Disconnected;
            
            bool sendDisconnectPacket = false;
            
            switch (reason)
            {
                case DisconnectReason.Error:
                    _sendQueue?.Clear();
                    AllowReconnect = true;
                    sendDisconnectPacket = true;
                    break;
                case DisconnectReason.Network:
                    AllowReconnect = true;
                    sendDisconnectPacket = false;
                    break;
                case DisconnectReason.User:
                    AllowReconnect = false;
                    sendDisconnectPacket = true;
                    break;
                case DisconnectReason.Kicked:
                    AllowReconnect = false;
                    sendDisconnectPacket = false;
                    break;
            }

            if (sendDisconnectPacket)
            {
                // Try to send a disconnect packet.
                // This is more a courtesy than anything, it just ensures the connection is immediately and cleanly closed on both ends.
                try
                {
                    SendPacketDirect(new PacketDisconnect()).Wait(2000);
                }
                catch (Exception) { /* ignored - disconnect packet is a courtesy */ }
            }
        
            Tcp.Close();
            
            Disconnected?.Invoke(this, new DisconnectedEventArgs(reason, reasonMessage));
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
                Disconnect(DisconnectReason.Network);
                return null;
            }

            int packetId = BitConverter.ToInt32(buffer, 0);
            int packetLength = BitConverter.ToInt32(buffer, 4);

            if (!Packet.Packets.ContainsKey(packetId))
            {
                Disconnect(DisconnectReason.Error);
                throw new Exception("Invalid packet ID: " + packetId);
            }

            // Do some sanity checking
            // We treat these as network errors since they should never happen unless the connection becomes corrupt
            if (packetLength > 3145728) // 3MiB max size
            {
                Disconnect(DisconnectReason.Network);
                throw new Exception("Packet over max size (3MiB) - possible packet overflow");
            }
            if (!Packet.Packets.ContainsKey(packetId))
            {
                Disconnect(DisconnectReason.Network);
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
                    Disconnect(DisconnectReason.Error);
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
                    Disconnect(DisconnectReason.Error);
                    Exception readException = new Exception($"Exception reading packet {packet.GetType().Name} (Last Marker = {packetBuffer.LastMarker})", e);
                    Log.Error(readException.Message, e);
                    throw readException;
                }
            }

            PacketReceived?.Invoke(this, new PacketReceivedEventArgs(packetId, packet));

            return packet;
        }
        
        public enum ConnectionState
        {
            Disconnected,
            Connected,
            Authenticated
        }
    }
}
