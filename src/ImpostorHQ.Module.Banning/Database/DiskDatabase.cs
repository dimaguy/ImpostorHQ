#nullable enable
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ImpostorHQ.Module.Banning.Database
{
    public class DiskDatabase<TKey, TValue> : IDatabase<TKey, TValue> where TValue : notnull where TKey : notnull
    {
        public string FilePath { get; }

        private readonly ConcurrentDictionary<TKey, TValue> _dictionary;

        private readonly FileStream _fs;
        private readonly SemaphoreSlim _locks;

        private readonly StreamWriter _writer;

        public ICollection<TValue> Elements => _dictionary.Values;

        public DiskDatabase(string path)
        {
            FilePath = path;
            _locks = new SemaphoreSlim(1, 1);
            _dictionary = new ConcurrentDictionary<TKey, TValue>();
            _fs = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 1024*1024, FileOptions.Asynchronous);
            if (_fs.Length != 0)
            {
                var reader = new StreamReader(_fs);
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    var record = JsonSerializer.Deserialize<KeyValuePair<TKey, TValue>>(line);
                    _dictionary.TryAdd(record.Key, record.Value);
                }
            }

            _writer = new StreamWriter(_fs);
        }

        public async ValueTask<bool> Add(TKey key, TValue value)
        {
            if (!_dictionary.TryAdd(key, value))
            {
                return false;
            }

            await _locks.WaitAsync();

            await _writer.WriteLineAsync(JsonSerializer.Serialize(new KeyValuePair<TKey,TValue>(key, value)));
            await _writer.FlushAsync();

            _locks.Release();

            return true;
        }

        public async ValueTask<bool> RemoveFast(TKey key)
        {
            if (!_dictionary.TryRemove(key, out _))
            {
                return false;
            }

            await _locks.WaitAsync();

            _fs.SetLength(0);
            await _fs.FlushAsync();
            foreach (var record in _dictionary)
            {
                await _writer.WriteLineAsync(JsonSerializer.Serialize(record));
            }

            await _writer.FlushAsync();
            _locks.Release();

            return true;
        }

        public ValueTask<bool> Remove(TValue value)
        {
            var record = _dictionary.FirstOrDefault(kvp => kvp.Value.Equals(value));
            if (record.Equals(default(KeyValuePair<TKey, TValue>)))
            {
                return new ValueTask<bool>(false);
            }

            return RemoveFast(record.Key);
        }

        public TValue? Get(TKey key)
        {
            return _dictionary.TryGetValue(key, out var value) ? value : default(TValue);
        }

        public bool ContainsFast(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public bool Contains(TValue value)
        {
            return _dictionary.Any(x => x.Value.Equals(value));
        }

        public async ValueTask Clear()
        {
            _dictionary.Clear();
            await _locks.WaitAsync();

            _fs.SetLength(0);
            await _fs.FlushAsync();

            _locks.Release();
        }

        public void Dispose()
        {
            _locks.Wait();
            _writer.Dispose();
        }
    }
}
