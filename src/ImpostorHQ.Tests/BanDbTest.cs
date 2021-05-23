using System;
using System.Linq;
using System.Threading.Tasks;
using ImpostorHQ.Module.Banning;
using ImpostorHQ.Module.Banning.Database;
using NUnit.Framework;

namespace ImpostorHQ.Tests
{
    public class BanDbTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task TestRemoveAddPurge()
        {
            var db = new DiskDatabase<string, PlayerBan>("test");
            await db.Add("3.1.4.1", new PlayerBan("3.1.4.1", null, DateTime.Now, new []{"dima", "dimaguy"}, "n/a"));
            Assert.IsTrue(db.Elements.ToArray()[0].IpAddress.Equals("3.1.4.1"));
            await db.RemoveFast("3.1.4.1");
            Assert.IsTrue(!db.Elements.Any());
            await db.Add("3.1.4.1", new PlayerBan("3.1.4.1", null, DateTime.Now, new[] { "dima", "dimaguy"}, "n/a"));
            await db.Clear();
            Assert.IsTrue(!db.Elements.Any());
        }
    }
}