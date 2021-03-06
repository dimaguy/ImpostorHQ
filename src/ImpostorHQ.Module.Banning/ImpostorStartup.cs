﻿using Impostor.Api.Plugins;
using ImpostorHQ.Module.Banning.Database;
using ImpostorHQ.Module.Banning.Handler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ImpostorHQ.Module.Banning
{
    class ImpostorStartup : IPluginStartup
    {
        public void ConfigureHost(IHostBuilder host)
        {
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IDatabase<string, PlayerBan>>(new DiskDatabase<string, PlayerBan>("ImpostorHQ.Bans.jsondb"));
            services.AddSingleton<FileOperationHandler>();
            services.AddScoped<RecordOperationHandler>();
            services.AddSingleton<BanManager>();
        }
    }
}