using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Impostor.Api.Games.Managers;
using ImpostorHQ.Core;
using ImpostorHQ.Core.Api;
using ImpostorHQ.Core.Commands.Handler;
using ImpostorHQ.Core.Extensions;
using ImpostorHQ.Core.Logs;
using ImpostorHQ.Module.Banning.Database;
using Microsoft.Extensions.ObjectPool;

namespace ImpostorHQ.Module.Banning.Handler
{
    public class RecordOperationHandler
    {
        private readonly IDatabase<string, PlayerBan> _database;

        private readonly ILogManager _logManager;

        private readonly IGameManager _gameManager;

        public RecordOperationHandler(IDatabase<string, PlayerBan> database, ILogManager logManager, IGameManager gameManager)
        {
            _database = database;
            _logManager = logManager;
            _gameManager = gameManager;
        }

        public async void Handle(DashboardCommandNotification obj)
        {
            var operation = obj.Tokens[0];
            var operand = obj.Tokens[1];
            var isIpAddress = false;

            switch (operand)
            {
                case "ip":
                {
                    isIpAddress = true;
                    break;
                }
                case "name":
                {
                    break;
                }
                default:
                    await obj.User.WriteConsole($"Invalid operand: \"{operand}\". Accepted ones are ip/name!", "ban handler");
                    return;
            }

            switch (operation)
            {
                case "add":
                {
                    if (obj.Tokens[2].Equals("--offline"))
                    {
                        if (obj.Tokens.Length != 6)
                        {
                            await obj.User.WriteConsole($"Unable to comply: This operation requires 6 arguments. See /help ban for more information.", "ban system");
                            return;
                        }

                        await HandleAddOfflineIp(obj.Tokens[3], obj.Tokens[4], obj.User, obj.Tokens[5]);
                        return;
                    }

                    var reason = obj.Tokens[3];
                    var target = obj.Tokens[2];
                    if (isIpAddress)
                    {
                        if (IsIpAInvalid(target))
                        {
                            await obj.User.WriteConsole($"Unable to comply: Invalid address.", "ban system");
                            return;
                        }
                        await HandleAddIp(target, obj.User, reason);
                    }
                    else await HandleAddName(target, obj.User, reason);
                    break;
                }

                case "remove":
                {
                    var target = obj.Tokens[2];
                    if (isIpAddress)
                    {
                        if (IsIpAInvalid(target))
                        {
                            await obj.User.WriteConsole($"Unable to comply: Invalid address.", "ban system");
                            return;
                        }
                        await HandleRemoveIp(target, obj.User);
                    }
                    else await obj.User.WriteConsole($"Unable to comply: You cannot remove by name!", "ban system");
                    break;
                }

                case "exists":
                {
                    var target = obj.Tokens[2];
                    if (isIpAddress)
                    {
                        if (IsIpAInvalid(target))
                        {
                            await obj.User.WriteConsole($"Unable to comply: Invalid address.", "ban system");
                            return;
                        }
                        await HandleExistsAddress(target, obj.User);
                    }
                    else await HandleExistsName(target, obj.User);
                    break;
                }

                case "info":
                {
                    var target = obj.Tokens[2];
                    if (isIpAddress)
                    {
                        if (IsIpAInvalid(target))
                        {
                            await obj.User.WriteConsole($"Unable to comply: Invalid address.", "ban system");
                            return;
                        }
                        await HandleInfoAddress(target, obj.User);
                    }
                    else await HandleInfoName(target, obj.User);
                    break;
                }

                default:
                    await obj.User.WriteConsole($"Invalid operation: \"{operation}\". Accepted ones are add/addOffline/remove/exists/info", "ban handler");
                    return;
            }
        }

        private bool IsIpAInvalid(string address)
        {
            return !IPAddress.TryParse(address, out _);
        }

        private async ValueTask HandleAddName(string target, WebApiUser user, string reason)
        {
            var targetPlayer = _gameManager.Games
                .SelectMany(game => game.Players)
                .FirstOrDefault(player => player.Character!.PlayerInfo.PlayerName.Equals(target));

            if (targetPlayer == null)
            {
                await user.WriteConsole($"Player not found.", "ban system");
                return;
            }

            var address = targetPlayer.Client.Connection!.EndPoint.Address.ToString();
            await user.WriteConsole($"Found address of player \"{target}\": {address}. Banning...", "ban system");
            await HandleAddIp(address, user, reason);
        }

