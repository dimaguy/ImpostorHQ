namespace ImpostorHQ.Http
{
    public readonly struct HttpRequest
    {
        public string Path { get; }

        public HttpRequestMethod Method { get; }

        public HttpRequest(string path, HttpRequestMethod method)
        {
            Path = path;
            Method = method;
        }
    }

    public enum HttpRequestMethod
    {
        GET,
        HEAD
    }
}