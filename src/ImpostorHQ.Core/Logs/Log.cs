#nullable enable
using System;
using Fleck;

namespace ImpostorHQ.Core.Logs
{
    public readonly struct Log
    {
        public LogLevel Type { get; }

        public string Message { get; }

        public Exception? Exception { get; }

        public Log(LogLevel type, string message, Exception? ex = null)
        {
            Type = type;
            Message = message;
            Exception = ex;
        }
    }
}