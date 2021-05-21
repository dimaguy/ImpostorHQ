using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using ImpostorHQ.Http.Handler;

namespace ImpostorHQ.Http
{
    public class HttpServer
    {
        private readonly byte[] _404Bytes;

        private readonly ConcurrentDictionary<string, IRequestHandler> _handlers;
        private readonly HttpListener _listener;

        public HttpServer(ushort port, IPAddress host, string notFound404Document)
        {
            _listener = new HttpListener(new IPEndPoint(host, port), HandleRequest);
            _handlers = new ConcurrentDictionary<string, IRequestHandler>();

            var notFound404Bytes = Encoding.UTF8.GetBytes(notFound404Document);
            using var ms = new MemoryStream();
            ms.Write(
                $"HTTP/1.1 404 Not Found\r\n" +
                $"Content-Length: {notFound404Document.Length}\r\n" +
                $"Content-Type: text/html\r\n" +
                $"Accept-Ranges: none\r\n" +
                $"Server: ImpostorHQ\r\n\r\n");
            ms.Write(notFound404Bytes);

            _404Bytes = ms.ToArray();
        }

        public void Start()
        {
            _listener.Start();
        }

        public void Stop()
        {
            _listener.Stop();
        }

        public void AddHandler(IRequestHandler handler)
        {
            if (!_handlers.TryAdd(handler.Path, handler))
                throw new Exception("A handler with the same path already exists.");
        }

        public bool ContainsHandler(string prefix)
        {
            return _handlers.ContainsKey(prefix);
        }

        private async void HandleRequest(HttpContext ctx)
        {
            if (_handlers.TryGetValue(ctx.Request.Path, out var handler))
            {
                await handler.HandleRequest(ctx);
                return;
            }

            try
            {
                await ctx.Transport.WriteAsync(_404Bytes);
            }
            catch
            {
                //ignored
                return;
            }
        }
    }
}