using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using ImpostorHQ.Module.Banning;
using ImpostorHQ.Module.Banning.Database;

namespace ImpostorHQ.Benchmark
{
    [MemoryDiagnoser]
    public class BanDatabaseBenchy
    {
        public static readonly PlayerBan _ban = new PlayerBan("69.69.69.69", new string[] {"dima, mini"}, DateTime.Now,
            new string[]{"aeonlucid", "AeonLucid"}, "n/a");

        private IDatabase<string, PlayerBan> _database;

        [GlobalSetup]
        public void SetUp()
        {
            _database = new DiskDatabase<string, PlayerBan>("test");
        }

        [Benchmark]
        public async Task TestAdd_RemoveFast()
        {
            await _database.Add(_ban.IpAddress, _ban);
            await _database.RemoveFast("69.69.69.69");
        }

        [Benchmark]
        public async Task TestAdd_RemoveName()
        {
            await _database.Add(_ban.IpAddress, _ban);
            await _database.RemoveFast(_database.Get("aeonlucid")!.IpAddress);
        }
    }
}