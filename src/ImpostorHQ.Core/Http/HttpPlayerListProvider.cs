using System.Linq;
using System.Text;
using Impostor.Api.Games.Managers;
using Microsoft.Extensions.ObjectPool;

namespace ImpostorHQ.Core.Http
{
    public class HttpPlayerListProvider
    {
        private readonly IGameManager _gameManager;

        private readonly ObjectPool<StringBuilder> _sbPool;

        public HttpPlayerListProvider(IGameManager clientManager, ObjectPool<StringBuilder> sbPool)
        {
            _gameManager = clientManager;
            _sbPool = sbPool;
        }

        public string ComposeCsv()
        {
            var sb = _sbPool.Get();

            sb.Append("NAME,GAME,MAP,IS HOST,IS IMPOSTOR,IS DEAD,ADDRESS").Append("\r\n");

            foreach (var client in _gameManager.Games.SelectMany(x => x.Players))
            {
                sb.Append(client.Character!.PlayerInfo.PlayerName).Append(',');
                sb.Append(client.Game.Code.Code).Append(',');
                sb.Append(client.Game.Options.Map.ToString()).Append(',');
                sb.Append(client.IsHost).Append(',');
                sb.Append(client.Character.PlayerInfo.IsImpostor).Append(',');
                sb.Append(client.Character.PlayerInfo.IsDead).Append(',');
                sb.Append(client.Client.Connection!.EndPoint.Address).Append("\r\n");
            }

            var result = sb.ToString();
            _sbPool.Return(sb);

            return result;
        }

        public (string mime, byte[] data) CreateHttpResponseBody() => ("text/csv", Encoding.UTF8.GetBytes(ComposeCsv()));
    }
}