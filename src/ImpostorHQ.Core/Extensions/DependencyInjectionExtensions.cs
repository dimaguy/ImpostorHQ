using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImpostorHQ.Core.Api;
using ImpostorHQ.Core.Api.Message;
using ImpostorHQ.Core.Commands;
using ImpostorHQ.Core.Commands.Handler;
using ImpostorHQ.Core.Config;
using ImpostorHQ.Core.Cryptography;
using ImpostorHQ.Core.Cryptography.BlackTea;
using ImpostorHQ.Core.Logs;
using ImpostorHQ.Core.Logs.Formatters;
using ImpostorHQ.Core.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace ImpostorHQ.Core.Extensions
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddBlackTea(this IServiceCollection services)
        {
            services.AddSingleton<IBitConverter, FastBitConverter>();
            services.AddSingleton<IKeyGenerator, KeyGenerator>();
            services.AddSingleton<IBlockManipulator, BlockManipulator>();
            services.AddSingleton<IBlackTea, BlackTeaCryptoServiceProvider>();
            services.AddSingleton<ICryptoManager, CryptoManager>();
            return services;
        }

        public static IServiceCollection AddApi(this IServiceCollection services)
        {
            services.AddSingleton<IPasswordFile, PasswordFile>();
            services.AddSingleton<IUnixDateProvider, UnixDateProvider>();
            services.AddSingleton<IMessageFactory, MessageFactory>();
            services.AddSingleton<IWebApiUserFactory, WebApiUserFactory>();
            services.AddSingleton<IWebApiMessageHandler, WebApiMessageHandler>();
            services.AddSingleton<IWebApi, WebApi>();
            return services;
        }

        public static IServiceCollection AddCsvLogging(this IServiceCollection services)
        {
            services.AddSingleton<ILogFormatter, CsvLogFormatter>();
            services.AddSingleton<ILogManager, LogManager>();
            return services;
        }

        public static IServiceCollection AddCommands(this IServiceCollection services)
        {
            services.AddTransient<ICommandParser<PlayerCommand>, CommandParser<PlayerCommand>>();
            services.AddTransient<ICommandParser<DashboardCommand>, CommandParser<DashboardCommand>>();
            services.AddSingleton<ICommandHelpProvider, CommandHelpProvider>();
            services.AddSingleton<IPlayerCommandHandler, PlayerCommandHandler>();
            services.AddSingleton<IDashboardCommandHandler, DashboardCommandHandler>();
            return services;
        }

        public static IServiceCollection AddStringBuilderPooling(this IServiceCollection services)
        {
            return services.AddSingleton<ObjectPool<StringBuilder>>(
                new DefaultObjectPool<StringBuilder>(new StringBuilderPooledObjectPolicy()));
        }

        public static IServiceCollection AddMetrics(this IServiceCollection services)
        {
            return services.AddSingleton<IMetricsProvider, MetricsProvider>();
        }
    }
}
