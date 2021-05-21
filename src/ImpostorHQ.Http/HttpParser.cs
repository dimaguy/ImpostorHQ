using System;
using System.Collections.Generic;
using System.Text;

namespace ImpostorHQ.Http
{
    public static class HttpParser
    {
        public static HttpRequest? ParseRequest(ReadOnlySpan<char> data)
        {
            if (data.Length < 14) return null; // it cannot be smaller than that.

            HttpRequestMethod method;

            if (data.StartsWith("GET")) method = HttpRequestMethod.GET;
            else if (data.StartsWith("HEAD")) method = HttpRequestMethod.HEAD;
            else return null; // unsupported method

            // remove "HEAD " or "GET "
            data = data.Slice(method == HttpRequestMethod.HEAD ? 5 : 4);

            var index = data.IndexOf(' ');
            if (index == -1) return null; // no version specified, invalid request.

            // select path
            data = data.Slice(0, index);
            return new HttpRequest(new string(data), method);
        }
    }
}
