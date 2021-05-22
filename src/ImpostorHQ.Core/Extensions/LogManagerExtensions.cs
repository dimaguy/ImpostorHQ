using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fleck;
using ImpostorHQ.Core.Logs;

namespace ImpostorHQ.Core.Extensions
{
    public static class LogManagerExtensions
    {
        public static ValueTask LogInformation(this ILogManager manager, string message) =>
            manager.Enqueue(new Log(LogLevel.Info, message));

        public static ValueTask LogWarning(this ILogManager manager, string message) =>
            manager.Enqueue(new Log(LogLevel.Warn, message));

        public static ValueTask LogError(this ILogManager manager, string message, Exception ex = null) =>
            manager.Enqueue(new Log(LogLevel.Error, message, ex));
    }
}
