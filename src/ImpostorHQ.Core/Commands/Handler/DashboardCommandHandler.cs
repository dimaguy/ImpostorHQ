using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImpostorHQ.Core.Api;
using ImpostorHQ.Core.Api.Message;
using ImpostorHQ.Core.Config;
using ImpostorHQ.Core.Logs;
using ImpostorHQ.Core.Util;
using ImpostorHQ.Http;
using ImpostorHQ.Http.Handler;
using Microsoft.Extensions.ObjectPool;

namespace ImpostorHQ.Core.Commands.Handler
{
    public class DashboardCommandHandler
    {
        private readonly CommandHelpProvider _helpProvider;

        private readonly LogManager _logManager;

        private readonly MessageFactory _messageFactory;
        private readonly CommandParser<DashboardCommand> _parser;

        private readonly string[] _passwords;

        private readonly ObjectPool<StringBuilder> _sbPool;

        private readonly HttpServer _server;

        public DashboardCommandHandler(CommandParser<DashboardCommand> parser, MessageFactory messageFactory,
            CommandHelpProvider helpProvider, LogManager logManager, ObjectPool<StringBuilder> sbPool,
            HttpServer server, PasswordFile passwordFile)
        {
            _parser = parser;
            _messageFactory = messageFactory;
            _helpProvider = helpProvider;
            _logManager = logManager;
            _sbPool = sbPool;
            _server = server;
            _passwords = passwordFile.Passwords;

            _parser.Register(new DashboardCommand(HelpRequested, "/help", "shows information about the commands", 0));
            _parser.Register(new DashboardCommand(FetchLogRequested, "/fetch-log", 
                "fetches log with the name obtained from /logs. Usage: /fetch-log [name]", 1));
            _parser.Register(new DashboardCommand(ListLogsRequested, "/logs", "lists logs", 0));
            _parser.Register(new DashboardCommand(LogSizeRequested, "/logsize", "shows the space used by the logs.", 0));
        }

        private async void LogSizeRequested(DashboardCommandNotification obj)
        {
            await obj.User.WriteConsole($"Log files: {_logManager.GetLogBytes().ToSizeNotation()}", "handler");
        }

        private async void HelpRequested(DashboardCommandNotification obj)
        {
            await obj.User.Write(
                _messageFactory.CreateConsoleLog(_helpProvider.CreateHelp(_parser.Commands), "handler"));
        }

        private async void FetchLogRequested(DashboardCommandNotification obj)
        {
            if (!_logManager.EnumerateLogFiles().Contains(obj.Tokens[0]))
            {
                await obj.User.Write(_messageFactory.CreateFetchLog(null, false));
                return;
            }

            foreach (var password in _passwords)
            {
                var path = $"/logs.csv?{password}&{obj.Tokens[0]}.csv";
                if (!_server.ContainsHandler(path))
                {
                    _server.AddHandler(new StaticHandler(path, _logManager.GetFullPath(obj.Tokens[0]), "text/csv"));
                }
            }

            await obj.User.Write(_messageFactory.CreateFetchLog(obj.Tokens[0], true));
        }

        private async void ListLogsRequested(DashboardCommandNotification obj)
        {
            var sb = _sbPool.Get();
            sb.Append("Logs: \r\n");
            foreach (var log in _logManager.EnumerateLogFiles())
            {
                sb.Append($"  {log}\r\n");
            }

            var result = sb.ToString();
            _sbPool.Return(sb);

            await obj.User.Write(_messageFactory.CreateConsoleLog(result, "handler"));
        }

        public void AddCommand(DashboardCommand command)
        {
            _parser.Register(command);
        }

        public async ValueTask Process(string data, WebApiUser user)
        {
            var parseResult = _parser.TryParse(data);
            switch (parseResult.Error)
            {
                case ParseStatus.Unspecified:
                    await user.Write(_messageFactory.CreateConsoleLog("Invalid input.", "handler"));
                    return;
                case ParseStatus.UnknownCommand:
                    await user.Write(_messageFactory.CreateConsoleLog(
                        "Unknown command. Please use /help to list the commands and their usages.", "handler"));
                    return;
                case ParseStatus.NoData:
                    await user.Write(_messageFactory.CreateConsoleLog("This command requires data.", "handler"));
                    return;
                case ParseStatus.InvalidSyntax:
                    await user.Write(
                        _messageFactory.CreateConsoleLog(
                            $"Invalid syntax. Please use /help to see the usage of that command.", "handler"));
                    return;
                case ParseStatus.WhiteSpace:
                    await user.Write(
                        _messageFactory.CreateConsoleLog($"Invalid command prefix.", "handler"));
                    return;
            }

            parseResult.Command!.Call(user, parseResult.Tokens);
        }
    }
}