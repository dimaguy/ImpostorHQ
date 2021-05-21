using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using ImpostorHQ.Module.Banning;

namespace ImpostorHQ.Benchmark
{
    [MemoryDiagnoser]
    public class BanDatabaseBenchy
    {
        private BanDatabase _database;

        public static readonly PlayerBan _ban = new PlayerBan("69.69.69.69", new string[] {"dima, mini"}, DateTime.Now,
            "aeonlucid");

        [GlobalSetup]
        public void SetUp()
        {
            this._database = new BanDatabase();
        }

        [Benchmark]
        public async Task TestAddRemove()
        {
            await _database.Add(_ban);
            await _database.Remove("69.69.69.69");
        }
    }
}
