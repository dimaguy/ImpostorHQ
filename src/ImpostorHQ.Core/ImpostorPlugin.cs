using System.Threading.Tasks;
using Impostor.Api.Plugins;
using ImpostorHQ.Core.Api;
using ImpostorHQ.Core.Commands.Handler;
using ImpostorHQ.Core.Http;
using ImpostorHQ.Core.Logs;
using ImpostorHQ.Http;

namespace ImpostorHQ.Core
{
    [ImpostorPlugin("ihq.core")]
    public class ImpostorPlugin : PluginBase
    {
        private readonly WebApi _api;
        private readonly HttpServer _httpServer;

        private readonly LogManager _logs;

        public ImpostorPlugin(HttpRootConfigurator httpConfigurator, HttpServer server, WebApi api,
            LogManager logManager, PlayerCommandHandler playerCommandHandler)
        {
            httpConfigurator.Configure();
            _httpServer = server;
            _api = api;
            _logs = logManager;
        }

        public override async ValueTask EnableAsync()
        {
            await _logs.LogInformation("Enabling...");
            _api.Start();
            await _logs.LogInformation("API Server successfully started.");
            _httpServer.Start();
            await _logs.LogInformation("HTTP Server successfully started.");
        }

        public override async ValueTask DisableAsync()
        {
            await _logs.LogInformation("Disabling...");
            await _api.Stop();
            _httpServer.Stop();
        }
    }
}