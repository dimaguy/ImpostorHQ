using System;
using System.IO;
using System.Text;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using ImpostorHQ.Plugin.HallOfShame.HallOfShame;
using Impostor.Commands.Core.QuantumExtensionDirector;

namespace ImpostorHQ.Plugin.HallOfShame
{
    public class MainClass : IPlugin
    {
        public string Name => "Hall of Shame";

        public string Author => "anti";

        public uint HqVersion => 3;

        public QuiteExtendableDirectInterface Interface { get; private set; }
        public string WebpageDir { get; private set; }
        public WebpageGenerator Generator{ get; private set; }
        public const string HttpHandler = "shame";
        public void Destroy()
        {
        }

        public void Load(QuiteExtendableDirectInterface reference, PluginFileSystem system)
        {
            this.Interface = reference;
            WebpageDir = Path.Combine(system.Store, "Webpage Partials");
            if (!Directory.Exists(WebpageDir))
            {
                reference.Logger.LogError($"ImpostorHQ Hall of Shame : Critical - missing data directory at {WebpageDir}. Execution cannot continue.");
                Environment.Exit(0);
            }
            Init();
        }

        private void Init()
        {
            Generator = new WebpageGenerator(WebpageDir);
            Interface.DashboardServer.AddInvalidPageHandler(HttpHandler);
            Interface.DashboardServer.OnSpecialHandlerInvoked += DashboardServer_OnSpecialHandlerInvoked;
            foreach (var permanentBan in Interface.JusticeSystem.PermanentBans)
            {
                Generator.AddBan(permanentBan);
            }

            Interface.JusticeSystem.OnPlayerBanned += (rep) =>
            {
                Generator.AddBan(rep);
            };
            Interface.JusticeSystem.OnPlayerPardoned += (rep) =>
            {
                Generator.RemoveReport(rep);
            };
            Interface.JusticeSystem.OnBanRead += (rep) =>
            {
                Generator.AddBan(rep);
            };
        }

        private void DashboardServer_OnSpecialHandlerInvoked(string handler, NetworkStream transport, string version, string address)
        {
            if (handler.Equals(HttpHandler))
            {
                byte[] data = Encoding.UTF8.GetBytes(Generator.Html.GetLatest());
                Interface.DashboardServer.WriteHeader(version,"text/html",data.Length, " 200 OK", ref transport);
                transport.Write(data,0,data.Length);
            }
        }
    }
}
