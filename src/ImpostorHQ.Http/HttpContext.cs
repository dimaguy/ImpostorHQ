using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace ImpostorHQ.Http
{
    public readonly struct HttpContext
    {
        public HttpRequest Request { get; }

        public Stream Transport { get; }

        public Socket Socket { get; }

        public HttpContext(HttpRequest request, Stream transport, Socket socket)
        {
            this.Request = request;
            this.Transport = transport;
            this.Socket = socket;
        }
    }
}
