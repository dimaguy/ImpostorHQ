using System.Threading.Tasks;
using Impostor.Api.Plugins;
using ImpostorHQ.Core.Api;
using ImpostorHQ.Core.Commands.Handler;
using ImpostorHQ.Core.Extensions;
using ImpostorHQ.Core.Http;
using ImpostorHQ.Core.Logs;
using ImpostorHQ.Http;

namespace ImpostorHQ.Core
{
    [ImpostorPlugin("ihq.core")]
    public class ImpostorPlugin : PluginBase
    {
        private readonly IWebApi _api;
        private readonly HttpServer _httpServer;

        private readonly ILogManager _logs;

        public ImpostorPlugin(
            HttpRootConfigurator httpConfigurator, 
            HttpServer server,
            IWebApi api,
            ILogManager logManager, 
            IPlayerCommandHandler playerCommandHandler)
        {
            httpConfigurator.Configure();
            _httpServer = server;
            _api = api;
            _logs = logManager;
        }

        public override async ValueTask EnableAsync()
        {
            await _logs.LogInformation("Enabling...");
            _httpServer.Start();
        }

        public override async ValueTask DisableAsync()
        {
            await _logs.LogInformation("Disabling...");
            _httpServer.Stop();
        }
    }
}