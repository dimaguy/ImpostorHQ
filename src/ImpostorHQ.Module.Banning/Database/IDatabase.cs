#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ImpostorHQ.Module.Banning.Database
{
    public interface IDatabase<in TKey, TValue> : IDisposable where TValue : notnull where TKey : notnull
    {
        string FilePath { get; }

        ICollection<TValue> Elements { get; }

        ValueTask<bool> Add(TKey key, TValue value);

        ValueTask<bool> RemoveFast(TKey key);

        ValueTask<bool> Remove(TValue value);

        TValue? Get(TKey key);

        bool ContainsFast(TKey key);

        bool Contains(TValue value);

        ValueTask Clear();
    }
}