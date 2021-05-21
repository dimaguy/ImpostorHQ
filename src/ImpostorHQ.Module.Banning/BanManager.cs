using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Impostor.Api.Events;
using Impostor.Api.Events.Managers;
using Impostor.Api.Games.Managers;
using ImpostorHQ.Core;
using ImpostorHQ.Core.Api;
using ImpostorHQ.Core.Commands.Handler;
using ImpostorHQ.Core.Config;
using ImpostorHQ.Core.Logs;
using ImpostorHQ.Http;
using ImpostorHQ.Http.Handler;
using Microsoft.Extensions.ObjectPool;

namespace ImpostorHQ.Module.Banning
{
    public class BanManager : IEventListener
    {
        private readonly BanDatabase _database;

        private readonly IGameManager _gameManager;

        private readonly ObjectPool<StringBuilder> _sbPool;

        private readonly LogManager _logManager;

        public BanManager(DashboardCommandHandler handler, HttpServer httpServer, PasswordFile passwordFile, BanDatabase database, IGameManager gameManager, ObjectPool<StringBuilder> sbPool, IEventManager eventManager, LogManager logManager)
        { 
            this._database = database;
            this._gameManager = gameManager;
            this._sbPool = sbPool;
            this._logManager = logManager;

            this._logManager.LogInformation($"Ban Manager loaded {_database.Bans.Count()} bans.");

            handler.AddCommand(new DashboardCommand(OnBanRequested, "/banip",
                "bans the player with the specified IP address. Usage: /banip [address]", 1));

            handler.AddCommand(new DashboardCommand(OnBanBlindRequested, "/banipblind",
                "bans the computer with the specified IP address, whether or not they are connected. Usage: /banipblind [address] [name]", 2));

            handler.AddCommand(new DashboardCommand(OnRemoveBanRequested, "/unbanip", "removes address from bans. Usage: /unbanip [address]",
                1));

            handler.AddCommand(new DashboardCommand(BanDatabaseRequested, "/bans",
                "ban database operations. Usage: /bans list/purge", 1));

            eventManager.RegisterListener(this);

            foreach (var password in passwordFile.Passwords)
            {
                httpServer.AddHandler(new DynamicHandler($"/bans.csv?{password}", GenerateHttpResponseBody));
            }
        }

        private async void OnRemoveBanRequested(DashboardCommandNotification obj)
        {
            if (!_database.Contains(obj.Tokens[0]))
            {
                await obj.User.WriteConsole($"Unable to comply: no such record.", "ban system");
                return;
            }

            await _database.Remove(obj.Tokens[0]);
            await obj.User.WriteConsole($"Ban removed.", "ban system");
            await this._logManager.LogInformation($"Ban Manager: removed ban for {obj.Tokens[0]}");
        }

        private (string mine, byte[] data) GenerateHttpResponseBody()
        {
            var sb = _sbPool.Get();

            sb.Append("IP Address,Date,Witnesses\r\n");
            foreach (var databaseBan in _database.Bans)
            {
                sb.Append(databaseBan.IpAddress).Append(',');
                sb.Append(databaseBan.Time.ToString("s")).Append(',');
                foreach (var databaseBanWitness in databaseBan.Witnesses)
                {
                    sb.Append(databaseBanWitness).Append(';');
                }

                sb.Append("\r\n");
            }

            var result = sb.ToString();
            _sbPool.Return(sb);

            return ("text/csv", Encoding.UTF8.GetBytes(result));
        }

        private async void BanDatabaseRequested(DashboardCommandNotification obj)
        {
            switch (obj.Tokens[0])
            {
                case "list":
                    var sb = _sbPool.Get();
                    sb.Append("Legend: IP Address, Time, Witness Count\r\n");

                    foreach (var databaseBan in _database.Bans)
                    {
                        sb.Append(databaseBan.IpAddress)
                            .Append(" ")
                            .Append(databaseBan.Time).Append(" ")
                            .Append(databaseBan.Witnesses.Length)
                            .Append("\r\n");
                    }

                    await obj.User.WriteConsole(sb.ToString(), "ban system");
                    _sbPool.Return(sb);

                    await this._logManager.LogInformation($"Ban Manager: listed bans for {obj.User.Socket.ConnectionInfo.ClientIpAddress}");
                    return;
                case "purge":
                    var count = _database.Bans.Count();
                    await _database.Clear();
                    await obj.User.WriteConsole($"Purged {count} records.", "ban system");

                    await this._logManager.LogInformation($"Ban Manager: PURGED bans for {obj.User.Socket.ConnectionInfo.ClientIpAddress}");
                    return;
                default:
                    await obj.User.WriteConsole("Invalid operation.", "ban system");
                    return;
            }
        }

        private async void OnBanBlindRequested(DashboardCommandNotification obj)
        {
            var ipa = obj.Tokens[0];
            if (IsBanned(ipa))
            {
                await obj.User.WriteConsole("Unable to comply: player is already banned.", "ban system");
                return;
            }

            await _database.Add(new PlayerBan(ipa, new string[]
            {
                $"Dashboard: {obj.User.Password}"
            }, DateTime.Now, obj.Tokens[1]));

            await obj.User.WriteConsole("Computer banned.", "ban system");

            await this._logManager.LogInformation($"Ban Manager: blind banned {ipa} for {obj.User.Socket.ConnectionInfo.ClientIpAddress}");
        }

        private async void OnBanRequested(DashboardCommandNotification obj)
        {
            try
            {
                var ipa = obj.Tokens[0];
                if (IsBanned(ipa))
                {
                    await obj.User.WriteConsole("Unable to comply: player is already banned.", "ban system");
                    return;
                }

                // do a style-ish query

                var targets = _gameManager.Games.SelectMany(game => game.Players).Where(player =>
                    player.Client.Connection!.EndPoint.Address.ToString().Equals(ipa));

                var count = 0;
                var name = string.Empty;
                foreach (var clientPlayer in targets)
                {
                    name = clientPlayer.Character!.PlayerInfo.PlayerName; // messy
                    _ = clientPlayer.BanAsync();
                    count++;
                }

                if (count == 0)
                {
                    await obj.User.WriteConsole($"Unable to comply: player not found.", "ban system");
                    return;
                }

                await _database.Add(new PlayerBan(ipa, new string[]
                {
                    $"Dashboard: {obj.User.Password}"
                }, DateTime.Now, name));

                await obj.User.WriteConsole($"Banned {count} instances.", "ban system");

                await this._logManager.LogInformation($"Ban Manager: blind banned {count} instances of {ipa} for {obj.User.Socket.ConnectionInfo.ClientIpAddress}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public bool IsBanned(string ipAddress) => _database.Contains(ipAddress);

        [EventListener(EventPriority.Highest)]
        public async void PlayerConnection(IGamePlayerJoinedEvent @event)
        {
            if (IsBanned(@event.Player.Client.Connection!.EndPoint.Address.ToString()))
            {
                await this._logManager.LogInformation($"Ban Manager: {@event.Player.Client.Connection!.EndPoint.Address} tried to join but is banned.");
                await @event.Player.BanAsync();
            }
        }
    }
}
