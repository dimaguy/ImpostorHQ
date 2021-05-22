using System;
using Impostor.Api.Net;

namespace ImpostorHQ.Core.Commands.Handler
{
    public class PlayerCommand : ICommand
    {
        private readonly Action<PlayerCommandNotification> _invoker;

        public PlayerCommand(Action<PlayerCommandNotification> invoker, string prefix, string information, int maxTokens, int minTokens)
        {
            if (!prefix.StartsWith("/"))
                throw new ArgumentException("Player commands must start with a forward slash (\"/\").");
            _invoker = invoker ?? throw new Exception("Invoker cannot be null.");
            Prefix = prefix;
            Information = information;
            MaxTokens = maxTokens;
            MinTokens = minTokens;
        }

        public string Prefix { get; }

        public string Information { get; }
        public int MinTokens { get; }

        public int MaxTokens { get; }

        public void Call(IClientPlayer sender, string[] tokens)
        {
            _invoker!.Invoke(new PlayerCommandNotification(sender, tokens));
        }
    }
}