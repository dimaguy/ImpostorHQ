using System.Collections.Generic;
using System.Linq;

namespace ImpostorHQ.Core.Api.Message
{
    public static class ApiMessageTypes
    {
        public enum MessageType
        {
            LoginApiRequest,
            LoginApiAccepted,
            LoginApiRejected,
            ConsoleLogMessage,
            ConsoleCommand,
            HeartbeatMessage,
            GameListMessage,
            DoKickOrDisconnect,
            FetchLogs
        }
        // support sloppy code written by Dimaguy

        public const string LoginApiRequest = "0";
        public const string LoginApiAccepted = "1";
        public const string LoginApiRejected = "2";
        public const string ConsoleLogMessage = "3";
        public const string ConsoleCommand = "4";
        public const string HeartbeatMessage = "5";
        public const string GameListMessage = "6";
        public const string DoKickOrDisconnect = "7";
        public const string FetchLogs = "8";

        private static readonly HashSet<string> ValidMessages =
            new HashSet<string>(Enumerable.Range(0, 9).Select(x => x.ToString()));

        public static bool IsValidMessageType(string code) => ValidMessages.Contains(code);

        public static MessageType GetEnum(string code) => (MessageType) int.Parse(code);
    }
}