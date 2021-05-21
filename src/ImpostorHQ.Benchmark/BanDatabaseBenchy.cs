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
            "aeonlucid");

        private BanDatabase _database;

        [GlobalSetup]
        public void SetUp()
        {
            _database = new BanDatabase();
        }

        [Benchmark]
        public async Task TestAddRemove()
        {
            await _database.Add(_ban);
            await _database.Remove("69.69.69.69");
        }
    }
}