using System.IO;

namespace ImpostorHQ.Core.Logs.Formatters
{
    public interface ILogFormatter
    {
        void Format(Log log, Stream stream);
    }
}