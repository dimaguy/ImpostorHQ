using System;
using System.IO;
using System.Text;
using System.Linq;
using Impostor.Api.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;

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
            public readonly RijndaelManaged EncryptionProvider = new RijndaelManaged();
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
                foreach (var password in AcceptedPasswords)
                {
                    try
                    {
                        var plaintext = Decrypt(data, password);
                        return new EnigmaticDataResult(plaintext, password);
                    }
                    catch
                    {
                    }
                }

                return null;
            }
            /// <summary>
            /// This is used to encrypt data with the given key. Please note that this adds significant overhead (of up to 1 millisecond)!
            /// </summary>
            /// <param name="data">The data to encrypt.</param>
            /// <param name="pass">The password to use.</param>
            /// <returns></returns>
            public string Encrypt(string data, string pass)
            {
                var salt = GetCryptographicBytes(32);
                var key = DeriveKey(pass, salt);
                Console.WriteLine($"Key: {Convert.ToBase64String(key)}");
                return EncryptRaw(data, key, salt, GetCryptographicBytes(16));
            }
            /// <summary>
            /// This is used to decrypt data, once the correct key is known. Please note that this adds significant overhead (of up to 1 millisecond)!
            /// </summary>
            /// <param name="data">The data to decrypt.</param>
            /// <param name="key">The key to use.</param>
            /// <returns>Plaintext data.</returns>
            public string Decrypt(string data, string key)
            {
                return DecryptRaw(data, key);
            }
            /// <summary>
            /// This is used internally to encrypt and should not be used on it's own.
            /// </summary>
            /// <param name="plainText">The plain text to encrypt.</param>
            /// <param name="derivedKey">The RFC2898 compliant derived key.</param>
            /// <param name="salt">The randomly generated salt.</param>
            /// <param name="initialVector">The IV.</param>
            /// <returns>Base64 encoded cipher text.</returns>
            private string EncryptRaw(string plainText, byte[] derivedKey, byte[] salt, byte[] initialVector)
            {
                var lenBytes = BitConverter.GetBytes(plainText.Length);
                lock (EncryptionProvider)
                {
                    EncryptionProvider.Key = derivedKey;
                    EncryptionProvider.IV = initialVector;
                    EncryptionProvider.Mode = CipherMode.CBC;
                    EncryptionProvider.Padding = PaddingMode.PKCS7;
                    ICryptoTransform encryptor = EncryptionProvider.CreateEncryptor(EncryptionProvider.Key, EncryptionProvider.IV);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        ms.Write(initialVector, 0, initialVector.Length);
                        ms.Write(salt, 0, salt.Length);
                        ms.Write(lenBytes, 0, lenBytes.Length);
                        using (CryptoStream encryptionStream = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            encryptionStream.Write(Encoding.UTF8.GetBytes(plainText), 0, plainText.Length);
                            encryptionStream.FlushFinalBlock();
                            return Convert.ToBase64String(ms.ToArray());
                        }
                    }
                }
            }
            /// <summary>
            /// This is used internally to decrypt data and is safe to be used on it's own.
            /// </summary>
            /// <param name="cipher">The Base64 encrypted cipher.</param>
            /// <param name="pass">The password to use.</param>
            /// <returns>Plaintext data.</returns>
            private string DecryptRaw(string cipher, string pass)
            {
                var cipherText = Convert.FromBase64String(cipher);
                var salt = cipherText.Skip(16).ToArray().Take(32).ToArray();
                var key = DeriveKey(pass, salt);
                var len = BitConverter.ToInt32(cipherText.Skip(16 + 32).ToArray().Take(4).ToArray(), 0);
                byte[] plain = new byte[len];
                lock (EncryptionProvider)
                {
                    EncryptionProvider.Key = key;
                    EncryptionProvider.IV = cipherText.Take(16).ToArray();
                    EncryptionProvider.Mode = CipherMode.CBC;
                    EncryptionProvider.Padding = PaddingMode.PKCS7;
                    ICryptoTransform decryptor = EncryptionProvider.CreateDecryptor(EncryptionProvider.Key, EncryptionProvider.IV);
                    using (MemoryStream ms = new MemoryStream(cipherText.Skip(16 + 32 + 4).ToArray()))
                    {
                        using (CryptoStream decryptionStream = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            decryptionStream.Read(plain, 0, len);
                        }
                    }

                    return Encoding.UTF8.GetString(plain);
                }
            }
            /// <summary>
            /// This is used internally to derive a key.
            /// </summary>
            /// <param name="pass">The password to derive from.</param>
            /// <param name="salt">The salt to derive with.</param>
            /// <returns>Return a correctly sized (128 bit) key.</returns>
            private byte[] DeriveKey(string pass, byte[] salt)
            {
                using (var deriver = new Rfc2898DeriveBytes(pass, salt))
                {
                    deriver.IterationCount = 1000;
                    return deriver.GetBytes(16);
                }
            }
            /// <summary>
            /// This is used to get cryptographically strong data.
            /// </summary>
            /// <param name="count">The number of bytes to generate.</param>
            /// <returns></returns>
            private byte[] GetCryptographicBytes(int count)
            {
                var randomBytes = new byte[count];
                using (var rnd = new RNGCryptoServiceProvider()) rnd.GetBytes(randomBytes);
                return randomBytes;
            }
            /// <summary>
            /// This is used to indicate the result of a decryption attempt.
            /// </summary>
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
        }
    }
}
