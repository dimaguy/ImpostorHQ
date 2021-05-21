using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ImpostorHQ.Module.Banning
{
    /// <summary>
    /// it's a stack
    /// </summary>
    public class BanDatabase : IDisposable
    {
        private readonly SemaphoreSlim _locks;

        private readonly ConcurrentDictionary<string, PlayerBan> _bans;

        public IEnumerable<PlayerBan> Bans => _bans.Values;

        private FileStream _fs;

        private StreamWriter _writer;

        private const string Path = "ImpostorHQ.Bans.jsondb";

        public BanDatabase()
        {
            this._locks = new SemaphoreSlim(1, 1);
            this._bans = new ConcurrentDictionary<string, PlayerBan>();
            this._fs = new FileStream(Path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.Asynchronous);
            if (_fs.Length != 0)
            {
                _fs.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(_fs);
                while (_fs.Position != _fs.Length)
                {
                    var line = reader.ReadLine();
                    if(string.IsNullOrEmpty(line) || string.IsNullOrWhiteSpace(line)) continue;
                    var ban = JsonSerializer.Deserialize<PlayerBan>(line);
                    _bans.TryAdd(ban.IpAddress, ban);
                }
            }

            this._writer = new StreamWriter(_fs);
        }

        public async ValueTask Add(PlayerBan ban)
        {
            await _locks.WaitAsync();

            _bans.TryAdd(ban.IpAddress, ban);
            await _writer.WriteLineAsync(ban.Serialize());
            await _writer.FlushAsync();

            _locks.Release();
        }

        public async ValueTask Remove(string address)
        {
            await _locks.WaitAsync();

            if (!_bans.ContainsKey(address))
            {
                return;
            }

            _bans.Remove(address, out _);

            this._fs.SetLength(0);
            await this._fs.FlushAsync();
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

        public void Dispose()
        {
            _locks.Wait();
            _writer.Dispose();
        }
    }
}
