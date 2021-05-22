using System;
using System.Security.Cryptography;

namespace ImpostorHQ.Core.Cryptography.BlackTea
{
    public class KeyGenerator : IKeyGenerator
    {
        private static readonly MD5 Md5 = new MD5CryptoServiceProvider();

        private readonly IBitConverter _bitConverter;

        public KeyGenerator(IBitConverter bitConverter)
        {
            _bitConverter = bitConverter;
        }

        public uint[] CreateKey(byte[] password)
        {
            var hash = Md5.ComputeHash(password);
            var key = new uint[4];
            key[0] = (uint) Math.Abs(_bitConverter.GetInt32UnsafeFastest(hash, 0));
            key[1] = (uint) Math.Abs(_bitConverter.GetInt32UnsafeFastest(hash, 4));
            key[2] = (uint) Math.Abs(_bitConverter.GetInt32UnsafeFastest(hash, 8));
            key[3] = (uint) Math.Abs(_bitConverter.GetInt32UnsafeFastest(hash, 12));
            return key;
        }
    }

    public interface IKeyGenerator
    {
        uint[] CreateKey(byte[] password);
    }
}