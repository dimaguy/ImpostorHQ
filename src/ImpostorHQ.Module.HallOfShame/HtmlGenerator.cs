using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImpostorHQ.Module.Banning;
using ImpostorHQ.Module.HallOfShame.Properties;
using Microsoft.Extensions.ObjectPool;

namespace ImpostorHQ.Module.HallOfShame
{
    public class HtmlGenerator
    {
        private readonly BanDatabase _database;

        private readonly ObjectPool<StringBuilder> _sbPool;

        public HtmlGenerator(BanDatabase banDatabase, ObjectPool<StringBuilder> sbPool)
        {
            this._database = banDatabase;
            this._sbPool = sbPool;
        }

        public string Generate()
        {
            var sb = _sbPool.Get();

            sb.AppendLine(Resources.startHtml);
            foreach (var databaseBan in _database.Bans)
            {
                sb.Append("<td>\r\n");
                sb.Append(databaseBan.PlayerName);
                sb.Append("</td>\r\n");
            }
            sb.AppendLine(Resources.endHtml);

            var result = sb.ToString();
            _sbPool.Return(sb);
            return result;
        }
    }
}
