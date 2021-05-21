using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private readonly HttpServer _httpServer;

        private readonly WebApi _api;

        private readonly LogManager _logs;

        public ImpostorPlugin(HttpRootConfigurator httpConfigurator, HttpServer server, WebApi api, LogManager logManager, PlayerCommandHandler playerCommandHandler)
        {
            httpConfigurator.Configure();
            this._httpServer = server;
            this._api = api;
            this._logs = logManager;
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
