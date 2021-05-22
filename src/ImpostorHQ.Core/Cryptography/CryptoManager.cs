#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using ImpostorHQ.Core.Api.Message;
using ImpostorHQ.Core.Config;
using ImpostorHQ.Core.Cryptography.BlackTea;
using Microsoft.Extensions.Logging;

namespace ImpostorHQ.Core.Cryptography
{
    public class CryptoManager : ICryptoManager
    {
        private readonly IBlackTea _crypto;

        private readonly ILogger _logger;

        private readonly List<Password> _passwords;

        public CryptoManager(ILogger<ICryptoManager> logger, IBlackTea btc, IPasswordFile passwordProvider)
        {
            _logger = logger;
            _crypto = btc;
            _passwords = passwordProvider.Passwords;
        }

        public bool TryDecrypt(string cipher, out (Password password, ApiMessage data) result)
        {
            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipher);
                foreach (var password in _passwords)
                {
                    try
                    {
                        var passwordBytes = Encoding.UTF8.GetBytes(password.ToString());
                        var plainBytes = _crypto.DecryptRaw(cipherBytes, passwordBytes);
                        var message = JsonSerializer.Deserialize<ApiMessage>(Encoding.UTF8.GetString(plainBytes));
                        result = (password, message);
                        return true;
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
            catch
            {
               // base 64
            }

            result = default;
            return false;

        }

        public string? Decrypt(string cipher, string password)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            try
            {
                var cipherBytes = Convert.FromBase64String(cipher);
                var plainBytes = _crypto.DecryptRaw(cipherBytes, passwordBytes);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ImpostorHQ Decrypt error: {ex.Message}");
                return null;
            }
        }
    }

    public interface ICryptoManager
    {
        bool TryDecrypt(string cipher, out (Password password, ApiMessage data) result);

        string? Decrypt(string cipher, string password);
    }
}