using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace Impostor.Commands.Core.Client
{
    internal sealed class BlackTeaSharp
    {
        private readonly ushort Rounds = 32;
        private readonly uint delta = 0x9E3779B9;

        private readonly MD5CryptoServiceProvider md5computer = new MD5CryptoServiceProvider();

        #region Final Functions

        /// <summary>
        /// This is used to encrypt data.
        /// </summary>
        /// <param name="data">The data to encrypt.</param>
        /// <param name="password">The password to use.</param>
        /// <returns>Cipher text, encoded in base64.</returns>
        public string Encrypt(string data, string password)
        {
            return Encrypt(Encoding.UTF8.GetBytes(data), Encoding.UTF8.GetBytes(password));
        }
        /// <summary>
        /// This is used to decrypt data.
        /// </summary>
        /// <param name="data">The base64 data to decrypt.</param>
        /// <param name="password">The password to use.</param>
        /// <returns>The plaintext.</returns>
        public string Decrypt(string data, string password)
        {
            return Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(data), Encoding.UTF8.GetBytes(password)));
        }

        #endregion

        #region Encryption
        private string Encrypt(byte[] data, byte[] password)
        {
            var keyBuffer = CreateKey(password);
            var blockBuffer = new uint[2];
            var result = new byte[NextBlockMultiple(data.Length + 4)];
            var lengthBuffer = BitConverter.GetBytes(data.Length);
            Array.Copy(lengthBuffer, result, lengthBuffer.Length);
            Array.Copy(data, 0, result, lengthBuffer.Length, data.Length);
            using (var stream = new MemoryStream(result))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    for (int i = 0; i < result.Length; i += 8)
                    {
                        blockBuffer[0] = BitConverter.ToUInt32(result, i);
                        blockBuffer[1] = BitConverter.ToUInt32(result, i + 4);
                        EncryptBlock(blockBuffer, keyBuffer);
                        writer.Write(blockBuffer[0]);
                        writer.Write(blockBuffer[1]);
                    }
                }
            }
            return Convert.ToBase64String(result);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EncryptBlock(uint[] block, uint[] key)
        {
            uint v0 = block[0], v1 = block[1], sum = 0, delta = 0x9E3779B9; //nothing up my sleeve
            for (uint i = 0; i < Rounds; i++)
            {
                v0 += (((v1 << 4) ^ (v1 >> 5)) + v1) ^ (sum + key[sum & 3]);
                sum += delta;
                v1 += (((v0 << 4) ^ (v0 >> 5)) + v0) ^ (sum + key[(sum >> 11) & 3]);
            }
            block[0] = v0;
            block[1] = v1;
        }


        #endregion

        #region Decryption

        private byte[] Decrypt(byte[] data, byte[] key)
        {
            if (data.Length % 8 != 0) throw new ArgumentException("Invalid data length.");
            var keyBuffer = CreateKey(key);
            var blockBuffer = new uint[2];
            var buffer = new byte[data.Length];
            Array.Copy(data, buffer, data.Length);
            using (var stream = new MemoryStream(buffer))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    for (int i = 0; i < buffer.Length; i += 8)
                    {
                        blockBuffer[0] = BitConverter.ToUInt32(buffer, i);
                        blockBuffer[1] = BitConverter.ToUInt32(buffer, i + 4);
                        DecryptBlock(blockBuffer, keyBuffer);
                        writer.Write(blockBuffer[0]);
                        writer.Write(blockBuffer[1]);
                    }
                }
            }
            // verify valid length
            var length = BitConverter.ToUInt32(buffer, 0);
            if (length > buffer.Length - 4)
            {
                throw new ArgumentException("Length checks have failed.");
            }
            var result = new byte[length];
            Array.Copy(buffer, 4, result, 0, length);
            return result;
        }
        /// <summary>
        /// Will decrypt the specified block.
        /// </summary>
        /// <param name="block">The block to decrypt.</param>
        /// <param name="key">The key to use.</param>
        private void DecryptBlock(uint[] block, uint[] key)
        {
            var v0 = block[0];
            var v1 = block[1];
            var sum = delta * Rounds;
            for (uint i = 0; i < Rounds; i++)
            {
                v1 -= (((v0 << 4) ^ (v0 >> 5)) + v0) ^ (sum + key[(sum >> 11) & 3]);
                sum -= delta;
                v0 -= (((v1 << 4) ^ (v1 >> 5)) + v1) ^ (sum + key[sum & 3]);
            }
            block[0] = v0;
            block[1] = v1;
        }

        #endregion

        /// <summary>
        /// Will get the next multiple of 8.
        /// </summary>
        /// <param name="length">The multiple's index.</param>
        /// <returns>A multiple of 8.</returns>
        private int NextBlockMultiple(int length)
        {
            return (length + 7) / 8 * 8;
        }
        public uint[] CreateKey(byte[] password)
        {
            var hash = md5computer.ComputeHash(password);
            var signedKey = new int[]
            {
                //we cut it up into uints
                BitConverter.ToInt32(hash, 0), BitConverter.ToInt32(hash, 4),
                BitConverter.ToInt32(hash, 8), BitConverter.ToInt32(hash, 12)
            };
            return new[]
            {
                (uint) Math.Abs(signedKey[0]), (uint)Math.Abs(signedKey[1]),
                (uint) Math.Abs(signedKey[2]), (uint)Math.Abs(signedKey[3])
            };
        }
    }
}