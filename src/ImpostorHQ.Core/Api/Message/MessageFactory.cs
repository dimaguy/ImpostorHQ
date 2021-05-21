using System;
using ImpostorHQ.Core.Util;

namespace ImpostorHQ.Core.Api.Message
{
    /// <summary>
    ///     Produces unencrypted messages.
    /// </summary>
    public class MessageFactory
    {
        private readonly UnixDateProvider _dateProvider;

        private readonly DateTime _startTime = DateTime.Now;

        public MessageFactory(UnixDateProvider dateProvider)
        {
            _dateProvider = dateProvider;
        }

        public string CreateLoginApiAccepted()
        {
            var message = new ApiMessage()
            {
                Date = _dateProvider.GetEpoch(),
                Type = ApiMessageTypes.LoginApiAccepted,
                Name = "welcome",
                Text = "You have successfully connected to ImpostorHQ!"
            };

            return message.Serialize();
        }

        public string CreateLoginApiRejected()
        {
            var message = new ApiMessage()
            {
                Date = _dateProvider.GetEpoch(),
                Type = ApiMessageTypes.LoginApiRejected
            };

            return message.Serialize();
        }

        public string CreateConsoleLog(string text, string source)
        {
            var message = new ApiMessage()
            {
                Date = _dateProvider.GetEpoch(),
                Name = source,
                Text = text,
                Type = ApiMessageTypes.ConsoleLogMessage
            };

            return message.Serialize();
        }

        public string CreateHeartBeat(int games, int players, int cpuUsagePercent, int memoryUsageBytes)
        {
            var message = new ApiMessage()
            {
                Date = _dateProvider.GetEpoch(),
                Flags = new float[]
                {
                    games, players, (DateTime.Now - _startTime).Minutes, cpuUsagePercent, memoryUsageBytes / 1048576
                },
                Type = ApiMessageTypes.HeartbeatMessage
            };

            return message.Serialize();
        }

        public string CreateFetchLog(string name, bool success)
        {
            var message = new ApiMessage()
            {
                Date = _dateProvider.GetEpoch(),
                Type = ApiMessageTypes.FetchLogs,
                Text = name != null ? $"{name}.csv" : null,
                Flags = new float[] {success ? 1 : 0}
            };

            return message.Serialize();
        }

        public string CreateKick(string reason, string sender)
        {
            var message = new ApiMessage()
            {
                Date = _dateProvider.GetEpoch(),
                Type = ApiMessageTypes.DoKickOrDisconnect,
                Text = reason,
                Name = sender
            };

            return message.Serialize();
        }
    }
}