using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Linq;
using Impostor.Api.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Encoder = System.Text.Encoder;

namespace Impostor.Commands.Core
{
    public class QuodEratDemonstrandum
    {
        /// <summary>
        /// This is a working player list.
        /// </summary>
        public class QuiteElegantDirectory
        {
            //nothing special going on here, but it is technically a quantum effect :)
            //P.S this is a joke
            public Thread ObserverThread { get; private set; }
            private bool DoObserve { get; set; }
            private List<IClientPlayer> Players { get; set; }

            public QuiteElegantDirectory()
            {
                this.Players = new List<IClientPlayer>();
                DoObserve = true;
                ObserverThread = new Thread(ObserverCallback);
                ObserverThread.Start();
            }

            //they are going to be entangled with the players in the enumerable list
            public async Task EntanglePlayer(IClientPlayer player)
            {
                if (player == null) return;
                await Task.Run(() =>
                {
                    lock (Players)
                    {
                        Players.Add(player);
                    }
                }).ConfigureAwait(false);
            }

            public void RemoveDeadPlayer(IClientPlayer player)
            {
                lock (Players)
                {
                    if (player!=null && Players.Contains(player)) Players.Remove(player);
                }
            }

            private void ObserverCallback()
            {
                while (DoObserve)
                {
                    Observe().ConfigureAwait(false);
                    Thread.Sleep(1000);
                }
            }

            public async Task Observe()
            {
                lock(Players)
                    if (Players.Count == 0)
                        return;
                await Task.Run(()  =>
                {
                    lock (Players)
                    {
                        foreach (var clientPlayer in Players)
                        {
                            if(CollapseSuperposition(clientPlayer))
                            {
                                RemoveDeadPlayer(clientPlayer);
                            }
                        }
                    }
                }).ConfigureAwait(false);
            }

            public List<IClientPlayer> AcquireList()
            {
                lock (Players)
                {
                    if (Players.Count == 0) return new List<IClientPlayer>();
                    return Players.ToList();
                }
            }

            private bool CollapseSuperposition(IClientPlayer clientPlayer)
            {
                //we observe the state, so we collapse the superposition.
                return (clientPlayer == null || clientPlayer.Client.Connection == null ||
                        !clientPlayer.Client.Connection.IsConnected);
            }
            
