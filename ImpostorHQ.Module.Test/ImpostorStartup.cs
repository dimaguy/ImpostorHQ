using Impostor.Api.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ImpostorHQ.Module.Test
{
    public class ImpostorStartup : IPluginStartup
    {
        public void ConfigureHost(IHostBuilder host)
        {
            
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<PingPongHandler>();
        }
    }
}
