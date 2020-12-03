using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Impostor.Commands.Core.Client
{
    public class BlackTeaTransport
    {
        public ClientWebSocket Socket { get; private set; }
        private BlackTeaSharp cryptographicFunction { get; set; }
        public string Key { get; private set; }
        public Action<BaseMessage> OnReceived { get; set; }
        private ArraySegment<byte> ReceiveBuffer { get; set; }
        private readonly object locker = new object();
        public bool ContinueReading { get; set; }
        public BlackTeaTransport(ClientWebSocket socket, string key, Action<BaseMessage> received)
        {
            this.Socket = socket;
            this.cryptographicFunction = new BlackTeaSharp();
            this.Key = key;
            this.OnReceived = received;
            this.ReceiveBuffer = new ArraySegment<byte>(new byte[1024]);
        }

        public async Task<bool> Authenticate()
        {
            string message = JsonSerializer.Serialize(new BaseMessage()
            {
                Type = MessageFlag.LoginApiRequest,
                Date = GetTime(),
                Flags = null,
                Name = null,
                Text = Key
            });
            SendAsync(message);
            var response = JsonSerializer.Deserialize<BaseMessage>(await Read().ConfigureAwait(false));
            if (response.Type.Equals(MessageFlag.LoginApiAccepted))
            {
                this.ContinueReading = true;
                Task.Run(ReadCallback).ConfigureAwait(false);
                return true;
            }
            else return false;
        }
        private async Task ReadCallback()
        {
            while (ContinueReading)
            {
                var received = cryptographicFunction.Decrypt(await Read().ConfigureAwait(false),Key);
                OnReceived?.Invoke(JsonSerializer.Deserialize<BaseMessage>(received));
            }
        }
        private async Task<string> Read()
        {
            using (var ms = new MemoryStream())
            {
                WebSocketReceiveResult result = null;
                do
                {
                    result = await Socket.ReceiveAsync(ReceiveBuffer, CancellationToken.None);
                    ms.Write(ReceiveBuffer.Array, ReceiveBuffer.Offset, result.Count);
                } while (!result.EndOfMessage);
                ms.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(ms, Encoding.UTF8)) return await reader.ReadToEndAsync().ConfigureAwait(false);
            }
        }
        public void SendAsync(string message)
        {
            Socket.SendAsync(Encoding.UTF8.GetBytes(cryptographicFunction.Encrypt(message,Key)),WebSocketMessageType.Text,true,CancellationToken.None).ConfigureAwait(false);
        }

        public static ulong GetTime()
        {
            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            return (ulong)t.TotalMilliseconds;
        }

    }
}