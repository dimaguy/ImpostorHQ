using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImpostorHQ.Http
{
    public class HttpListener
    {
        private readonly Socket _listener;

        private readonly CancellationTokenSource _cts;

        private readonly Action<HttpContext> _handler;
 
        public HttpListener(IPEndPoint localEp, Action<HttpContext> handler)
        {
            this._handler = handler;

            this._cts = new CancellationTokenSource();

            this._listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this._listener.Bind(localEp);
        }

        public void Start()
        {
            this._listener.Listen(int.MaxValue);
            _ = AcceptAsync();
        }

        public void Stop()
        {
            _cts.Cancel();
            _listener.Dispose();
        }

        private async Task AcceptAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                var socket = await _listener.AcceptAsync();
                _ = HandleSocket(socket);
            }
        }

        private async ValueTask HandleSocket(Socket socket)
        {
            using (socket)
            {
                await using var ns = new NetworkStream(socket);

                // malformed request
                var data = await ns.ReadLineSized(4096, _cts.Token);
                if(data == null) return;

                // invalid request
                var request = HttpParser.ParseRequest(data);
                if (request == null) return;

                _handler.Invoke(new HttpContext(request.Value, ns, socket));
            }
        }
    }
}
