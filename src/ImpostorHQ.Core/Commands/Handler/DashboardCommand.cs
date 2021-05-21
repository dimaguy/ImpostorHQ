using System;
using ImpostorHQ.Core.Api;

namespace ImpostorHQ.Core.Commands.Handler
{
    public class DashboardCommand : ICommand
    {
        private readonly Action<DashboardCommandNotification> _invoker;

        public DashboardCommand(Action<DashboardCommandNotification> invoker, string prefix, string information,
            int tokens)
        {
            _invoker = invoker;
            Prefix = prefix;
            Information = information;
            Tokens = tokens;
        }

        public string Prefix { get; }

        public string Information { get; }

        public int Tokens { get; }

        public void Call(WebApiUser user, string[] tokens)
        {
            _invoker!.Invoke(new DashboardCommandNotification(user, tokens));
        }
    }
}