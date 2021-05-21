using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ImpostorHQ.Http.Handler
{
    public class DynamicHandler : IRequestHandler
    {
        public string Path { get; }

        private readonly Func<(string mime, byte[] data)> _gen;

        public DynamicHandler(string endpoint, Func<(string mine, byte[] data)> generator)
        {
            if (!endpoint.StartsWith("/")) throw new ArgumentException("Endpoint must start with a forward slash (\"/\").");
            this.Path = endpoint;
            this._gen = generator;
        }

        public async ValueTask HandleRequest(HttpContext context)
        {
            var (mime, data) = _gen();
            var header = Encoding.UTF8.GetBytes(
                $"HTTP/1.1 200 OK\r\nContent-Length: {data.Length}\r\nContent-Type: {mime}\r\nAccept-Ranges: none\r\nServer: ImpostorHQ\r\n\r\n");

            await using var ms = new MemoryStream(header.Length + data.Length);
            ms.Write(header);
            ms.Write(data);

            await context.Transport.WriteAsync(ms.ToArray());
        }
    }
}
