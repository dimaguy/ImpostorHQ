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
    public class WebApiMessageHandler
    {
        private readonly DashboardCommandHandler _commandHandler;

        public WebApiMessageHandler(DashboardCommandHandler commandHandler)
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
}