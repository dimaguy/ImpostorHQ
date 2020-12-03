using System;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace Impostor.Commands.Core.Client
{
    public class ImpostorHQSocket
    {
        public IPEndPoint RemoteEndPoint { get; private set; }
        public bool Connected { get; private set; }
        public ClientWebSocket Client { get; private set; }
        public BlackTeaTransport Transport { get; private set; }
        private TcpClient Socket { get; set; }

        public ImpostorHQSocket(IPEndPoint ep)
        {
            this.RemoteEndPoint = ep;
        }
        public ImpostorHQSocket(string address, ushort port)
        {
            this.RemoteEndPoint  = new IPEndPoint(IPAddress.Parse(address),port);
        }

        public ImpostorHQSocket(IPAddress address, ushort port)
        {
            this.RemoteEndPoint = new IPEndPoint(address, port);
        }
        /// <summary>
        /// Use this function in order to connect and authenticate.
        /// </summary>
        /// <param name="key">Your API key.</param>
        public async Task Connect(string key, bool useTls, Action<BaseMessage> received)
        {
            this.Client = new ClientWebSocket();
            Uri url;
            if(useTls) url = new Uri($"wss://{RemoteEndPoint.ToString()}");
            else url = new Uri($"ws://{RemoteEndPoint.ToString()}");

            await Client.ConnectAsync(url, CancellationToken.None).ConfigureAwait(false);
            this.Transport = new BlackTeaTransport(Client,key,received);
            if (await Transport.Authenticate().ConfigureAwait(false))
            {
                Connected = true;
            }
            else throw new InvalidCredentialException("Login rejected.");
        }

        public void Disconnect()
        {
            if (Connected)
            {
                this.Transport.ContinueReading = false;
                this.Client.CloseAsync(WebSocketCloseStatus.Empty,"N/A",CancellationToken.None);
                this.Socket.Dispose();
            }
        }
        /// <summary>
        /// This is used to send a raw message to the server.
        /// </summary>
        /// <param name="msg"></param>
        public void SendRaw(BaseMessage msg)
        {
            if (Connected)
            {
                Transport.SendAsync(JsonSerializer.Serialize(msg));
            }
            else
            {
                throw new SocketException((int)SocketError.NotConnected);
            }
        }
    }
}
