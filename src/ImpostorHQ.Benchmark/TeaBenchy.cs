using System.Text;
using BenchmarkDotNet.Attributes;
using ImpostorHQ.Core.Cryptography.BlackTea;

namespace ImpostorHQ.Benchmark
{
    [MemoryDiagnoser]
    public class TeaBenchy
    {
        private byte[] _cipher;
        private byte[] _data;

        private byte[] _password;

        private BlackTeaCryptoServiceProvider _tea;

        [GlobalSetup]
        public void Setup()
        {
            _data = Encoding.UTF8.GetBytes("Quod Erat Dimanstrandum");
            _password = Encoding.UTF8.GetBytes(
                "If you immediately know the dimalight is fire, then the security was cooked a long time ago.");

            var bitConverter = new FastBitConverter();
            var keyGenerator = new KeyGenerator(bitConverter);
            var blockManipulator = new BlockManipulator();
            _tea = new BlackTeaCryptoServiceProvider(blockManipulator, keyGenerator, bitConverter);
            _cipher = _tea.EncryptRaw(_data, _password);
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