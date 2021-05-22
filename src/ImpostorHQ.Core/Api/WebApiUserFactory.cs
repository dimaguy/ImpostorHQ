using Fleck;
using ImpostorHQ.Core.Api.Message;
using ImpostorHQ.Core.Config;
using ImpostorHQ.Core.Cryptography.BlackTea;

namespace ImpostorHQ.Core.Api
{
    public class WebApiUserFactory : IWebApiUserFactory
    {
        private readonly IBlackTea _csp;

        private readonly IWebApiMessageHandler _handler;

        private readonly IMessageFactory _messageFactory;

        public WebApiUserFactory(
            IWebApiMessageHandler handler, 
            IBlackTea csp,
            IMessageFactory messageFactory)
        {
            _handler = handler;
            _csp = csp;
            _messageFactory = messageFactory;
        }

        public WebApiUser Create(IWebSocketConnection connection, Password password) =>
            new(connection, password, _csp, _handler, _messageFactory);
    }

    public interface IWebApiUserFactory
    {
        public WebApiUser Create(IWebSocketConnection connection, Password password);
    }
}