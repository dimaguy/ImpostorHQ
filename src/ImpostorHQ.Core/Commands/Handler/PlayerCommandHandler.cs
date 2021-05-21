using System.Linq;
using Impostor.Api.Events;
using Impostor.Api.Events.Managers;
using Impostor.Api.Events.Player;

namespace ImpostorHQ.Core.Commands.Handler
{
    public class PlayerCommandHandler : IEventListener
    {
        private readonly CommandHelpProvider _helpProvider;
        private readonly CommandParser<PlayerCommand> _parser;

        public PlayerCommandHandler(CommandParser<PlayerCommand> parser, IEventManager eventManager,
            CommandHelpProvider helpProvider)
        {
            _parser = parser;
            _helpProvider = helpProvider;

            eventManager.RegisterListener(this);

            AddCommand(new PlayerCommand(ListCommandsRequested, "/commands", "Shows all commands.", 0));
            AddCommand(new PlayerCommand(InformationCommandRequested, "/help",
                "Shows help about the specified command. Use: /help [command]", 1));
        }

        private async void InformationCommandRequested(PlayerCommandNotification obj)
        {
            var target = _parser.Commands.FirstOrDefault(x => x.Prefix.Equals(obj.Tokens![0]));

            if (target == null)
            {
                await obj.Player.Character!.SendChatToPlayerAsync("No such command exists.");
                return;
            }

            await obj.Player.Character!.SendChatToPlayerAsync(target!.Information);
        }

        private async void ListCommandsRequested(PlayerCommandNotification notification)
        {
            await notification.Player.Character!.SendChatToPlayerAsync(_helpProvider.CreateHelp(_parser.Commands));
        }

        public void AddCommand(PlayerCommand command)
        {
            _parser.Register(command);
        }

        [EventListener]
        public async void OnChat(IPlayerChatEvent @event)
        {
            if (@event.Message[0] != '/') return;

            @event.IsCancelled = true;
            var parseResult = _parser.TryParse(@event.Message);
            switch (parseResult.Error)
            {
                case ParseStatus.Unspecified:
                    await @event.ClientPlayer.Character!.SendChatToPlayerAsync("Invalid input.");
                    return;
                case ParseStatus.UnknownCommand:
                    await @event.ClientPlayer.Character!.SendChatToPlayerAsync(
                        "Unknown command. Please use /help to list the commands and their usages.");
                    return;
                case ParseStatus.NoData:
                    await @event.ClientPlayer.Character!.SendChatToPlayerAsync(
                        $"This command requires data. Please use /help {parseResult.Command!.Prefix} to see the usage of that command.");
                    return;
                case ParseStatus.InvalidSyntax:
                    await @event.ClientPlayer.Character!.SendChatToPlayerAsync(
                        $"Invalid syntax. Please use /help {parseResult.Command!.Prefix} to see the usage of that command.");
                    return;
                case ParseStatus.WhiteSpace:
                    await @event.ClientPlayer.Character!.SendChatToPlayerAsync("Invalid command prefix.");
                    return;
            }

            parseResult.Command!.Call(@event.ClientPlayer, parseResult.Tokens);
        }
    }
}