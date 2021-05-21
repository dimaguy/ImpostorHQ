using System.IO;
using Impostor.Api.Plugins;
using ImpostorHQ.Core.Config;
using ImpostorHQ.Core.Http;
using ImpostorHQ.Core.Properties;
using ImpostorHQ.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ImpostorHQ.Core
{
    public class ImpostorStartup : IPluginStartup
    {
        public void ConfigureHost(IHostBuilder host)
        {
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var configuration = CreateConfigurator();

            var cfg = configuration
                .GetSection(PrimaryConfig.Section)
                .Get<PrimaryConfig>() ?? new PrimaryConfig();

            services.AddSingleton(cfg);

            services.AddSingleton<HttpPlayerListProvider>();
            services.AddHttpServer(cfg.Host, cfg.HttpPort, Resources.html404);
            services.AddSingleton<HttpRootConfigurator>();
            services.AddMetrics();
            services.AddBlackTea();
            services.AddApi();
            services.AddStringBuilderPooling();
            services.AddCsvLogging();
            services.AddCommands();
        }

        private static IConfiguration CreateConfigurator()
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(Directory.GetCurrentDirectory());
            configurationBuilder.AddJsonFile("ImpostorHQ.json", true);
            configurationBuilder.AddEnvironmentVariables("IHQ_");
            return configurationBuilder.Build();
        }
    }
}