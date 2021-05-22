using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.ObjectPool;

namespace ImpostorHQ.Core.Commands
{
    public class CommandHelpProvider : ICommandHelpProvider
    {
        private readonly ObjectPool<StringBuilder> _sbPool;

        public CommandHelpProvider(ObjectPool<StringBuilder> sbPool)
        {
            _sbPool = sbPool;
        }

        public string CreateHelp(IEnumerable<ICommand> commands)
        {
            var sb = _sbPool.Get();
            sb.Append("\nUse /help [command] to see more information. Commands:");
            sb.Append("\r\n");
            foreach (var command in commands)
            {
                sb.Append("  ").Append(command.Prefix).Append("\n");
            }

            var result = sb.ToString();

            _sbPool.Return(sb);

            return result;
        }
    }

    public interface ICommandHelpProvider
    {
        string CreateHelp(IEnumerable<ICommand> commands);
    }
}