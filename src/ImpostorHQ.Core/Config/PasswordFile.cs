using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace ImpostorHQ.Core.Config
{
    public class PasswordFile : IPasswordFile
    {
        private const string Path = "IHQ_Passwords.txt";

        public List<Password> Passwords { get; }

        public PasswordFile(ILogger<IPasswordFile> logger)
        {
            if (!File.Exists(Path))
            {
                var random = "admin:" + Guid.NewGuid().ToString();
                File.WriteAllText(Path, random);
                logger.LogInformation($"ImpostorHQ: Created random password [{random}] for user \"admin\".");
            }

            var lines = File.ReadAllLines(Path);

            if (lines.Length == 0) throw new Exception("Password file cannot be empty.");

            var passwords = new List<Password>();
            
            foreach (var line in lines)
            {
                var tokens = line.Split(':', 2, StringSplitOptions.None);

                if (tokens.Length != 2)
                {
                    throw new Exception($"Invalid password: \"{line}\". The format is user:pass");
                }

                var user = tokens[0];
                var pass = tokens[1];

                if (passwords.Any(p => p.User.Equals(user))) throw new Exception("User duplicates in password file.");
                if (string.IsNullOrEmpty(pass)) throw new Exception("Invalid password in file.");
                if (string.IsNullOrEmpty(user)) throw new Exception("Invalid user in file.");
                if (pass.Length < 5) throw new Exception($"password too weak: \"{line}\"");
                if (pass.Length < 8) logger.LogWarning($"ImpostorHQ: \"{line}\" is not a very strong password.");

                passwords.Add(new Password(pass, user));
            }

            if (passwords.Count == 0) throw new Exception("No valid passwords in file.");
            Passwords = passwords;
        }
    }

    public interface IPasswordFile
    {
        List<Password> Passwords { get; }
    }
}