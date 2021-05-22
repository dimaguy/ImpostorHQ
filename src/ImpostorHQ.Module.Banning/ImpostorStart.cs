using Impostor.Api.Plugins;
using ImpostorHQ.Module.Banning.Handler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ImpostorHQ.Module.Banning
{
    class ImpostorStart : IPluginStartup
    {
        public void ConfigureHost(IHostBuilder host)
        {
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<BanDatabase>();
            services.AddSingleton<FileOperationHandler>();
            services.AddScoped<RecordOperationHandler>();
            services.AddSingleton<BanManager>();
        }
    }
}