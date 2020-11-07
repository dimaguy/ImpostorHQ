using System;
using System.Collections.Generic;
using Fleck;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Impostor.Api.Net.Messages;
using Impostor.Api.Events.Player;
using Impostor.Api.Net;
using Impostor.Api.Net.Manager;
using Microsoft.Extensions.Logging;
using Impostor.Commands.Core.DashBoard;

#region Impostor API Imports
using Impostor.Api.Games;
using Impostor.Api.Plugins;
using Impostor.Api.Games.Managers;
using Impostor.Api.Events.Managers;
#endregion

namespace Impostor.Commands.Core
{
    [ImpostorPlugin("ImpostorHQ","Impostor HeadQuarters API","anti, Dimaguy","0.0.1 dev")]
    public class Class : PluginBase
    {
        #region Members
        //indicates that the plugin is active.
        public bool Running { get; private set; }
        //the main thread.
        public Thread MainThread { get; private set; }
        //the paths to the different files that we use.
        public string ConfigPath = Path.Combine("configs", "Impostor.Command.cfg");
        public string BanFolder = Path.Combine("configs", "bans");
        //the dynamically loaded configuration.
        public Structures.PluginConfiguration Configuration { get; private set; }
        //the web API server.
        public WebApiServer ApiServer { get; private set; }
        //the HTTP server, hosting the dashboard HTML client.
        public HttpServer DashboardServer { get; private set; }
        //the web client code, read from the disk.
        public string ClientHTML { get; private set; }
        //a window into the events that happen on the server and in-game.
        public GamePluginInterface GameEventListener { get; private set; }
        //a window into the lobbies that the server hosts.
        public IGameManager GameManager { get; set; }
        //the logger used to write to the console, for debugging.
        private readonly ILogger<Class> Logger;
        //provides MessageWriters. Not used for anything, yet.
        public IMessageWriterProvider MessageWriterProvider { get; private set; }
        public JusticeSystem HighCourt{ get; set; }
        public GameCommandChatInterface ChatInterface { get; set; }
        public IEventManager EventManager{ get; set; }
        private ParallelOptions Options { get; set; }
        #endregion
        /// <summary>
        /// This constructor will be 'injected' with the required references by the plugin API.
        /// </summary>
        /// <param name="logger">The global server logger.</param>
        /// <param name="manager">The global event manager.</param>
        /// <param name="gameManager">The global game manager.</param>
        /// <param name="provider">A provider for MessageWriters (not used yet).</param>
        public Class(ILogger<Class> logger, IEventManager manager, IGameManager gameManager,IMessageWriterProvider provider)
        {
            this.GameManager = gameManager;
            this.Logger = logger;
            this.MessageWriterProvider = provider;
            this.EventManager = manager;
            this.Options = new ParallelOptions();
            Options.MaxDegreeOfParallelism = Environment.ProcessorCount;
        }

        #region Impostor low-level API members.
        /// <summary>
        /// This is called when the plugin is being loaded by the server.
        /// </summary>
        /// <returns></returns>
        public override ValueTask EnableAsync()
        {
            //we enable our threads and start executing code.
            Running = true;
            MainThread = new Thread(Main);
            MainThread.Start();
            return default;
        }
        /// <summary>
        /// This is called when the plugin is being disabled.
        /// </summary>
        /// <returns></returns>
        public override ValueTask DisableAsync()
        {
            //we shut down the API, the HTTP server and save the config
            //(intended for the future, when we'll write settings from the dashboard).

            Running = false;
            Logger.LogInformation("Server Commands shutting down.");
            ApiServer.Shutdown();
            DashboardServer.Shutdown();
            Configuration.SaveTo(ConfigPath);
            return default;
        }
        #endregion
        //
        /// <summary>
        /// Our main thread. This executes all our code.
        /// </summary>
        public void Main()
        {
            if (!Directory.Exists("configs")) Directory.CreateDirectory("configs");
            //we get the configuration-->
            if (!File.Exists(ConfigPath))
            {
                //first time run. Create a new configuration.
                var cfg = Structures.PluginConfiguration.GetDefaultConfig();
                cfg.SaveTo(ConfigPath);
                this.Configuration = cfg;
                //we need to create some keys.
                Logger.LogInformation($"ImpostorHQ : Detected first run. Your API key : {cfg.APIKeys[0]}");
            }
            else
            {
                this.Configuration = Structures.PluginConfiguration.LoadFrom(ConfigPath);
            }
            InitializeInterfaces();
            InitializeServers();
            

            HighCourt = new JusticeSystem(BanFolder,10,Logger,ChatInterface,this);
            HighCourt.OnPlayerBanned += PlayerBanned;
        }

