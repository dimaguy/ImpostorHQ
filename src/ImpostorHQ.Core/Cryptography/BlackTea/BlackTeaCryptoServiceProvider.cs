using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace ImpostorHQ.Core.Cryptography.BlackTea
{
    public class BlackTeaCryptoServiceProvider
    {
        private readonly BlockManipulator _blockManipulator;

        private readonly FastBitConverter _fastBitConverter;

        private readonly KeyGenerator _keyGenerator;

        public BlackTeaCryptoServiceProvider(BlockManipulator blockManipulator, KeyGenerator keyGenerator,
            FastBitConverter bitConverter)
        {
            _blockManipulator = blockManipulator;
            _keyGenerator = keyGenerator;
            _fastBitConverter = bitConverter;
        }

        public byte[] EncryptRaw(byte[] data, byte[] password)
        {
            var blockLength = _blockManipulator.NextBlockMultiple(data.Length + 8);
            var keyBuffer = _keyGenerator.CreateKey(password);

            var result = new byte[blockLength];

            var blockBuffer = (Span<uint>) stackalloc uint[2];
            var byteBuffer = (Span<byte>) stackalloc byte[4];

            Buffer.BlockCopy(BitConverter.GetBytes((long) data.Length), 0, result, 0, 8);

            Buffer.BlockCopy(data, 0, result, 8, data.Length);

            using var ms = new MemoryStream(result);

            for (var i = 0; i < blockLength; i += 8)
            {
                blockBuffer[0] = _fastBitConverter.GetUInt32UnsafeFastest(result, i);
                blockBuffer[1] = _fastBitConverter.GetUInt32UnsafeFastest(result, i + 4);
                _blockManipulator.EncryptBlock(blockBuffer, keyBuffer);
                _fastBitConverter.SetUInt32Unsafe(byteBuffer, blockBuffer[0], 0);
                ms.Write(byteBuffer);
                _fastBitConverter.SetUInt32Unsafe(byteBuffer, blockBuffer[1], 0);
                ms.Write(byteBuffer);
            }

            return result;
        }

        public byte[] DecryptRaw(byte[] data, byte[] password)
        {
            var keyBuffer = _keyGenerator.CreateKey(password);

            var blockBuffer = (Span<uint>) stackalloc uint[2];
            var byteBuffer = (Span<byte>) stackalloc byte[4];
            var destinationBuffer = (Span<byte>) stackalloc byte[8];

            DecryptBlock(keyBuffer, blockBuffer, byteBuffer, data, destinationBuffer, 0);

            var predictedLength = BitConverter.ToInt64(destinationBuffer);

            var blockLength = _blockManipulator.NextBlockMultiple((int) predictedLength + 8);

            if (blockLength != data.Length) throw new Exception("Length check failed.");

            var result = new byte[predictedLength];

            var source = data.AsSpan(8);
            for (var i = 0; i < source.Length; i += 8)
            {
                DecryptBlock(keyBuffer, blockBuffer, byteBuffer, source, result, i);
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DecryptBlock(ReadOnlySpan<uint> key, Span<uint> blockBuffer, Span<byte> byteBuffer,
            Span<byte> source, Span<byte> destination, int index)
        {
            blockBuffer[0] = _fastBitConverter.GetUInt32UnsafeFastest(source, index);
            blockBuffer[1] = _fastBitConverter.GetUInt32UnsafeFastest(source, index + 4);
            _blockManipulator.DecryptBlock(blockBuffer, key);
            _fastBitConverter.SetUInt32Unsafe(byteBuffer, blockBuffer[0], 0);

            var remaining = Math.Min(4, destination.Length - index);
            if(remaining < 1) return;
            byteBuffer[..remaining].CopyTo(destination.Slice(index));


            _fastBitConverter.SetUInt32Unsafe(byteBuffer, blockBuffer[1], 0);
            remaining = Math.Min(4, destination.Length - index - 4);
            if (remaining < 1) return;
            byteBuffer[..remaining].CopyTo(destination.Slice(index + 4));
        }
    }
}