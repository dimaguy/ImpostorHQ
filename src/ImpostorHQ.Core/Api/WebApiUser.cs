#nullable enable
using System;
using System.Text;
using System.Threading.Tasks;
using Fleck;
using ImpostorHQ.Core.Api.Message;
using ImpostorHQ.Core.Config;
using ImpostorHQ.Core.Cryptography.BlackTea;

namespace ImpostorHQ.Core.Api
{
    public class WebApiUser
    {
        private readonly IBlackTea _crypto;

        private readonly IWebApiMessageHandler _handler;

        private readonly IMessageFactory _messageFactory;

        private readonly byte[] _password;

        public WebApiUser(IWebSocketConnection connection,
            Password password,
            IBlackTea csp,
            IWebApiMessageHandler messageHandler,
            IMessageFactory messageFactory)
        {
            Socket = connection;
            Password = password;

            _password = Encoding.UTF8.GetBytes(password.ToString());
            _crypto = csp;
            _handler = messageHandler;
            _messageFactory = messageFactory;
        }

        public IWebSocketConnection Socket { get; }

        public Password Password { get; }

        /// <summary>
        ///     Returns a ValueTask that will show if the message was handled successfully.
        /// </summary>
        /// <param name="message"></param>
        /// <returns>False if the operation is invalid and the client must be removed.</returns>
        public ValueTask<(bool success, Exception? ex)> HandleMessage(ApiMessage message)
        {
            if (!ApiMessageTypes.IsValidMessageType(message.Type))
            {
                return new ValueTask<(bool, Exception?)>((false, null));
            }

            var type = ApiMessageTypes.GetEnum(message.Type);

            return type switch
            {
                ApiMessageTypes.MessageType.ConsoleCommand => _handler.HandleConsoleCommand(message, this),
                _ => new ValueTask<(bool, Exception?)>((false, null))
            };
        }

        public async ValueTask<bool> Write(string data, bool encrypted = true)
        {
            try
            {
                if (encrypted)
                {
                    var bytes = _crypto.EncryptRaw(Encoding.UTF8.GetBytes(data), _password);
                    await Socket.Send(Convert.ToBase64String(bytes)).ConfigureAwait(false);
                }
                else
                {
                    await Socket.Send(data).ConfigureAwait(false);
                }

                return true;
            }
            catch
            {
                Socket.Close();
                OnDisconnected?.Invoke();
                return false;
            }
        }

        public ValueTask<bool> WriteConsole(string message, string sender)
        {
            return Write(_messageFactory.CreateConsoleLog(message, sender));
        }

        public ValueTask<bool> Kick()
        {
            OnDisconnected?.Invoke();
            return Write(_messageFactory.CreateKick("kicked.", "the server"));
        }

        public event Action? OnDisconnected;
    }
}