using System;

namespace ImpostorHQ.Core.Cryptography.BlackTea
{
    public unsafe class FastBitConverter : IBitConverter
    {
        #region Int

        public int GetInt32UnsafeFastest(Span<byte> array, int offset)
        {
            fixed (byte* ptr = &array[0])
            {
                return *(int*) (ptr + offset);
            }
        }

        #endregion

        #region Uint

        public uint GetUInt32UnsafeFastest(Span<byte> target, int offset)
        {
            fixed (byte* ptr = &target[0])
            {
                return *(uint*) (ptr + offset);
            }
        }

        public void SetUInt32UnsafeFastest(Span<byte> target, uint value, int index)
        {
            var p = &value;
            target[index] = *(byte*) p;
            target[++index] = *((byte*) p + 1);
            target[++index] = *((byte*) p + 2);
            target[++index] = *((byte*) p + 3);
        }

        #endregion
    }

    public interface IBitConverter
    {
        int GetInt32UnsafeFastest(Span<byte> array, int offset);

        uint GetUInt32UnsafeFastest(Span<byte> target, int offset);

        void SetUInt32UnsafeFastest(Span<byte> target, uint value, int index);
    }
}