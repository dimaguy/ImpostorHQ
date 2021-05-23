using System.IO;
using System.Net.Sockets;

namespace ImpostorHQ.Http
{
    public readonly struct HttpContext
    {
        public HttpRequest Request { get; }

        public Stream Transport { get; }

        public Socket Socket { get; }

        public HttpContext(HttpRequest request, Stream transport, Socket socket)
        {
            Request = request;
            Transport = transport;
            Socket = socket;
        }
    }
}