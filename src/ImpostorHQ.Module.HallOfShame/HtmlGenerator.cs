using System.Linq;
using System.Text;
using ImpostorHQ.Module.Banning;
using ImpostorHQ.Module.Banning.Database;
using ImpostorHQ.Module.HallOfShame.Properties;
using Microsoft.Extensions.ObjectPool;

namespace ImpostorHQ.Module.HallOfShame
{
    public class HtmlGenerator
    {
        private readonly IDatabase<string, PlayerBan> _database;

        private readonly ObjectPool<StringBuilder> _sbPool;

        public HtmlGenerator(IDatabase<string, PlayerBan> banDatabase, ObjectPool<StringBuilder> sbPool)
        {
            _database = banDatabase;
            _sbPool = sbPool;
        }

        public string Generate()
        {
            var sb = _sbPool.Get();

            sb.AppendLine(Resources.startHtml);
            foreach (var databaseBan in _database.Elements)
            {
                sb.Append("<td>\r\n");
                sb.Append($"\"{databaseBan.PlayerNames[0]}\"");
                if (databaseBan.PlayerNames.Length > 1)
                {
                    sb.Append(" (aka ");
                    sb.Append(string.Join(", ", databaseBan.PlayerNames.Skip(1).Select(name => $"\"{name}\"")));
                    sb.Append($")");
                }
                sb.Append($", banned for \"{databaseBan.Reason}\"</td>\r\n");
            }

            sb.AppendLine(Resources.endHtml);

            var result = sb.ToString();
            _sbPool.Return(sb);
            return result;
        }
    }
}