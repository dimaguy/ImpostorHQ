using System;
using System.Collections.Generic;
using System.Text;

namespace ImpostorHQ.Http
{
    public readonly struct HttpRequest
    {
        public string Path { get; }

        public HttpRequestMethod Method { get; }

        public HttpRequest(string path, HttpRequestMethod method)
        {
            this.Path = path;
            this.Method = method;
        }
    }

    public enum HttpRequestMethod
    {
        GET, HEAD
    }
}
