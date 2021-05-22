using System.Collections.Generic;
using System.Linq;
using System.Text;
using ImpostorHQ.Core.Config;
using ImpostorHQ.Core.Properties;
using ImpostorHQ.Http;
using ImpostorHQ.Http.Handler;

namespace ImpostorHQ.Core.Http
{
    public class HttpRootConfigurator
    {
        private readonly ImpostorHqConfig _config;

        private readonly List<Password> _passwords;

        private readonly IHttpPlayerListProvider _playerList;
        private readonly HttpServer _server;

        public HttpRootConfigurator(
            HttpServer server, 
            ImpostorHqConfig config, 
            IPasswordFile passwordFile,
            IHttpPlayerListProvider playerListProvider)
        {
            _server = server;
            _config = config;
            _playerList = playerListProvider;
            _passwords = passwordFile.Passwords;
        }

        public void Configure()
        {
            _server.AddHandler(new StaticHandler(_config.DashboardEndPoint,
                Encoding.UTF8.GetBytes(Resources.htmlClient), "text/html"));
            _server.AddHandler(new StaticHandler("/favicon.ico", Resources.favicon, "image/x-icon"));

            _server.AddHandler(new StaticHandler("/css/water.css", Encoding.UTF8.GetBytes(Resources.water),
                "text/css"));
            _server.AddHandler(new StaticHandler("/css/header.css", Encoding.UTF8.GetBytes(Resources.header),
                "text/css"));
            _server.AddHandler(new StaticHandler("/css/align.css", Encoding.UTF8.GetBytes(Resources.align),
                "text/css"));

            _server.AddHandler(new StaticHandler("/js/main.js", Encoding.UTF8.GetBytes(Resources.main.Replace(" + \":22023\";", $" + \":{_config.ApiPort}\";")),
                "text/javascript"));
            _server.AddHandler(new StaticHandler("/js/blackTea.js", Encoding.UTF8.GetBytes(Resources.blackTea),
                "text/javascript"));
            _server.AddHandler(new StaticHandler("/js/chart.js", Encoding.UTF8.GetBytes(Resources.chart),
                "text/javascript"));
            _server.AddHandler(new StaticHandler("/js/chart.streaming.js",
                Encoding.UTF8.GetBytes(Resources.chart_streaming), "text/javascript"));
            _server.AddHandler(
                new StaticHandler("/js/md5.js", Encoding.UTF8.GetBytes(Resources.md5), "text/javascript"));
            _server.AddHandler(new StaticHandler("/js/moment.js", Encoding.UTF8.GetBytes(Resources.moment),
                "text/javascript"));

            _server.AddHandler(
                new StaticHandler("/font/Audiowide-Regular.ttf", Resources.Audiowide_Regular, "font/ttf"));
            _server.AddHandler(new StaticHandler("/font/AlfaSlabOne-Regular.ttf", Resources.AlfaSlabOne_Regular,
                "font/ttf"));

            #region Icons

            _server.AddHandler(new StaticHandler("/ico/apple-touch-icon-57x57.png", Resources.apple_touch_icon_57x57,
                "image/png"));
            _server.AddHandler(new StaticHandler("/ico/apple-touch-icon-60x60.png", Resources.apple_touch_icon_60x60,
                "image/png"));
            _server.AddHandler(new StaticHandler("/ico/apple-touch-icon-72x72.png", Resources.apple_touch_icon_72x72,
                "image/png"));
            _server.AddHandler(new StaticHandler("/ico/apple-touch-icon-76x76.png", Resources.apple_touch_icon_76x76,
                "image/png"));
            _server.AddHandler(new StaticHandler("/ico/apple-touch-icon-114x114.png",
                Resources.apple_touch_icon_114x114, "image/png"));
            _server.AddHandler(new StaticHandler("/ico/apple-touch-icon-120x120.png",
                Resources.apple_touch_icon_120x120, "image/png"));
            _server.AddHandler(new StaticHandler("/ico/apple-touch-icon-144x144.png",
                Resources.apple_touch_icon_144x144, "image/png"));
            _server.AddHandler(new StaticHandler("/ico/apple-touch-icon-152x152.png",
                Resources.apple_touch_icon_152x152, "image/png"));
            _server.AddHandler(new StaticHandler("/ico/favicon-16x16.png", Resources.favicon_16x16, "image/png"));
            _server.AddHandler(new StaticHandler("/ico/favicon-32x32.png", Resources.favicon_32x32, "image/png"));
            _server.AddHandler(new StaticHandler("/ico/favicon-96x96.png", Resources.favicon_96x96, "image/png"));
            _server.AddHandler(new StaticHandler("/ico/favicon-128.png", Resources.favicon_128, "image/png"));
            _server.AddHandler(new StaticHandler("/ico/favicon-196x196.png", Resources.favicon_196x196, "image/png"));
            _server.AddHandler(new StaticHandler("/ico/mstile-70x70.png", Resources.mstile_70x70, "image/png"));
            _server.AddHandler(new StaticHandler("/ico/mstile-144x144.png", Resources.mstile_144x144, "image/png"));
            _server.AddHandler(new StaticHandler("/ico/mstile-150x150.png", Resources.mstile_150x150, "image/png"));
            _server.AddHandler(new StaticHandler("/ico/mstile-310x150.png", Resources.mstile_310x150, "image/png"));
            _server.AddHandler(new StaticHandler("/ico/mstile-310x310.png", Resources.mstile_310x310, "image/png"));

            #endregion

            foreach (var password in _passwords)
            {
                _server.AddHandler(new DynamicHandler($"/players.csv?{password}", _playerList.CreateHttpResponseBody));
            }
        }
    }
}