using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.ObjectPool;

namespace ImpostorHQ.Core.Logs.Formatters
{
    /// <summary>
    ///     Write logs in CSV format.
    /// </summary>
    public class CsvLogFormatter : ILogFormatter
    {
        private readonly ObjectPool<StringBuilder> _pool;

        public CsvLogFormatter(ObjectPool<StringBuilder> pool)
        {
            _pool = pool;
        }

        public void Format(Log log, Stream stream)
        {
            var sb = _pool.Get();

            sb.Append(DateTime.Now.ToString("G"));
            sb.Append(',');

            sb.Append(log.Type.ToString());
            sb.Append(',');

            sb.Append(log.Message.Contains(',') ? log.Message.Replace(',', ';') : log.Message);
            sb.Append(',');

            if (log.Exception == null)
            {
                sb.Append("N/A");
            }
            else
            {
                sb.Append(log.Exception);
            }

            sb.Append("\r\n");

            stream.Write(Encoding.UTF8.GetBytes(sb.ToString()));

            _pool.Return(sb);
        }
    }
}