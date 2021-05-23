using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ImpostorHQ.Http
{
    public class HttpListener
    {
        private readonly CancellationTokenSource _cts;

        private readonly Action<HttpContext> _handler;
        private readonly Socket _listener;

        public HttpListener(IPEndPoint localEp, Action<HttpContext> handler)
        {
            _handler = handler;

            _cts = new CancellationTokenSource();

            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listener.Bind(localEp);
        }

        public void Start()
        {
            _listener.Listen(int.MaxValue);
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
                if (data == null) return;

                // invalid request
                var request = HttpParser.ParseRequest(data);
                if (request == null) return;

                _handler.Invoke(new HttpContext(request.Value, ns, socket));
            }
        }
    }
}