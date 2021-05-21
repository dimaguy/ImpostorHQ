using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ImpostorHQ.Core.Commands
{
    public class CommandParser<T> where T : class, ICommand
    {
        private readonly ConcurrentDictionary<string, T> _commands;

        public CommandParser()
        {
            _commands = new ConcurrentDictionary<string, T>();
        }

        public ICollection<T> Commands => _commands.Values;

        public void Register(T command)
        {
            if (_commands.ContainsKey(command.Prefix))
                throw new InvalidOperationException("A command with the same prefix already exists.");

            _commands.TryAdd(command.Prefix, command);
        }

        public CommandParseResult<T> TryParse(string text)
        {
            if (string.IsNullOrEmpty(text)) return new CommandParseResult<T>(null, null, ParseStatus.WhiteSpace);

            if (!text.Contains(' '))
            {
                if (!_commands.TryGetValue(text, out var cmd))
                {
                    return new CommandParseResult<T>(null, null, ParseStatus.UnknownCommand);
                }

                if (cmd.Tokens != 0)
                {
                    return new CommandParseResult<T>(cmd, null, ParseStatus.NoData);
                }

                return new CommandParseResult<T>(cmd, null);
            }

            var tokens = text.Trim().Split(' ');

            if (string.IsNullOrEmpty(tokens[0]))
            {
                return new CommandParseResult<T>(null, null, ParseStatus.WhiteSpace);
            }

            if (!_commands.TryGetValue(tokens[0], out var command))
            {
                return new CommandParseResult<T>(null, tokens, ParseStatus.UnknownCommand);
            }

            if (command.Tokens != tokens.Length - 1)
            {
                return new CommandParseResult<T>(command, tokens, ParseStatus.InvalidSyntax);
            }

            return new CommandParseResult<T>(command, tokens.Skip(1).ToArray());
        }
    }
}