using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using ImpostorHQ.Core.Cryptography;
using ImpostorHQ.Core.Cryptography.BlackTea;
using NUnit.Framework;

namespace ImpostorHQ.Tests
{
    class TeaTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestEncryptDecrypt()
        {
            var bitConverter = new FastBitConverter();
            var keyGenerator = new KeyGenerator(bitConverter);
            var blockManipulator = new BlockManipulator();
            var tea = new BlackTeaCryptoServiceProvider(blockManipulator, keyGenerator, bitConverter);

            var password = Encoding.UTF8.GetBytes("Quod Erat Dimanstrandum");
            var data = Encoding.UTF8.GetBytes("If you immediately know the dimalight is fire, then the security was cooked a long time ago.");

            var cipher = tea.EncryptRaw(data, password);
            var plain = tea.DecryptRaw(cipher, password);

            Assert.IsTrue(plain.SequenceEqual(data));
        }

        [Test]
        public void TestDeterministicOutput()
        {
            var bitConverter = new FastBitConverter();
            var keyGenerator = new KeyGenerator(bitConverter);
            var blockManipulator = new BlockManipulator();
            var tea = new BlackTeaCryptoServiceProvider(blockManipulator, keyGenerator, bitConverter);

            var passwordBytes = new byte[16];
            var dataBytes = new byte[16];

            for (var i = 0; i < 10000; i++)
            {
                Guid.NewGuid().TryWriteBytes(passwordBytes);
                Guid.NewGuid().TryWriteBytes(dataBytes);

                var cipher = tea.EncryptRaw(dataBytes, passwordBytes);
                var plain = tea.DecryptRaw(cipher, passwordBytes);

                Assert.IsTrue(plain.SequenceEqual(dataBytes));
            }
        }
    }
}
