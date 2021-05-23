using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Impostor.Api.Events;
using Impostor.Api.Events.Managers;
using Impostor.Api.Games.Managers;
using Impostor.Api.Innersloth;
using ImpostorHQ.Core;
using ImpostorHQ.Core.Api.Message;
using ImpostorHQ.Core.Commands.Handler;
using ImpostorHQ.Core.Config;
using ImpostorHQ.Core.Extensions;
using ImpostorHQ.Core.Logs;
using ImpostorHQ.Http;
using ImpostorHQ.Http.Handler;
using ImpostorHQ.Module.Banning.Database;
using ImpostorHQ.Module.Banning.Handler;
using Microsoft.Extensions.ObjectPool;

namespace ImpostorHQ.Module.Banning
{
    public class BanManager : IEventListener
    {
        private readonly IDatabase<string, PlayerBan> _banDatabase;

        private readonly ObjectPool<StringBuilder> _sbPool;

        private readonly ILogManager _logManager;

        public BanManager(
            IDashboardCommandHandler handler,
            HttpServer httpServer, 
            IPasswordFile passwordFile,
            IDatabase<string, PlayerBan> banDatabase, 
            ObjectPool<StringBuilder> sbPool,
            IEventManager eventManager, 
            ILogManager logManager, 
            FileOperationHandler fileOperationHandler, 
            RecordOperationHandler operationHandler)
        {
            _banDatabase = banDatabase;
            _sbPool = sbPool;
            _logManager = logManager;

            logManager.LogInformation($"Ban Manager loaded {_banDatabase.Elements.Count()} bans.");

            handler.AddCommand(new DashboardCommand(operationHandler.Handle, "/ban",
                "ban database record operations. Usage: /ban add/remove/exists/info ip/name [--offline] [Player Name / IP Address] [reason]. Note that the player name and reason may be supplied in quotes (\").",
                6, 3));

            handler.AddCommand(new DashboardCommand(fileOperationHandler.Handle, "/bans",
                "ban database file operations. Usage: /bans list/purge/download", 1, 1));

            eventManager.RegisterListener(this);

            foreach (var password in passwordFile.Passwords)
            {
                httpServer.AddHandler(new DynamicHandler($"/bans.csv?{password}", GenerateHttpResponseBody));
            }
        }

        private (string mine, byte[] data) GenerateHttpResponseBody()
        {
            var sb = _sbPool.Get();

            sb.Append("IP Addr,Date,Witnesses,Names,Reason\r\n");
            foreach (var databaseBan in _banDatabase.Elements)
            {
                sb.Append(databaseBan.IpAddress).Append(',');
                sb.Append(databaseBan.Time.ToString("s")).Append(',');

                sb.Append('"');
                foreach (var databaseBanWitness in databaseBan.Witnesses)
                {
                    sb.Append(databaseBanWitness.Replace("\"", "\"\"")).Append(';');
                }
                sb.Append("\",");

                sb.Append('"');
                foreach (var playerName in databaseBan.PlayerNames)
                {
                    sb.Append(playerName.Replace("\"", "\"\"")).Append(';');
                }
                sb.Append("\",");

                sb.Append('"');
                sb.Append(databaseBan.Reason.Replace("\"", "\"\""));
                sb.Append('"');

                sb.Append("\r\n");
            }

            var result = sb.ToString();
            _sbPool.Return(sb);

            return ("text/csv", Encoding.UTF8.GetBytes(result));
        }

        [EventListener(EventPriority.Highest)]
        public async void PlayerConnection(IGamePlayerJoinedEvent @event)
        {
            var record = _banDatabase.Get(@event.Player.Client.Connection!.EndPoint.Address.ToString());
            if (!record.Equals(default(PlayerBan)))
            {
                await _logManager.LogInformation(
                    $"Ban Manager: {@event.Player.Client.Connection!.EndPoint.Address} tried to join but is banned.");
                await @event.Player.Client.DisconnectAsync(DisconnectReason.Custom, record.Reason);
            }
        }
    }
}