using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using ImpostorHQ.Core.Cryptography.BlackTea;

namespace ImpostorHQ.Benchmark
{
    [MemoryDiagnoser]
    public class TeaBenchy
    {
        private byte[] _data;

        private byte[] _password;

        private BlackTeaCryptoServiceProvider _tea;

        private byte[] _cipher;

        [GlobalSetup]
        public void Setup()
        {
            this._data = Encoding.UTF8.GetBytes("Quod Erat Dimanstrandum");
            this._password = Encoding.UTF8.GetBytes("If you immediately know the dimalight is fire, then the security was cooked a long time ago.");

            var bitConverter = new FastBitConverter();
            var keyGenerator = new KeyGenerator(bitConverter);
            var blockManipulator = new BlockManipulator();
            this._tea = new BlackTeaCryptoServiceProvider(blockManipulator, keyGenerator, bitConverter);
            this._cipher = _tea.EncryptRaw(_data, _password);
        }

        [Benchmark]
        public void Encrypt()
        {
            _tea.EncryptRaw(_data, _password);
        }

        [Benchmark]
        public void Decrypt()
        {
            _tea.DecryptRaw(_cipher, _password);
        }
    }
}
