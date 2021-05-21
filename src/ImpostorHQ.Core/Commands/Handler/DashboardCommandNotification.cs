using ImpostorHQ.Core.Api;

namespace ImpostorHQ.Core.Commands.Handler
{
    public readonly struct DashboardCommandNotification
    {
        public WebApiUser User { get; }

        public string[] Tokens { get; }

        public DashboardCommandNotification(WebApiUser user, string[] tokens)
        {
            User = user;
            Tokens = tokens;
        }
    }
}