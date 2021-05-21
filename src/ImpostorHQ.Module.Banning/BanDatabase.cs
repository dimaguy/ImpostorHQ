using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ImpostorHQ.Module.Banning
{
    /// <summary>
    ///     it's a stack
    /// </summary>
    public class BanDatabase : IDisposable
    {
        private const string Path = "ImpostorHQ.Bans.jsondb";

        private readonly ConcurrentDictionary<string, PlayerBan> _bans;

        private readonly FileStream _fs;
        private readonly SemaphoreSlim _locks;

        private readonly StreamWriter _writer;

        public BanDatabase()
        {
            _locks = new SemaphoreSlim(1, 1);
            _bans = new ConcurrentDictionary<string, PlayerBan>();
            _fs = new FileStream(Path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 4096,
                FileOptions.Asynchronous);
            if (_fs.Length != 0)
            {
                var options = new JsonSerializerOptions() {PropertyNameCaseInsensitive = true};
                var reader = new StreamReader(_fs);
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    var record = JsonSerializer.Deserialize<PlayerBan>(line, options);
                    if (record.IpAddress == null)
                    {
                        // weirdness
                        continue;
                    }

                    _bans.TryAdd(record.IpAddress, record);
                }
            }

            _writer = new StreamWriter(_fs);
        }

        public IEnumerable<PlayerBan> Bans => _bans.Values;

        public void Dispose()
        {
            _locks.Wait();
            _writer.Dispose();
        }

        public async ValueTask Add(PlayerBan ban)
        {
            _bans.TryAdd(ban.IpAddress, ban);

            await _locks.WaitAsync();

            await _writer.WriteLineAsync(ban.Serialize());
            await _writer.FlushAsync();

            _locks.Release();
        }

        public async ValueTask Remove(string address)
        {
            if (!_bans.TryRemove(address, out _))
            {
                return;
            }

            await _locks.WaitAsync();

            _fs.SetLength(0);
            await _fs.FlushAsync();
            foreach (var playerBan in _bans)
            {
                await _writer.WriteLineAsync(playerBan.Value.Serialize());
                await _writer.FlushAsync();
            }

            _locks.Release();
        }

        public async ValueTask Clear()
        {
            _bans.Clear();
            await _locks.WaitAsync();

            _fs.SetLength(0);
            await _fs.FlushAsync();

            _locks.Release();
        }

        public bool Contains(string address)
        {
            return _bans.ContainsKey(address);
        }
    }
}