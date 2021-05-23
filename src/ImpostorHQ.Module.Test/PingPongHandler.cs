using ImpostorHQ.Core.Commands.Handler;

namespace ImpostorHQ.Module.Test
{
    public class PingPongHandler
    {
        public PingPongHandler(
            IDashboardCommandHandler dashboardCommandHandler,
            IPlayerCommandHandler playerCommandHandler)
        {
            dashboardCommandHandler.AddCommand(new DashboardCommand(HandlePingDashboard, "/ping", "test", 1, 0));
            playerCommandHandler.AddCommand(new PlayerCommand(HandlePingPlayer, "/ping", "test", 1, 0));
        }

        private async void HandlePingDashboard(DashboardCommandNotification obj)
        {
            if (obj.Tokens == null)
            {
                await obj.User.WriteConsole("Pong!", "test");
            }
            else
            {
                await obj.User.WriteConsole(obj.Tokens[0], "test");
            }
        }

        private async void HandlePingPlayer(PlayerCommandNotification obj)
        {
            if (obj.Tokens == null)
            {
                await obj.Player.Character!.SendChatToPlayerAsync("Pong!");
            }
            else
            {
                await obj.Player.Character!.SendChatToPlayerAsync(obj.Tokens[0]);
            }
        }

    }
}
