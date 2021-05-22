using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using ImpostorHQ.Module.Banning;

namespace ImpostorHQ.Benchmark
{
    [MemoryDiagnoser]
    public class BanDatabaseBenchy
    {
        public static readonly PlayerBan _ban = new PlayerBan("69.69.69.69", new string[] {"dima, mini"}, DateTime.Now,
            new string[]{"aeonlucid", "AeonLucid"}, "n/a");

        private BanDatabase _database;

        [GlobalSetup]
        public void SetUp()
        {
            _database = new BanDatabase();
        }

        [Benchmark]
        public async Task TestAdd_RemoveFast()
        {
            await _database.Add(_ban);
            await _database.Remove("69.69.69.69");
        }

        [Benchmark]
        public async Task TestAdd_RemoveName()
        {
            await _database.Add(_ban);
            await _database.Remove(_database.Get("aeonlucid")!.Value.IpAddress);
        }
    }
}