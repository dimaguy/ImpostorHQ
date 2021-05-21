using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Fleck;
using ImpostorHQ.Core.Logs.Formatters;

namespace ImpostorHQ.Core.Logs
{
    /// <summary>
    ///     Log to file.
    /// </summary>
    public class LogManager : IDisposable
    {
        private const string Dir = "ImpostorHQ.Logs";

        private static readonly byte[] Header = Encoding.UTF8.GetBytes("DATE,MESSAGE TYPE,MESSAGE,EXCEPTION\r\n");

        private readonly CancellationTokenSource _cts;

        private readonly ILogFormatter _formatter;
        private readonly FileStream _fs;

        private readonly Channel<Log> _queue;

        public LogManager(ILogFormatter formatter)
        {
            _formatter = formatter;

            var path = $"{Dir}/{DateTime.Now:MM.dd. hh.mm.ss}.csv";
            new DirectoryInfo(Dir).Create();

            _fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
            _queue = Channel.CreateBounded<Log>(100);
            _cts = new CancellationTokenSource();

            if (_fs.Length == 0)
            {
                _fs.Write(Header);
                _fs.Flush();
            }

            FleckLog.LogAction = HandleFleckLog;

            _ = WriteFile();
        }

        public void Dispose()
        {
            _cts.Cancel();
        }

        public ValueTask Enqueue(Log log) => _queue.Writer.WriteAsync(log, _cts.Token);

        private async void HandleFleckLog(LogLevel logLevel, string message, Exception ex)
        {
            if (message.StartsWith("Fleck: Sent"))
            {
                return;
            }

            await Enqueue(new Log(logLevel, string.Concat("Fleck: ", message), ex));
        }

        private async Task WriteFile()
        {
            await foreach (var log in _queue.Reader.ReadAllAsync(_cts.Token))
            {
                _formatter.Format(log, _fs);
                _fs.Flush();
            }
        }

        public IEnumerable<string> EnumerateLogFiles()
        {
            return Directory.EnumerateFiles(Dir, "*.csv", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileNameWithoutExtension);
        }

        public string GetFullPath(string name)
        {
            return $"{Dir}/{name}.csv";
        }

        public FileStream Open(string logFile)
        {
            var path = $"{Dir}/{logFile}.csv";
            var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return fs;
        }

        public long GetLogBytes()
        {
            long size = 0;
            foreach (var enumerateLogFile in EnumerateLogFiles())
            {
                size += new FileInfo(enumerateLogFile).Length;
            }

            return size;
        }
    }
}