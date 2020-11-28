using System;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using ImpostorHQ.Plugin.HallOfShame.HallOfShame;
using Impostor.Commands.Core.QuantumExtensionDirector;

namespace ImpostorHQ.Plugin.HallOfShame
{
    public class MainClass : IPlugin
    {
        public string Name => "Hall of Shame";

        public string Author => "anti";

        public uint HqVersion => 4;

        public QuiteExtendableDirectInterface Interface { get; private set; }
        public PluginFileSystem FileSystem { get; private set; }
        public string WebpageDir { get; private set; }
        public WebpageGenerator Generator{ get; private set; }
        public const string HttpHandler = "shame";
        public const string HighCourtPluginName = "High Court";
        public HighCourt.MainClass JusticeMain { get; private set; }
        public void Destroy()
        {
        }

        public void Load(QuiteExtendableDirectInterface reference, PluginFileSystem system)
        {
            this.Interface = reference;
            this.FileSystem = system;
            var mainThread = new Thread(Main);
            mainThread.Start();
        }

        private void Main()
        {
            Thread.Sleep(1000); //ensure that the dependencies have been loaded.
            var dependency = Interface.UnsafeDirectReference.PluginLoader.TryGetPlugin(HighCourtPluginName);
            if (!dependency.Found)
            {
                Interface.Logger.LogError("ImpostorHQ Hall of Shame : Fatal error - ImpostorHQ.Plugin.JusticeSystem was not found.");
                Environment.Exit(0);
            }
            this.JusticeMain = (HighCourt.MainClass)dependency.Instance.MainClass;
            Interface.Logger.LogInformation($"ImpostorHQ Hall of Shame : Acquired lock onto the justice plugin: {dependency.Found}, Type: {dependency.Instance.Main}, Null: {dependency.Instance.MainClass == null}");
            WebpageDir = Path.Combine(FileSystem.Store, "Webpage Partials");
            if (!Directory.Exists(WebpageDir))
            {
                Interface.Logger.LogError($"ImpostorHQ Hall of Shame : Critical - missing data directory at {WebpageDir}. Execution cannot continue.");
                Environment.Exit(0);
            }
            Init();
        }
        private void Init()
        {
            Generator = new WebpageGenerator(WebpageDir);
            Interface.DashboardServer.AddInvalidPageHandler(HttpHandler);
            Interface.DashboardServer.OnSpecialHandlerInvoked += DashboardServer_OnSpecialHandlerInvoked;
            foreach (var permanentBan in JusticeMain.HighCourt.PermanentBans)
            {
                Generator.AddBan(permanentBan);
            }

            JusticeMain.HighCourt.OnPlayerBanned += (rep) =>
            {
                Generator.AddBan(rep);
            };
            JusticeMain.HighCourt.OnPlayerPardoned += (rep) =>
            {
                Generator.RemoveReport(rep);
            };
            JusticeMain.HighCourt.OnBanRead += (rep) =>
            {
                Generator.AddBan(rep);
            };
        }

        private void DashboardServer_OnSpecialHandlerInvoked(string handler, Stream transport, string version, string address)
        {
            if (handler.Equals(HttpHandler))
            {
                byte[] data = Encoding.UTF8.GetBytes(Generator.Html.GetLatest());
                Interface.DashboardServer.WriteDocument(data,"text/html",transport);
            }
        }
    }
}
