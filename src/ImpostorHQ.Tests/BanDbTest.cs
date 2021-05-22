﻿using System;
using System.Linq;
using System.Threading.Tasks;
using ImpostorHQ.Module.Banning;
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
            var db = new BanDatabase();
            await db.Add(new PlayerBan("3.1.4.1", null, DateTime.Now, new []{"dima", "dimaguy"}, "n/a"));
            Assert.IsTrue(db.Bans.ToArray()[0].IpAddress.Equals("3.1.4.1"));
            await db.Remove("3.1.4.1");
            Assert.IsTrue(!db.Bans.Any());
            await db.Add(new PlayerBan("3.1.4.1", null, DateTime.Now, new[] { "dima", "dimaguy"}, "n/a"));
            await db.Clear();
            Assert.IsTrue(!db.Bans.Any());
        }
    }
}