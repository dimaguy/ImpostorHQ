using System;

namespace Impostor.Commands.Core.Client
{
    [Serializable]
    public static class MessageFlag
    {
        public const string LoginApiRequest = "0";      // A request to log in, with a given API key.
        public const string LoginApiAccepted = "1";     // The API Key is correct, so the login is successful.
        public const string LoginApiRejected = "2";     // The API key is incorrect, so the login is rejected.
        public const string ConsoleLogMessage = "3";    // The only working text message, so far.
        public const string ConsoleCommand = "4";       // A command sent from the dashboard to the API.
        public const string HeartbeatMessage = "5";     // Data for the graphs.
        public const string GameListMessage = "6";      // A relic of the past.
        public const string DoKickOrDisconnect = "7";   // A message when a client is kicked (not implemented) or the server shuts down.
        public const string FetchLogs = "8";            // A specialized message. This is only server sided, and will indicate that a log file exists or not.
        public const string SelectProtocol = "x9";      // A special message, not used by the dashboard.
    }
}