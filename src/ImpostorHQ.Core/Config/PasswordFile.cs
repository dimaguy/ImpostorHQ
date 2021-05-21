using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace ImpostorHQ.Core.Config
{
    public class PasswordFile
    {
        private const string Path = "IHQ_Passwords.txt";
        public readonly string[] Passwords;

        public PasswordFile(ILogger<PasswordFile> logger)
        {
            if (!File.Exists(Path))
            {
                var random = Guid.NewGuid().ToString();
                File.WriteAllText(Path, random);
                logger.LogInformation($"ImpostorHQ: Created random password [{random}]");
            }

            var lines = File.ReadAllLines(Path);

            if (lines.Length == 0) throw new Exception("Password file cannot be empty.");
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) throw new Exception("Empty passwords in file.");
                if (line.Length < 5) throw new Exception($"password too weak: \"{line}\"");
                if (line.Length < 8) logger.LogWarning($"ImpostorHQ: \"{line}\" is not a very strong password.");
            }

            Passwords = lines;
        }
    }
}