        private void InitializeInterfaces()
        {
            this.ChatInterface = new GameCommandChatInterface(MessageWriterProvider, Logger);
            this.GameEventListener = new GamePluginInterface(ChatInterface);
            //register commands : -->
            //GameEventListener.RegisterCommand(GamePluginInterface.CommandPrefixes.TestCommand);
            //GameEventListener.RegisterCommand(GamePluginInterface.CommandPrefixes.ReportCommand);
            //-> disabled for Skeld.NET

            //we are ready to start listening for game events.
            EventManager.RegisterListener(GameEventListener);
            GameEventListener.OnPlayerCommandReceived += GameEventListener_OnPlayerCommandReceived;
            GameEventListener.OnPlayerSpawnedFirst += GameEventListener_OnPlayerSpawnedFirst;
        }

        private void InitializeServers()
        {
            ClientHTML = File.ReadAllText(Path.Combine("dashboard", "client.html")).Replace("%listenport%", Configuration.APIPort.ToString());
            var error404Html = File.ReadAllText(Path.Combine("dashboard", "404.html"));
            var errorMimeHtml = File.ReadAllText(Path.Combine("dashboard", "mime.html"));
            //we initialize our servers and set up the events.
            this.ApiServer = new WebApiServer(Configuration.APIPort, Configuration.ListenInterface, Configuration.APIKeys, Logger,GameManager);
            this.DashboardServer = new HttpServer(Configuration.ListenInterface, Configuration.WebsitePort, ClientHTML, error404Html, errorMimeHtml,this);
            ApiServer.OnMessageReceived += DashboardCommandReceived;

            ApiServer.RegisterCommand(Structures.DashboardCommands.BansMessage,"=> will list the current permanent bans.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.HelpMessage, "=> will display the help message.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.ServerWideBroadcast," <your message here> => will send a message to all lobbies.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.StatusMessage,"=> will show you some general statistics about the server.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.BanIpAddress, " <ip address> => will permanently ban the IP address. The players must be connected.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.BanIpAddressBlind," <ip address> => just like the above, but the player does not need to be connected. Warning: he will not be kicked if he is connected to a game. Use the above command if that is your intention.");

        }

        private void PlayerBanned(string username, string IP)
        {
            ApiServer.Push($"Player {username} / {IP} was banned permanently.","reportsys",Structures.MessageFlag.ConsoleLogMessage,null);
        }

        private void GameEventListener_OnPlayerCommandReceived(string command, string data, IPlayerChatEvent source)
        {
            switch (command)
            {
                case GamePluginInterface.CommandPrefixes.TestCommand:
                {
                    ChatInterface.SafeMultiMessage(source.Game, $"Broad : Your test command was registered : {data}", Structures.BroadcastType.Information);
                    ChatInterface.SafeMultiMessage(source.Game, "Private : works.", Structures.BroadcastType.Information, "(server/private)", source.ClientPlayer);
                    break;
                }
                case GamePluginInterface.CommandPrefixes.ReportCommand:
                {
                    HighCourt.HandleReport(data,source);
                    break;
                }
            }
            ApiServer.Push($"Received command {{from {source.PlayerControl.PlayerInfo.PlayerName}}} : {command} [{data}]","cmdsys",Structures.MessageFlag.ConsoleLogMessage,null);
        }

        
        private void GameEventListener_OnPlayerSpawnedFirst(IPlayerSpawnedEvent evg)
        {
            HighCourt.HandleSpawn(evg);
        }
        /// <summary>
        /// This function is called when a dashboard user sends a command.
        /// </summary>
        /// <param name="message">The network transport containing the data.</param>
        /// <param name="client">The client that executed the command.</param>
        private void DashboardCommandReceived(Structures.BaseMessage message,IWebSocketConnection client)
        {
            //this indicates if the command was successfully parsed.
            bool handled = true;
            try
            {
                string cmd = string.Empty;
                //this indicates if the command is simple (e.g /help).
                //if it is, there should be no parameters.
                bool isSingle = false; 
                if(message.Text.Contains(" "))
                {
                    //we process the command to see if it is complex.
                    //we now separate the data.
                    cmd = new string(message.Text.Take(message.Text.IndexOf(' ')).ToArray());
                    //we now extract the data.
                    message.Text = message.Text.Remove(0, cmd.Length);
                }
                else
                {
                    //our command is simple.
                    cmd = message.Text;
                    isSingle = true;
                }
                switch (cmd)
                {
                    case Structures.DashboardCommands.ServerWideBroadcast:
                    {
                        if (isSingle)
                        {
                            //a broadcast command must contain some data to broadcast.
                            handled = false;
                            break;
                        }
                        lock (GameManager)
                        {
                            lock (GameManager.Games)
                            {
                                //we broadcast to all games.
                                //to do this, we compile a list, then broadcast the message in parallel.
                                Task[] tasks = new Task[GameManager.Games.Count()];
                                if (tasks.Length == 0) break;   //no lobbies.
                                int index = 0;
                                foreach (var game in GameManager.Games)
                                {
                                    tasks[index] = ChatInterface.SafeAsyncBroadcast(game, message.Text,
                                        Structures.BroadcastType.Information);
                                }
                                //if this does not return, our server is not working.
                                Task.WhenAny(tasks);
                            }
                        }
                        break;
                    }
                    case Structures.DashboardCommands.HelpMessage:
                    {
                        if (!isSingle)
                        {
                            //we actually reject data, because the user might be confused.
                            //if we accept his command, we might increase his level of confusion.
                            handled = false;
                            break;
                        }

                        var helpstr = "Dashboard commands : \n";
                        foreach (var command in ApiServer.Commands)
                        {
                            helpstr += "  " + command.Key + " " + command.Value + "\n";
                        }
                        ApiServer.PushTo(helpstr,Structures.ServerSources.SystemInfo, Structures.MessageFlag.ConsoleLogMessage, client);
                        break;
                    }
                    case Structures.DashboardCommands.StatusMessage:
                    {
                        if (!isSingle)
                        {
                            handled = false;
                            break;
                        }
                        ApiServer.PushTo("Status:\n"+CompileStatus(),Structures.ServerSources.SystemInfo, Structures.MessageFlag.ConsoleLogMessage, client);
                        break;
                    }
                    case Structures.DashboardCommands.BansMessage:
                    {
                        if (!isSingle)
                        {
                            handled = false;
                            break;
                        }

                        lock (HighCourt.PermanentBans)
                        {
                            string response = $"  Total bans : {HighCourt.PermanentBans.Count}\n";
                            if (HighCourt.PermanentBans.Count > 0)
                            {
                                //there are bans, so we add them to our message.
                                foreach (var ban in HighCourt.PermanentBans)
                                {
                                    response += "  IPA : " + ban.Target.ToString() + $" / {ban.TargetName}\n";
                                }
                            }
                            ApiServer.PushTo(response,"sysinfo", Structures.MessageFlag.ConsoleLogMessage, client);
                        }
                        break;
                    }
                    case Structures.DashboardCommands.BanIpAddress:
                    {
                        IPAddress Address;
                        message.Text = message.Text.Trim().Replace(" ", "");
                        if (IPAddress.TryParse(message.Text,out Address))
                        {
                            handled = true;
                            if (HighCourt.BanPlayer(Address, client.ConnectionInfo.ClientIpAddress))
                            {
                                ApiServer.Push($"The target [{Address} has been banned, permanently!","(SERVER/CRITICAL/WIDE)",Structures.MessageFlag.ConsoleLogMessage);
                            }
                            else
                            {
                                ApiServer.PushTo("Could not find player.",Structures.ServerSources.DebugSystem,Structures.MessageFlag.ConsoleLogMessage,client);
                            }
                        }
                        else
                        {
                            handled = false;
                        }
                        break;
                    }
                    case Structures.DashboardCommands.BanIpAddressBlind:
                    {
                        IPAddress Address;
                        message.Text = message.Text.Trim().Replace(" ", "");
                        if (IPAddress.TryParse(message.Text, out Address))
                        {
                            var report = new Structures.Report
                            {
                                Messages = new List<string>
                                {
                                    "<adminsys / " + DateTime.Now.ToString() + ">"
                                },
                                Sources = new List<string>
                                {
                                    client.ConnectionInfo.ClientIpAddress
                                },
                                Target = Address.ToString(),
                                TargetName = "<unknown>",
                                MinutesRemaining = 0,
                                TotalReports = 0
                            };

                            HighCourt.AddPermBan(report);
                            handled = true;
                        }
                        else
                        {
                            handled = false;
                        }
                        break;
                    }
                    default:
                        //a command that is not implemented or is invalid.
                        handled = false;
                        break;
                }

                if (handled)
                {
                    //we don't need to inform that the command was executed, if it is a command that returns data (like /help)
                    if (!isSingle) ApiServer.PushTo("Command executed successfully.", "cmdsys", Structures.MessageFlag.ConsoleLogMessage, client);
                }
                else ApiServer.PushTo("Invalid command.","cmdsys", Structures.MessageFlag.ConsoleLogMessage, client);
            }
            catch
            {
                //invalid messagereturn;
            }
        }
        
        /// <summary>
        /// We use this function to construct a response to the status request.
        /// </summary>
        /// <returns>The response to send back.</returns>
        private string CompileStatus()
        {
            StringBuilder sb = new StringBuilder();
            lock(HighCourt.IpReports) lock(HighCourt.PermanentBans) lock (GameManager.Games)
            {
                sb.Append($"  Total games : {GameManager.Games.Count()}\n");
                ulong players = 0; //never going to need so much...
                foreach (var game in GameManager.Games)
                {
                    foreach (var player in game.Players)
                    {
                        players++;
                    }
                }

                sb.Append($"  Players connected : {players}\n");
                sb.Append($"  Total incriminated players : {HighCourt.IpReports.Count}\n");
                int largest = 0;
                string name = null;
                foreach (var ipReport in HighCourt.IpReports)
                {
                    if (ipReport.TotalReports > largest)
                    {
                        largest = ipReport.TotalReports;
                        name = ipReport.TargetName;
                    }
                }

                if (!String.IsNullOrEmpty(name))
                {
                    sb.Append($"  Most reported address - reported {largest} times : {name}\n");
                }
                sb.Append($"  Total bans : {HighCourt.PermanentBans.Count}\n");
                return sb.ToString();
            }
        }
        /// <summary>
        /// This is used to get all the players connected to the server.
        /// </summary>
        /// <returns></returns>
        public string CompilePlayers()
        {
            var sb = new StringBuilder();
            foreach (var player in GetPlayers())
            {
                sb.Append(player.Character.PlayerInfo.PlayerName);
                sb.Append(',');
                sb.Append(player.Client.Connection.EndPoint.Address.ToString());
                sb.Append("\n");
            }
            
            return sb.ToString();
        }

        public IEnumerable<IClientPlayer> GetPlayers()
        {
            lock (GameManager.Games)
            {
                foreach (var game in GameManager.Games)
                {
                    lock (game.Players)
                    {
                        foreach (var player in game.Players)
                        {
                            yield return player;
                        }
                    }
                }
            }
        }
    }
}