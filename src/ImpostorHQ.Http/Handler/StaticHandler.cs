using System;
using System.IO;
using System.Threading.Tasks;

namespace ImpostorHQ.Http.Handler
{
    public class StaticHandler : IRequestHandler
    {
        private readonly byte[] _bytes;

        private readonly byte[] _header;

        public StaticHandler(string endpoint, string path, string mime)
        {
            if (!endpoint.StartsWith("/"))
                throw new ArgumentException("Endpoint must start with a forward slash (\"/\").");
            Path = endpoint;

            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _bytes = new byte[fs.Length];
            var read = 0;
            while (read != _bytes.Length)
            {
                read += fs.Read(_bytes, read, _bytes.Length);
            }

            var (full, header) = GenerateResponse(_bytes, mime);
            _bytes = full;
            _header = header;
        }

        public StaticHandler(string endpoint, byte[] data, string mime, bool @override = true)
        {
            if (!endpoint.StartsWith("/"))
                throw new ArgumentException("Endpoint must start with a forward slash (\"/\").");
            Path = endpoint;

            var possiblePath = $"ImpostorHQ.Overrides{endpoint}";

            if (File.Exists(possiblePath))
            {
                data = File.ReadAllBytes(possiblePath);
            }

            var (full, header) = GenerateResponse(data, mime);
            _bytes = full;
            _header = header;
        }

        public string Path { get; }

        public ValueTask HandleRequest(HttpContext context) =>
            context.Transport.WriteAsync(context.Request.Method == HttpRequestMethod.GET ? _bytes : _header);

        private static (byte[] full, byte[] header) GenerateResponse(byte[] content, string mime)
        {
            using var ms = new MemoryStream();

            ms.Write(
                $"HTTP/1.1 200 OK\r\nContent-Length: {content.Length}\r\nContent-Type: {mime}\r\nAccept-Ranges: none\r\nServer: ImpostorHQ\r\n\r\n");
            var header = ms.ToArray();
            ms.Write(content);

            return (ms.ToArray(), header);
        }
    }
}