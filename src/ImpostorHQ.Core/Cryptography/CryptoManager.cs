#nullable enable
using System;
using System.Text;
using System.Text.Json;
using ImpostorHQ.Core.Api.Message;
using ImpostorHQ.Core.Config;
using ImpostorHQ.Core.Cryptography.BlackTea;
using Microsoft.Extensions.Logging;

namespace ImpostorHQ.Core.Cryptography
{
    public class CryptoManager
    {
        private readonly BlackTeaCryptoServiceProvider _crypto;

        private readonly ILogger _logger;

        private readonly string[] _passwords;

        public CryptoManager(ILogger<CryptoManager> logger, BlackTeaCryptoServiceProvider btc,
            PasswordFile passwordProvider)
        {
            _logger = logger;
            _crypto = btc;
            _passwords = passwordProvider.Passwords;
        }

        public bool TryDecrypt(string cipher, out (string password, ApiMessage data) result)
        {
            var cipherBytes = Convert.FromBase64String(cipher);
            foreach (var password in _passwords)
            {
                try
                {
                    var passwordBytes = Encoding.UTF8.GetBytes(password);
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

            result = default;
            return false;
        }

        public string? Decrypt(string cipher, string password)
        {
            var cipherBytes = Convert.FromBase64String(cipher);
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            try
            {
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
}