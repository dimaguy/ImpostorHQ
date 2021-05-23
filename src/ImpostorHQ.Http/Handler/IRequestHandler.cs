using System.Threading.Tasks;

namespace ImpostorHQ.Http.Handler
{
    public interface IRequestHandler
    {
        string Path { get; }

        ValueTask HandleRequest(HttpContext context);
    }
}