            public void Shutdown()
            {
                DoObserve = false;
            }
        }
        /// <summary>
        /// A relic of the past.
        /// </summary>
        public class QuiteEnigmaticData
        {
            public readonly string[] AcceptedPasswords;
            private readonly BlackTeaSharp cryptographicFunction = new BlackTeaSharp();
            /// <summary>
            /// This class is a relic of the past. It is not wired into any systems, so it can be removed. I left it here because it might be used by somebody...
            /// </summary>
            /// <param name="acceptedPasswords"></param>
            public QuiteEnigmaticData(string[] acceptedPasswords)
            {
                this.AcceptedPasswords = acceptedPasswords;
            }
            /// <summary>
            /// This is used by the authentication protocol to identify correct keys.
            /// </summary>
            /// <param name="data"></param>
            /// <returns></returns>
            public EnigmaticDataResult TryDecrypt(string data)
            {
                var dataBin = Convert.FromBase64String(data);
                foreach (var password in AcceptedPasswords)
                {
                    try
                    {
                        var plaintext = cryptographicFunction.DecryptNonExceptable(dataBin, Encoding.UTF8.GetBytes(password));
                        if(plaintext == null) continue;
                        return new EnigmaticDataResult(Encoding.UTF8.GetString(plaintext), password);
                    }
                    catch
                    {
                    }
                }

                return null;
            }
            /// <summary>
            /// This will decrypt the data. Only use this when you are certain that you have the correct key!
            /// </summary>
            /// <returns></returns>
            public string Decrypt(string data, string key)
            {
                try
                {
                    return cryptographicFunction.Decrypt(data, key);
                }
                catch (ArgumentException)
                {
                    return null;
                }
                catch (Exception ex)
                {
                    return null;
                    //unexpected error.
                }
            }
            /// <summary>
            /// This will encrypt the data.
            /// </summary>
            /// <param name="data"></param>
            /// <param name="key"></param>
            /// <returns></returns>
            public string Encrypt(string data, string key)
            {
                return cryptographicFunction.Encrypt(data, key);
            }
            public class EnigmaticDataResult
            {
                public EnigmaticDataResult(string data, string pass)
                {
                    this.Data = data;
                    this.Password = pass;
                }
                public string Data { get; private set; }
                public string Password { get; private set; }
            }
            internal sealed class BlackTeaSharp
            {
                readonly ArrayPool<byte> BufferPool = ArrayPool<byte>.Shared;
                readonly ArrayPool<uint> BlockPool = ArrayPool<uint>.Shared;
                private readonly ushort Rounds = 32;
                private readonly uint delta = 0x9E3779B9;
                private readonly MD5 md5computer = MD5.Create();

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
                    var rLen = NextBlockMultiple(data.Length + 4);
                    var keyBuffer = CreateKey(password);
                    var blockBuffer = BlockPool.Rent(2);
                    var result = BufferPool.Rent(rLen);
                    var lengthBuffer = BufferPool.Rent(4);
                    var bitBuffer = BufferPool.Rent(4);
                    FastBitConverter.SetUInt32Unsafe(lengthBuffer, (uint)data.Length, 0);
                    Array.Copy(lengthBuffer, 0, result, 0, 4);
                    Array.Copy(data, 0, result, 4, data.Length);
                    using (var stream = new MemoryStream(result))
                    {
                        for (int i = 0; i < rLen; i += 8)
                        {
                            blockBuffer[0] = FastBitConverter.GetUInt32UnsafeFastest(result, (uint)i);
                            blockBuffer[1] = FastBitConverter.GetUInt32UnsafeFastest(result, (uint)i + 4);
                            EncryptBlock(blockBuffer, keyBuffer);
                            FastBitConverter.SetUInt32Unsafe(bitBuffer, blockBuffer[0], 0);
                            stream.Write(bitBuffer, 0, 4);
                            FastBitConverter.SetUInt32Unsafe(bitBuffer, blockBuffer[1], 0);
                            stream.Write(bitBuffer, 0, 4);
                        }
                    }

                    var str = Convert.ToBase64String(result);
                    BlockPool.Return(blockBuffer);
                    BufferPool.Return(result);
                    BufferPool.Return(lengthBuffer);
                    BufferPool.Return(bitBuffer);
                    BlockPool.Return(keyBuffer);
                    return str;
                }

