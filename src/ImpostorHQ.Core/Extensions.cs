using System;
using System.Text;
using System.Threading.Tasks;
using Fleck;
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

namespace ImpostorHQ.Core
{
    public static class Extensions
    {
        public static IServiceCollection AddBlackTea(this IServiceCollection services)
        {
            services.AddSingleton<FastBitConverter>();
            services.AddSingleton<KeyGenerator>();
            services.AddSingleton<BlockManipulator>();
            services.AddSingleton<BlackTeaCryptoServiceProvider>();
            services.AddSingleton<CryptoManager>();
            return services;
        }

        public static IServiceCollection AddApi(this IServiceCollection services)
        {
            services.AddSingleton<PasswordFile>();
            services.AddSingleton<UnixDateProvider>();
            services.AddSingleton<MessageFactory>();
            services.AddSingleton<WebApiUserFactory>();
            services.AddSingleton<WebApiMessageHandler>();
            services.AddSingleton<WebApi>();
            return services;
        }

        public static IServiceCollection AddCsvLogging(this IServiceCollection services)
        {
            services.AddSingleton<ILogFormatter, CsvLogFormatter>();
            services.AddSingleton<LogManager>();
            return services;
        }

        public static IServiceCollection AddCommands(this IServiceCollection services)
        {
            services.AddTransient<CommandParser<PlayerCommand>>();
            services.AddTransient<CommandParser<DashboardCommand>>();
            services.AddSingleton<CommandHelpProvider>();
            services.AddSingleton<PlayerCommandHandler>();
            services.AddSingleton<DashboardCommandHandler>();
            return services;
        }

        public static IServiceCollection AddStringBuilderPooling(this IServiceCollection services)
        {
            return services.AddSingleton<ObjectPool<StringBuilder>>(
                new DefaultObjectPool<StringBuilder>(new StringBuilderPooledObjectPolicy()));
        }

        public static IServiceCollection AddMetrics(this IServiceCollection services)
        {
            return services.AddSingleton<MetricsProvider>();
        }

        public static ValueTask LogInformation(this LogManager manager, string message) =>
            manager.Enqueue(new Log(LogLevel.Info, message));

        public static ValueTask LogWarning(this LogManager manager, string message) =>
            manager.Enqueue(new Log(LogLevel.Warn, message));

        public static ValueTask LogError(this LogManager manager, string message, Exception ex = null) =>
            manager.Enqueue(new Log(LogLevel.Error, message, ex));
    }
}