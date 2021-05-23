using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImpostorHQ.Core;
using ImpostorHQ.Core.Api;
using ImpostorHQ.Core.Commands.Handler;
using ImpostorHQ.Core.Extensions;
using ImpostorHQ.Core.Logs;
using ImpostorHQ.Module.Banning.Database;
using Microsoft.Extensions.ObjectPool;

namespace ImpostorHQ.Module.Banning.Handler
{
    public class FileOperationHandler
    {
        private readonly ObjectPool<StringBuilder> _sbPool;

        private readonly IDatabase<string, PlayerBan> _database;

        private readonly ILogManager _logManager;

        public FileOperationHandler(ObjectPool<StringBuilder> sbPool, IDatabase<string, PlayerBan> database, ILogManager logManager)
        {
            _sbPool = sbPool;
            _database = database;
            _logManager = logManager;
        }

        public async void Handle(DashboardCommandNotification obj)
        {
            switch (obj.Tokens[0])
            {
                case "list":
                    await HandleList(obj.User);
                    return;
                case "purge":
                    await HandlePurge(obj.User);
                    return;
                case "download":
                    await HandleDownload(obj.User);
                    return;
                default:
                    await obj.User.WriteConsole("Invalid operation.", "ban system");
                    return;
            }
        }

        private async ValueTask HandleList(WebApiUser source)
        {
            var sb = _sbPool.Get();
            sb.Append("\nLegend: IP IpAddress, Time, Witness Count, Names, Reason for ban\r\n");
            foreach (var databaseBan in _database.Elements)
            {
                sb.Append(databaseBan.IpAddress)
                    .Append("; ")
                    .Append(databaseBan.Time).Append("; ")
                    .Append(databaseBan.Witnesses.Length).Append("; ")
                    .Append(string.Join(", ", databaseBan.PlayerNames.Select(name=>$"\"{name}\""))).Append("; ")
                    .Append(databaseBan.Reason)
                    .Append("\r\n");
            }

            await source.WriteConsole(sb.ToString(), "ban system");
            _sbPool.Return(sb);

            await _logManager.LogInformation(
                $"Ban Manager: listed bans for {source.Password.User}");
        }

        private async ValueTask HandlePurge(WebApiUser source)
        {
            var count = _database.Elements.Count();
            await _database.Clear();
            await source.WriteConsole($"Purged {count} records.", "ban system");

            await _logManager.LogInformation(
                $"Ban Manager: PURGED bans for {source.Password.User}");
        }

        private async ValueTask HandleDownload(WebApiUser source)
        {
            await source.WriteConsole($"Please browse to the document located at \"/bans.csv?{source.Password}\".", "ban system");
        }
    }
}
