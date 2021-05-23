using System;
using Impostor.Api.Innersloth;
using ImpostorHQ.Core.Commands.Handler;

namespace ImpostorHQ.Module.Lobby
{
    public class LobbyCommands
    {
        private readonly Config _cfg;

        public LobbyCommands(IPlayerCommandHandler handler, Config cfg)
        {
            _cfg = cfg;

            handler.AddCommand(new PlayerCommand(MapInvoked, "/map", "sets the map.", 1, 1));
            handler.AddCommand(new PlayerCommand(PlayersInvoked, "/players", "sets the number of players.", 1, 1));
            handler.AddCommand(new PlayerCommand(ImpostorsInvoked, "/impostors", "sets the number of impostors.", 1, 1));
        }

        private async void MapInvoked(PlayerCommandNotification obj)
        {
            var player = obj.Player;
            var game = player.Game;

            if (!player.IsHost)
            {
                await player.Character!.SendChatToPlayerAsync(
                    "The map can only be changed by the host.");
                return;
            }

            if (obj.Player.Game.GameState != GameStates.NotStarted)
            {
                await player.Character!.SendChatToPlayerAsync(
                    "The map can only be changed before starting the game.");
                return;
            }

            if (!Enum.TryParse<MapTypes>(obj.Tokens![0], true, out var value))
            {
                await player.Character!.SendChatToPlayerAsync(
                    $"Invalid map type. Valid ones are: {string.Join(", ", Enum.GetValues<MapTypes>())}");
                return;
            }
            
            game.Options.Map = value;
            await game.SyncSettingsAsync();
            await obj.Player.Character!.SendChatToPlayerAsync("The map has been changed.");
        }

        private async void PlayersInvoked(PlayerCommandNotification obj)
        {
            var player = obj.Player;
            var game = player.Game;

            if (!player.IsHost)
            {
                await player.Character!.SendChatToPlayerAsync(
                    "The player count can only be changed by the host.");
                return;
            }

            if (obj.Player.Game.GameState != GameStates.NotStarted)
            {
                await player.Character!.SendChatToPlayerAsync(
                    "The player count can only be changed before starting the game.");
                return;
            }

            if (!byte.TryParse(obj.Tokens![0], out var players))
            {
                await player.Character!.SendChatToPlayerAsync(
                    "Invalid argument. Please provide a positive integer.");
                return;
            }

            if (players < 4)
            {
                await player.Character!.SendChatToPlayerAsync(
                    "Minimum value is 4.");
                return;
            }

            if (players > _cfg.MaxPlayers)
            {
                await player.Character!.SendChatToPlayerAsync(
                    $"Value too large. Maximum: {_cfg.MaxPlayers}");
                return;
            }

            game.Options.MaxPlayers = players;
            await game.SyncSettingsAsync();
            await obj.Player.Character!.SendChatToPlayerAsync("The player count has been changed.");

        }

        private async void ImpostorsInvoked(PlayerCommandNotification obj)
        {
            var player = obj.Player;
            var game = player.Game;

            if (!player.IsHost)
            {
                await player.Character!.SendChatToPlayerAsync(
                    "The impostor count can only be changed by the host.");
                return;
            }

            if (obj.Player.Game.GameState != GameStates.NotStarted)
            {
                await player.Character!.SendChatToPlayerAsync(
                    "The impostor count can only be changed before starting the game.");
                return;
            }

            if (!byte.TryParse(obj.Tokens![0], out var impostors))
            {
                await player.Character!.SendChatToPlayerAsync(
                    "Invalid argument. Please provide a positive integer.");
                return;
            }
            
            if (impostors < 1)
            {
                await player.Character!.SendChatToPlayerAsync(
                    "Minimum value is 1.");
                return;
            }
           
            if (impostors > _cfg.MaxImpostors)
            {
                await player.Character!.SendChatToPlayerAsync(
                    $"Value too large. Maximum: {_cfg.MaxImpostors}");
                return;
            }
            
            game.Options.NumImpostors = impostors;
            await game.SyncSettingsAsync();
            await obj.Player.Character!.SendChatToPlayerAsync("The impostor count has been changed.");
        }
    }
}
