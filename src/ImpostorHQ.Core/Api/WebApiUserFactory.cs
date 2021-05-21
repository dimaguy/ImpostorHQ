using Fleck;
using ImpostorHQ.Core.Api.Message;
using ImpostorHQ.Core.Cryptography.BlackTea;

namespace ImpostorHQ.Core.Api
{
    public class WebApiUserFactory
    {
        private readonly BlackTeaCryptoServiceProvider _csp;

        private readonly WebApiMessageHandler _handler;

        private readonly MessageFactory _messageFactory;

        public WebApiUserFactory(WebApiMessageHandler handler, BlackTeaCryptoServiceProvider csp,
            MessageFactory messageFactory)
        {
            _handler = handler;
            _csp = csp;
            _messageFactory = messageFactory;
        }

        public WebApiUser Create(IWebSocketConnection connection, string password) =>
            new(connection, password, _csp, _handler, _messageFactory);
    }
}