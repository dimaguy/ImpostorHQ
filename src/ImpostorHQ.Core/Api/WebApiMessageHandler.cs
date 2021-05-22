#nullable enable
using System;
using System.Threading.Tasks;
using ImpostorHQ.Core.Api.Message;
using ImpostorHQ.Core.Commands.Handler;

namespace ImpostorHQ.Core.Api
{
    /// <summary>
    ///     Handles messages from the client.
    /// </summary>
    public class WebApiMessageHandler : IWebApiMessageHandler
    {
        private readonly IDashboardCommandHandler _commandHandler;

        public WebApiMessageHandler(IDashboardCommandHandler commandHandler)
        {
            _commandHandler = commandHandler;
        }

        public async ValueTask<(bool, Exception?)> HandleConsoleCommand(ApiMessage message, WebApiUser user)
        {
            try
            {
                await _commandHandler.Process(message.Text, user);
            }
            catch (Exception e)
            {
                return (false, e);
            }

            return (true, null);
        }
    }

    public interface IWebApiMessageHandler
    {
        ValueTask<(bool, Exception?)> HandleConsoleCommand(ApiMessage message, WebApiUser user);
    }
}