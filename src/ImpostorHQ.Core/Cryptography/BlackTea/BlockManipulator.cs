using System;

namespace ImpostorHQ.Core.Cryptography.BlackTea
{
    public class BlockManipulator
    {
        private const ushort Rounds = 32;
        private const uint Delta = 0x9E3779B9;

        public void EncryptBlock(Span<uint> block, ReadOnlySpan<uint> key)
        {
            var v0 = block[0];
            var v1 = block[1];
            uint sum = 0;
            for (uint i = 0; i < Rounds; i++)
            {
                v0 += (((v1 << 4) ^ (v1 >> 5)) + v1) ^ (sum + key[(int) (sum & 3)]);
                sum += Delta;
                v1 += (((v0 << 4) ^ (v0 >> 5)) + v0) ^ (sum + key[(int) ((sum >> 11) & 3)]);
            }

            block[0] = v0;
            block[1] = v1;
        }

        public void DecryptBlock(Span<uint> block, ReadOnlySpan<uint> key)
        {
            var v0 = block[0];
            var v1 = block[1];
            var sum = unchecked(Delta * Rounds);
            for (uint i = 0; i < Rounds; i++)
            {
                v1 -= (((v0 << 4) ^ (v0 >> 5)) + v0) ^ (sum + key[(int) ((sum >> 11) & 3)]);
                sum -= Delta;
                v0 -= (((v1 << 4) ^ (v1 >> 5)) + v1) ^ (sum + key[(int) (sum & 3)]);
            }

            block[0] = v0;
            block[1] = v1;
        }

        public int NextBlockMultiple(int length)
        {
            return (length + 7) / 8 * 8;
        }
    }
}