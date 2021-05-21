#nullable enable
using Impostor.Api.Net;

namespace ImpostorHQ.Core.Commands.Handler
{
    public readonly struct PlayerCommandNotification
    {
        public string[]? Tokens { get; }

        public IClientPlayer Player { get; }

        public PlayerCommandNotification(IClientPlayer player, string[]? tokens = null)
        {
            Tokens = tokens;
            Player = player;
        }
    }
}