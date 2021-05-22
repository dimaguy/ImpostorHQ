using System;
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
using ImpostorHQ.Module.Banning.Handler;
using Microsoft.Extensions.ObjectPool;

namespace ImpostorHQ.Module.Banning
{
    public class BanManager : IEventListener
    {
        private readonly BanDatabase _database;

        private readonly ObjectPool<StringBuilder> _sbPool;

        private readonly ILogManager _logManager;

        public BanManager(
            IDashboardCommandHandler handler,
            HttpServer httpServer, 
            IPasswordFile passwordFile,
            BanDatabase database, 
            ObjectPool<StringBuilder> sbPool,
            IEventManager eventManager, 
            ILogManager logManager, 
            FileOperationHandler fileOperationHandler, 
            RecordOperationHandler operationHandler)
        {
            _database = database;
            _sbPool = sbPool;
            _logManager = logManager;

            logManager.LogInformation($"Ban Manager loaded {_database.Bans.Count()} bans.");

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

            sb.Append("IP IpAddress,Date,Witnesses\r\n,Names,Reason");
            foreach (var databaseBan in _database.Bans)
            {
                sb.Append(databaseBan.IpAddress).Append(',');
                sb.Append(databaseBan.Time.ToString("s")).Append(',');
                foreach (var databaseBanWitness in databaseBan.Witnesses)
                {
                    sb.Append(databaseBanWitness).Append(';');
                }

                foreach (var databaseBanWitness in databaseBan.PlayerNames)
                {
                    sb.Append(databaseBanWitness).Append(';');
                }

                sb.Append(databaseBan.Reason);

                sb.Append("\r\n");
            }

            var result = sb.ToString();
            _sbPool.Return(sb);

            return ("text/csv", Encoding.UTF8.GetBytes(result));
        }

        [EventListener(EventPriority.Highest)]
        public async void PlayerConnection(IGamePlayerJoinedEvent @event)
        {
            var record = _database.GetFast(@event.Player.Client.Connection!.EndPoint.Address.ToString());
            if (record.HasValue)
            {
                await _logManager.LogInformation(
                    $"Ban Manager: {@event.Player.Client.Connection!.EndPoint.Address} tried to join but is banned.");
                await @event.Player.Client.DisconnectAsync(DisconnectReason.Custom, record.Value.Reason);
            }
        }
    }
}