using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerTrade.Net
{
    public class ConnectionFailedException : Exception
    {
        public readonly bool AllowReconnect;

        public ConnectionFailedException()
        {
        }

        /// <param name="message">Error message.</param>
        /// <param name="allowReconnect">Whether the client should attempt to reconnect.</param>
        /// <param name="innerException">The root cause of the failed connection.</param>
        public ConnectionFailedException(string message, bool allowReconnect = false, Exception innerException = null) : base(message, innerException)
        {
            AllowReconnect = allowReconnect;
        }
    }
}
