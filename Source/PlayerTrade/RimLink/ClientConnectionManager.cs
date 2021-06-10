using System;
using System.Threading.Tasks;
using RimLink.Anticheat;
using RimLink.Net;
using RimLink.Systems;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimLink
{
    /// <summary>
    /// Handles connecting, reconnecting, etc..
    /// </summary>
    public class ClientConnectionManager
    {
        private const int MaxReconnectTime = 30;
        
        public float TimeUntilReconnect => Mathf.Max(0, _reconnectIn);
        public bool Connecting => _connecting;
        
        protected RimLink RimLink;

        protected Client Client => RimLink.Client;

        private bool _connecting;
        private float _reconnectIn = float.NaN;
        private int _failedAttempts;

        public ClientConnectionManager(RimLink rimLink)
        {
            RimLink = rimLink;
        }
        
        public async Task Connect()
        {
            if (_connecting)
            {
                Log.Warn("Attempt to connect while we're already trying to connect!");
                return;
            }

            _connecting = true;
            RimLink.Client = new Client(RimLink);
            Client.Connected += OnConnected;
            Client.PlayerConnected += RimLink.OnPlayerConnected;
            Client.PlayerUpdated += RimLink.OnPlayerUpdated;
            
            try
            {
                await Client.Connect(RimLinkMod.Instance.Settings.ServerIp, RimLinkMod.Instance.Settings.ServerPort);
                // _connecting is set back to false in OnClientConnected
            }
            catch (ConnectionFailedException e)
            {
                _connecting = false;
                throw;
            }
        }
        
        public void QueueConnect(float seconds = 0f) // todo: can we move this out of here?
        {
            if (Client?.Tcp != null && Client.Tcp.Connected)
            {
                Log.Warn("Tried to queue connect while we're already connected.");
                return;
            }

            _reconnectIn = seconds;
        }
        
        public void Update()
        {
            if (!float.IsNaN(_reconnectIn) && !_connecting)
            {
                _reconnectIn -= Time.deltaTime;

                if (_reconnectIn <= 0f)
                {
                    // Reconnect now
                    _reconnectIn = float.NaN;

                    _ = Connect().ContinueWith(t =>
                    {
                        if (t.IsFaulted && t.Exception?.InnerException != null &&
                            t.Exception.InnerException is ConnectionFailedException connectionException)
                        {
                            if (!connectionException.AllowReconnect)
                            {
                                // Cannot auto reconnect. Abort reconnecting and show connection failed dialog.
                                _reconnectIn = float.NaN;
                                ShowConnectionFailedDialog(connectionException);
                                return;
                            }
                        }
                        
                        if (t.IsFaulted)
                        {
                            // Queue next attempt. Reconnect time doubles each failed attempt, up to a defined maximum
                            _reconnectIn = Mathf.Min(Mathf.Pow(2, ++_failedAttempts), MaxReconnectTime);
                            Log.Message($"Reconnect attempt in {_reconnectIn} seconds ({_failedAttempts} failed attempts)");
                        }
                    });
                }
            }
        }
        
        public void OnConnected(object sender, EventArgs args)
        {
            Log.Message("Connected to server. GUID: " + RimLink.Guid);
            
            _connecting = false;
            _failedAttempts = 0; // reset failed attempts

            Client.Disconnected += OnDisconnected;
            
            RimLink.OnClientConnected(sender, args);
        }

        private void OnDisconnected(object sender, DisconnectedEventArgs e)
        {
            Log.Message($"Disconnect: {e.Reason}.{(e.ReasonMessage == null ? "" : $" Reason: {e.ReasonMessage}")}");
            
            RimLink.ClientOnDisconnected(sender, e);
            
            if (Client.AllowReconnect)
                QueueConnect();
        }
        
        private void ShowConnectionFailedDialog(ConnectionFailedException exception)
        {
            var connectionFailedMsgBox = new Dialog_MessageBox(exception.Message, title: "Server Connection Failed",
                buttonAText: "Quit to Main Menu", buttonAAction: GenScene.GoToMainMenu,
                buttonBText: "Close");
            connectionFailedMsgBox.forcePause = true;
            Find.WindowStack.Add(connectionFailedMsgBox);
        }
    }
}