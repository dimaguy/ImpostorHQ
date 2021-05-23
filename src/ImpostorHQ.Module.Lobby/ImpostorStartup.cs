using System.IO;
using Impostor.Api.Plugins;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ImpostorHQ.Module.Lobby
{
    public class ImpostorStartup : IPluginStartup
    {
        public void ConfigureHost(IHostBuilder host) { }

        public void ConfigureServices(IServiceCollection services)
        {
            var configuration = CreateConfigurator();

            var cfg = configuration
                .GetSection(Config.Section)
                .Get<Config>() ?? new Config();

            services.AddSingleton(cfg);
            services.AddSingleton<LobbyCommands>();
        }

        private static IConfiguration CreateConfigurator()
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(Directory.GetCurrentDirectory());
            configurationBuilder.AddJsonFile("ImpostorHQ.Lobbies.json", true);
            configurationBuilder.AddEnvironmentVariables("IHQL_");
            return configurationBuilder.Build();
        }
    }
}