        private async ValueTask HandleAddIp(string target, WebApiUser user, string reason)
        {
            var players = _gameManager.Games
                .SelectMany(game => game.Players)
                .Where(player => player.Client.Connection!.EndPoint.Address.ToString().Equals(target));

            var count = 0;
            var uniqueNames = 0;
            var name = string.Empty;
            var names = new List<string>();
            foreach (var clientPlayer in players)
            {
                if (!name.Equals(clientPlayer.Character!.PlayerInfo.PlayerName))
                {
                    uniqueNames++;
                    names.Add(clientPlayer.Character!.PlayerInfo.PlayerName);
                }
                name = clientPlayer.Character!.PlayerInfo.PlayerName;
                _ = clientPlayer.BanAsync();
                count++;
            }

            if (count == 0)
            {
                await user.WriteConsole($"Unable to comply: player not found.", "ban system");
                return;
            }

            await _database.Add(target, new PlayerBan(target, new string[]
            {
                $"Dashboard: {user.Password.User}"
            }, DateTime.Now, names.ToArray(), reason));

            await user.WriteConsole($"IP Banned {count} instances, with {uniqueNames} unique names.", "ban system");

            await _logManager.LogInformation($"Ban Manager: IP banned {count} instances of {target} with {uniqueNames} unique instance names for {user.Password.User}");
        }

        private async ValueTask HandleAddOfflineIp(string target, string name, WebApiUser user, string reason)
        {
            if (IsIpAInvalid(target))
            {
                await user.WriteConsole($"Unable to comply: Invalid target address.", "ban system");
                return;
            }

            var ban = new PlayerBan(target,
                new string[]
                {
                    $"Dashboard: {user.Password.User} [ban offline]"
                },
                DateTime.Now,
                new string[]
                {
                    name
                }, reason);

            if (!await _database.Add(ban.IpAddress, ban))
            {
                await user.WriteConsole($"Unable to comply: Ban already exists.", "ban handler");
            }
            else
            {
                await user.WriteConsole($"Ban added.", "ban handler");
            }
        }

        private async ValueTask HandleRemoveIp(string target, WebApiUser user)
        {
            if (!await _database.RemoveFast(target))
            {
                await user.WriteConsole($"Unable to comply: no such record.", "ban system");
                return;
            }

            await user.WriteConsole($"Ban removed.", "ban system");
            await _logManager.LogInformation($"Ban Manager: removed ban {target} for {user.Password.User}");
        }

        private async ValueTask HandleExistsName(string target, WebApiUser user)
        {
            var record = _database.Get(target);
            if (record.Equals(default(PlayerBan)))
            {
                await user.WriteConsole($"No such record found.", "ban handler");
            }
            else
            {
                await user.WriteConsole($"Record identified: {record.IpAddress}", "ban handler");
            }
        }

        private async ValueTask HandleExistsAddress(string target, WebApiUser user)
        {
            if (!_database.ContainsFast(target))
            {
                await user.WriteConsole($"No such record found.", "ban handler");
            }
            else
            {
                await user.WriteConsole($"Record identified.", "ban handler");
            }
        }

        private async ValueTask HandleInfoName(string target, WebApiUser user)
        {
            var record = _database.Get(target);
            if (record.Equals(default(PlayerBan)))
            {
                await user.WriteConsole($"No such record found.", "ban handler");
            }
            else
            {
                await user.WriteConsole(
                    $"\nAddress: {record.IpAddress},\n" +
                    $"Names: {string.Join(", ", record.PlayerNames.Select(name => $"\"{name}\""))},\n" + 
                    $"Date: {record.Time},\n" +
                    $"Witnesses: {string.Join(", ", record.Witnesses.Select(witness => $"\"{witness}\""))}\n",
                    "ban handler");
            }
        }

        private async ValueTask HandleInfoAddress(string target, WebApiUser user)
        {
            var record = _database.Get(target);
            if (record.Equals(default(PlayerBan)))
            {
                await user.WriteConsole($"No such record found.", "ban handler");
            }
            else
            {
                await user.WriteConsole(
                    $"\nNames: {string.Join(", ", record.PlayerNames.Select(name=>$"\"{name}\""))},\n" +
                    $"Date: {record.Time},\n" +
                    $"Witnesses: {string.Join(", ", record.Witnesses.Select(witness => $"\"{witness}\""))}\n",
                    "ban handler");
            }
        }
    }
}
