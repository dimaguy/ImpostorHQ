using System;
using ImpostorHQ.Core.Api;

namespace ImpostorHQ.Core.Commands.Handler
{
    public class DashboardCommand : ICommand
    {
        private readonly Action<DashboardCommandNotification> _invoker;

        public DashboardCommand(Action<DashboardCommandNotification> invoker, string prefix, string information,
            int maxTokens, int minTokens)
        {
            _invoker = invoker;
            Prefix = prefix;
            Information = information;
            MaxTokens = maxTokens;
            MinTokens = minTokens;
        }

        public string Prefix { get; }

        public string Information { get; }

        public int MinTokens { get; }

        public int MaxTokens { get; }

        public void Call(WebApiUser user, string[] tokens)
        {
            _invoker!.Invoke(new DashboardCommandNotification(user, tokens));
        }
    }
}