﻿using System;

namespace PlayerTrade.Net
{
    public class DisconnectedEventArgs : EventArgs
    {
        public DisconnectReason Reason;
        public string ReasonMessage;

        public DisconnectedEventArgs(DisconnectReason reason, string reasonMessage)
        {
            Reason = reason;
            ReasonMessage = reasonMessage;
        }
    }
}