                /// <summary>
                /// Will encrypt the specified block, with the specified key.
                /// </summary>
                /// <param name="block">The block to encrypt.</param>
                /// <param name="key">The key to use.</param>
                private void EncryptBlock(uint[] block, uint[] key)
                {
                    uint v0 = block[0];
                    uint v1 = block[1];
                    uint sum = 0;
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

                public byte[] Decrypt(byte[] data, byte[] key)
                {
                    if (data.Length % 8 != 0) throw new ArgumentException("Invalid data length.");
                    var keyBuffer = CreateKey(key);
                    var blockBuffer = BlockPool.Rent(2);
                    var buffer = BufferPool.Rent(data.Length);
                    var bitBuffer = BufferPool.Rent(4);
                    Array.Copy(data, buffer, data.Length);
                    using (var stream = new MemoryStream(buffer))
                    {
                        for (uint i = 0; i < buffer.Length; i += 8)
                        {
                            blockBuffer[0] = FastBitConverter.GetUInt32UnsafeFastest(buffer, i);
                            blockBuffer[1] = FastBitConverter.GetUInt32UnsafeFastest(buffer, i + 4);
                            DecryptBlock(blockBuffer, keyBuffer);
                            FastBitConverter.SetUInt32Unsafe(bitBuffer, blockBuffer[0], 0);
                            stream.Write(bitBuffer, 0, 4);
                            FastBitConverter.SetUInt32Unsafe(bitBuffer, blockBuffer[1], 0);
                            stream.Write(bitBuffer, 0, 4);
                        }
                    }

                    var length = FastBitConverter.GetUInt32UnsafeFastest(buffer, 0);
                    if (length > buffer.Length - 4)
                    {
                        BufferPool.Return(buffer);
                        BufferPool.Return(bitBuffer);
                        BlockPool.Return(blockBuffer);
                        BlockPool.Return(keyBuffer);
                        throw new ArgumentException("Length checks have failed.");
                    }

                    var result = new byte[length];
                    Array.Copy(buffer, 4, result, 0, length);
                    BufferPool.Return(buffer);
                    BufferPool.Return(bitBuffer);
                    BlockPool.Return(blockBuffer);
                    BlockPool.Return(keyBuffer);
                    return result;
                }

                /// <summary>
                /// This function will not throw anything.
                /// </summary>
                /// <param name="data">The data to decrypt.</param>
                /// <param name="key">The key to use.</param>
                /// <returns>An array if successful, null if not.</returns>
                public byte[] DecryptNonExceptable(byte[] data, byte[] key)
                {
                    if (data.Length % 8 != 0) return null;
                    var keyBuffer = CreateKey(key);
                    var blockBuffer = BlockPool.Rent(2);
                    var buffer = BufferPool.Rent(data.Length);
                    var bitBuffer = BufferPool.Rent(4);
                    Array.Copy(data, buffer, data.Length);
                    using (var stream = new MemoryStream(buffer))
                    {
                        for (uint i = 0; i < buffer.Length; i += 8)
                        {
                            blockBuffer[0] = FastBitConverter.GetUInt32UnsafeFastest(buffer, i);
                            blockBuffer[1] = FastBitConverter.GetUInt32UnsafeFastest(buffer, i + 4);
                            DecryptBlock(blockBuffer, keyBuffer);
                            FastBitConverter.SetUInt32Unsafe(bitBuffer, blockBuffer[0], 0);
                            stream.Write(bitBuffer, 0, 4);
                            FastBitConverter.SetUInt32Unsafe(bitBuffer, blockBuffer[1], 0);
                            stream.Write(bitBuffer, 0, 4);
                        }
                    }

                    var length = FastBitConverter.GetUInt32UnsafeFastest(buffer, 0);
                    if (length > buffer.Length - 4)
                    {
                        BufferPool.Return(buffer);
                        BufferPool.Return(bitBuffer);
                        BlockPool.Return(blockBuffer);
                        BlockPool.Return(keyBuffer);
                        return null;
                    }

                    var result = new byte[length];
                    Array.Copy(buffer, 4, result, 0, length);
                    BufferPool.Return(buffer);
                    BufferPool.Return(bitBuffer);
                    BlockPool.Return(blockBuffer);
                    BlockPool.Return(keyBuffer);
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

                private uint[] CreateKey(byte[] password)
                {
                    var hash = md5computer.ComputeHash(password);
                    var key = BlockPool.Rent(4);
                    key[0] = (uint)Math.Abs(FastBitConverter.GetInt32UnsafeFastest(hash, 0));
                    key[1] = (uint)Math.Abs(FastBitConverter.GetInt32UnsafeFastest(hash, 4));
                    key[2] = (uint)Math.Abs(FastBitConverter.GetInt32UnsafeFastest(hash, 8));
                    key[3] = (uint)Math.Abs(FastBitConverter.GetInt32UnsafeFastest(hash, 12));
                    return key;
                }

                /// <summary>
                /// In my tests, this is a lot faster.
                /// </summary>
                static unsafe class FastBitConverter
                {
                    #region Uint

                    public static uint GetUInt32UnsafeFastest(byte[] array, uint offset)
                    {
                        fixed (byte* ptr = &array[0])
                        {
                            return *(uint*)(ptr + offset);
                        }
                    }

                    public static void SetUInt32Unsafe(byte[] target, uint value, uint index)
                    {
                        uint* p = &value;
                        target[index] = *((byte*)p);
                        target[++index] = *((byte*)p + 1);
                        target[++index] = *((byte*)p + 2);
                        target[++index] = *((byte*)p + 3);
                    }

                    #endregion

                    #region Int

                    public static int GetInt32UnsafeFastest(byte[] array, int offset)
                    {
                        fixed (byte* ptr = &array[0])
                        {
                            return *(int*)(ptr + offset);
                        }
                    }

                    #endregion
                }
            }
        }
    }
}
