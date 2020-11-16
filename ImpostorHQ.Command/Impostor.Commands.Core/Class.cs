using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
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
using Impostor.Api.Innersloth;
using Impostor.Api.Net;
using Impostor.Api.Net.Manager;
using Microsoft.Extensions.Logging;
using Impostor.Commands.Core.DashBoard;
using Impostor.Commands.Core.SELF;

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
        public string ChatConfigPath = Path.Combine("configs", "playerCommands.cfg");

        public string BanFolder = Path.Combine("configs", "bans");
        //the dynamically loaded configuration.
        public Structures.PluginConfiguration Configuration { get; private set; }
        public Structures.PlayerCommandConfiguration ChatCommandCfg { get; private set; }
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
        public IClientManager ClientManager { get; set; }

        //change this externally if you want to.
        public Action<IGame, string, Structures.BroadcastType> ExternalCallback;
        private string[] KnownColors { get; set; }
        //our lovely log manager!
        public SpatiallyEfficientLogFileManager LogManager { get; private set; }
        public AnnouncementServer AnnouncementManager { get; private set; }
        #endregion
        /// <summary>
        /// This constructor will be 'injected' with the required references by the plugin API.
        /// </summary>
        /// <param name="logger">The global server logger.</param>
        /// <param name="manager">The global event manager.</param>
        /// <param name="gameManager">The global game manager.</param>
        /// <param name="provider">A provider for MessageWriters (not used yet).</param>
        public Class(ILogger<Class> logger, IEventManager manager, IGameManager gameManager,IMessageWriterProvider provider,IClientManager clientManager)
        {
            this.GameManager = gameManager;
            this.Logger = logger;
            this.MessageWriterProvider = provider;
            this.EventManager = manager;
            this.ClientManager = clientManager;
            this.Options = new ParallelOptions();
            Options.MaxDegreeOfParallelism = Environment.ProcessorCount;
            KnownColors = Enum.GetNames(typeof(System.Drawing.KnownColor));
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
            Running = false;
            Logger.LogInformation("Server Commands shutting down.");
            ApiServer.Shutdown();
            DashboardServer.Shutdown();
            Configuration.SaveTo(ConfigPath);
            ChatCommandCfg.SaveTo(ChatConfigPath);
            LogManager.Finish();
            AnnouncementManager.Shutdown();
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


            if (!File.Exists(ChatConfigPath))
            {
                //first time run. Create a new configuration.
                var cfg = Structures.PlayerCommandConfiguration.GetDefaultConfig();
                cfg.SaveTo(ChatConfigPath);
                this.ChatCommandCfg = cfg;
                //we need to create some keys.
                Logger.LogInformation($"ImpostorHQ : Commands enabled by default: Map changer, max player changer, max impostors changer.");
            }
            else
            {
                this.ChatCommandCfg = Structures.PlayerCommandConfiguration.LoadFrom(ChatConfigPath);
            }

            this.LogManager = new SpatiallyEfficientLogFileManager("hqlogs");
            InitializeInterfaces();
            InitializeServers();
            HighCourt = new JusticeSystem(BanFolder,ChatCommandCfg.ReportsRequiredForBan,Logger,ChatInterface,this);
            HighCourt.OnPlayerBanned += PlayerBanned;
        }

        private void InitializeInterfaces()
        {
            this.ChatInterface = new GameCommandChatInterface(MessageWriterProvider, Logger);
            this.GameEventListener = new GamePluginInterface(ChatInterface);

            if (ChatCommandCfg.EnableImpostorChange) GameEventListener.RegisterCommand(Structures.PlayerCommands.ImpostorChange);
            if (ChatCommandCfg.EnableMapChange) GameEventListener.RegisterCommand(Structures.PlayerCommands.MapChange);
            if (ChatCommandCfg.EnableMaxPlayersChange) GameEventListener.RegisterCommand(Structures.PlayerCommands.MaxPlayersChange);
            if (ChatCommandCfg.EnableReportCommand) GameEventListener.RegisterCommand(Structures.PlayerCommands.ReportCommand);

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
            this.ApiServer = new WebApiServer(Configuration.APIPort, Configuration.ListenInterface, Configuration.APIKeys.ToArray(), Logger,GameManager,this);
            this.DashboardServer = new HttpServer(Configuration.ListenInterface, Configuration.WebsitePort, ClientHTML, error404Html, errorMimeHtml,this,ApiServer);
            this.AnnouncementManager = new AnnouncementServer(this,"configs");
            ApiServer.OnMessageReceived += DashboardCommandReceived;

            ApiServer.RegisterCommand(Structures.DashboardCommands.BansMessage,"=> will list the current permanent bans.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.HelpMessage, "=> will display the help message.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.ServerWideBroadcast, " <color>:<message> => will send a message to all lobbies.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.ListColors, "=> will list all the colors accepted by the /broadcast commands.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.StatusMessage,"=> will show you some general statistics about the server.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.BanIpAddress, " <ip address> => will permanently ban the IP address. The players must be connected.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.BanIpAddressBlind," <ip address> => just like the above, but the player does not need to be connected. Warning: he will not be kicked if he is connected to a game. Use the above command if that is your intention.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.ListKeys,"=> will list all registered API keys.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.AddKey,"=> will register the selected key.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.DeleteKey,"=> will delete the API key, if it is valid.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.ReloadBans,"=> this will reload the bans from the disk. This can be useful if you need to remove a ban, and don't want to restart the server. To do that, just delete the ban from the disk and execute this command.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.PlayerInfo," <username> => this will show information about a player.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.UnBanAddress," <IP address> => this is the equivalent of deleting a ban file and reloading the bans. The said player will be unbanned from the server, if he is banned.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.ListLogs,"=> will list the logs that you can then fetch.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.FetchLog, " <file, listed by /logs> => will download the log in CSV format. They will be converted to CSV on-demand, so don't spam this command, because it will use up CPU time.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.AnnouncementMultiCommand," set <message> => will set that announcement, clear => will clear the message and delete it from the disk.");
        }

        private void PlayerBanned(string username, string IP)
        {
            ApiServer.Push($"Player {username} / {IP} was banned permanently.","reportsys",Structures.MessageFlag.ConsoleLogMessage,null);
        }

        private void GameEventListener_OnPlayerCommandReceived(string command, string data, IPlayerChatEvent source)
        {
            if (string.IsNullOrEmpty(data))
            {
                ChatInterface.SafeMultiMessage(source.Game, "Invalid command data. Please use /help for more information.", Structures.BroadcastType.Error, destination: source.ClientPlayer);
                return;
            }
            switch (command)
            {
                case Structures.PlayerCommands.ReportCommand:
                {
                    HighCourt.HandleReport(data,source);
                    break;
                }
                case Structures.PlayerCommands.MapChange:
                {
                    if (!source.ClientPlayer.IsHost)
                    {
                        ChatInterface.SafeMultiMessage(source.Game, "You are not the host.", Structures.BroadcastType.Error, destination: source.ClientPlayer);
                        break;
                    }
                    var map = data.ToLower();
                    if (!Structures.Maps.Contains(map))
                    {
                        ChatInterface.SafeMultiMessage(source.Game, "Invalid map. Accepted values: Skeld, MiraHQ, Polus.", Structures.BroadcastType.Error, destination: source.ClientPlayer);
                    }
                    else
                    {
                        var flag = MapTypes.Skeld;
                        switch (map)
                        {
                            case "polus":
                            {
                                flag = MapTypes.Polus;
                                break;
                            }
                            case "mirahq":
                            {
                                flag = MapTypes.MiraHQ;
                                break;
                            }
                        }

                        source.Game.Options.Map = flag;
                        source.Game.SyncSettingsAsync();
                    }
                    break;
                }
                case Structures.PlayerCommands.MaxPlayersChange:
                {
                    if (!source.ClientPlayer.IsHost)
                    {
                        ChatInterface.SafeMultiMessage(source.Game, "You are not the host.", Structures.BroadcastType.Error, destination: source.ClientPlayer);
                        break;
                    }
                    if (Byte.TryParse(data, out byte num))
                    {
                        if (!Structures.MaxPlayers.Contains(num))
                        {
                            ChatInterface.SafeMultiMessage(source.Game, "Invalid number of players. Accepted numbers: 10, 8, 6, 4.", Structures.BroadcastType.Error, destination: source.ClientPlayer);
                        }
                        else
                        {
                            source.Game.Options.MaxPlayers = num;
                            source.Game.SyncSettingsAsync();
                        }
                    }
                    else
                    {
                        ChatInterface.SafeMultiMessage(source.Game,"Invalid syntax. Please use \"/players <10,8,6,4>.",Structures.BroadcastType.Error,destination:source.ClientPlayer);
                    }
                    break;
                }
                case Structures.PlayerCommands.ImpostorChange:
                {
                    if (!source.ClientPlayer.IsHost)
                    {
                        ChatInterface.SafeMultiMessage(source.Game, "You are not the host.", Structures.BroadcastType.Error, destination: source.ClientPlayer);
                        break;
                    }
                    if (Byte.TryParse(data, out byte num))
                    {
                        if (!Structures.MaxImpostors.Contains(num))
                        {
                            ChatInterface.SafeMultiMessage(source.Game, "Invalid number of impostors. Accepted numbers: 3, 2, 1.", Structures.BroadcastType.Error, destination: source.ClientPlayer);
                        }
                        else
                        {
                            source.Game.Options.NumImpostors = num;
                            source.Game.SyncSettingsAsync();
                        }
                    }
                    else
                    {
                        ChatInterface.SafeMultiMessage(source.Game, "Invalid syntax. Please use \"/impostors <3,2,1>.", Structures.BroadcastType.Error, destination: source.ClientPlayer);
                    }
                    break;
                }
            }
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
                if (message.Text.Contains(" "))
                {
                    //we process the command to see if it is complex.
                    //we now separate the data.
                    cmd = new string(message.Text.Take(message.Text.IndexOf(' ')).ToArray());
                    //we now extract the data.
                    message.Text = message.Text.Remove(0, cmd.Length + 1); //remove ' '
                }
                else
                {
                    //our command is simple.
                    cmd = message.Text;
                    isSingle = true;
                }

                if (isSingle)
                {
                    LogManager.LogDashboard(IPAddress.Parse(client.ConnectionInfo.ClientIpAddress), $"\"{cmd}\"");
                }
                else
                {
                    LogManager.LogDashboard(IPAddress.Parse(client.ConnectionInfo.ClientIpAddress),
                        $"{cmd} {message.Text}");
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

                        isSingle = true; //we may get errors.
                        if (!message.Text.Contains(':'))
                        {
                            ApiServer.PushTo("Invalid structure. Please use: /broadcast <color>:<message>",
                                Structures.ServerSources.DebugSystem, Structures.MessageFlag.ConsoleLogMessage, client);
                            break;
                        }

                        var sides = message.Text.Split(':');
                        if (string.IsNullOrEmpty(sides[0]) || string.IsNullOrEmpty(sides[1]))
                        {
                            ApiServer.PushTo(
                                "Please specify a color and a message. Example: /broadcast green:Greetings to all players!",
                                Structures.ServerSources.DebugSystem, Structures.MessageFlag.ConsoleLogMessage, client);
                            break;
                        }

                        if (!KnownColors.Contains(sides[0]))
                        {
                            ApiServer.PushTo("Unknown color. Please use /broadcastcolors to list all colors.",
                                Structures.ServerSources.DebugSystem, Structures.MessageFlag.ConsoleLogMessage, client);
                            break;
                        }


                        isSingle =
                            false; //no errors. That means we need to inform the user that the broadcast is running.
                        lock (GameManager.Games)
                        {
                            //we broadcast to all games.
                            //to do this, we compile a list, then broadcast the message in parallel.
                            Task[] tasks = new Task[GameManager.Games.Count()];
                            if (tasks.Length == 0) break; //no lobbies.
                            int index = 0;
                            foreach (var game in GameManager.Games)
                            {
                                var col = ParseColor(sides[0]);
                                tasks[index] = ExternalCallback == null
                                    ? ChatInterface.SafeAsyncBroadcast(game, col + sides[1],
                                        Structures.BroadcastType.Manual)
                                    : new Task(() =>
                                        ExternalCallback(game, sides[1], Structures.BroadcastType.Information));
                            }

                            //if this does not return, our server is not working.
                            var t = new Thread(() => ParallelExecuteBroadcast(tasks));
                            t.Start();
                        }

                        break;
                    }
                    case Structures.DashboardCommands.ListColors:
                    {
                        if (!isSingle)
                        {
                            handled = false;
                            break;
                        }

                        string response = "Broadcast colors:\n";
                        foreach (var color in KnownColors)
                        {
                            response += $"  {color}\n";
                        }

                        ApiServer.PushTo(response, Structures.ServerSources.SystemInfo,
                            Structures.MessageFlag.ConsoleLogMessage, client);
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

                        ApiServer.PushTo(helpstr, Structures.ServerSources.SystemInfo,
                            Structures.MessageFlag.ConsoleLogMessage, client);
                        break;
                    }
                    case Structures.DashboardCommands.StatusMessage:
                    {
                        if (!isSingle)
                        {
                            handled = false;
                            break;
                        }

                        ApiServer.PushTo("Status:\n" + CompileStatus(), Structures.ServerSources.SystemInfo,
                            Structures.MessageFlag.ConsoleLogMessage, client);
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

                            ApiServer.PushTo(response, "sysinfo", Structures.MessageFlag.ConsoleLogMessage, client);
                        }

                        break;
                    }
                    case Structures.DashboardCommands.BanIpAddress:
                    {
                        if (isSingle)
                        {
                            handled = false;
                            break;
                        }

                        if (IPAddress.TryParse(message.Text, out IPAddress address))
                        {
                            handled = true;
                            isSingle = true;
                            if (HighCourt.BanPlayer(address, client.ConnectionInfo.ClientIpAddress))
                            {
                                ApiServer.Push(
                                    $"The target [{address}] has been banned permanently by {client.ConnectionInfo.ClientIpAddress}!",
                                    "(SERVER/CRITICAL/WIDE)", Structures.MessageFlag.ConsoleLogMessage);
                            }
                            else
                            {
                                ApiServer.PushTo("Could not find player.", Structures.ServerSources.DebugSystem,
                                    Structures.MessageFlag.ConsoleLogMessage, client);
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
                        if (isSingle)
                        {
                            handled = false;
                            break;
                        }

                        if (IPAddress.TryParse(message.Text, out IPAddress address))
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
                                Target = address.ToString(),
                                TargetName = "<unknown>",
                                MinutesRemaining = 0,
                                TotalReports = 0
                            };

                            HighCourt.AddPermBan(report);
                            ApiServer.Push(
                                $"The target [{address}] has been blindly banned by {client.ConnectionInfo.ClientIpAddress}!",
                                "(SERVER/CRITICAL/WIDE)", Structures.MessageFlag.ConsoleLogMessage);
                            isSingle = true;
                            handled = true;
                        }
                        else
                        {
                            handled = false;
                        }

                        break;
                    }
                    case Structures.DashboardCommands.ListKeys:
                    {
                        if (!isSingle)
                        {
                            handled = false;
                            break;
                        }

                        string response = "Api keys:\n";
                        lock (ApiServer.ApiKeys)
                        {
                            foreach (var key in ApiServer.ApiKeys)
                            {
                                response += $"  \"{key}\"\n";
                            }
                        }

                        ApiServer.PushTo(response, Structures.ServerSources.SystemInfo,
                            Structures.MessageFlag.ConsoleLogMessage, client);
                        break;
                    }
                    case Structures.DashboardCommands.AddKey:
                    {
                        if (isSingle)
                        {
                            handled = false;
                            break;
                        }

                        lock (ApiServer.ApiKeys)
                        {
                            if (!AddApiKey(message.Text))
                            {
                                ApiServer.PushTo("Cannot add key: the key already exists.",
                                    Structures.ServerSources.DebugSystem, Structures.MessageFlag.ConsoleLogMessage,
                                    client);
                                isSingle = true; //we inhibit it from sending 'Command executed successfully'
                            }
                            else
                            {
                                isSingle = true; //we inhibit it from sending 'Command executed successfully'
                                ApiServer.PushTo($"The key \"{message.Text}\" is now valid and can be used.",
                                    Structures.ServerSources.CommandSystem, Structures.MessageFlag.ConsoleLogMessage,
                                    client);
                            }
                        }

                        break;
                    }
                    case Structures.DashboardCommands.DeleteKey:
                    {
                        if (isSingle)
                        {
                            handled = false;
                            break;
                        }

                        lock (ApiServer.ApiKeys)
                            if (ApiServer.ApiKeys.Count == 1)
                            {
                                isSingle = true;
                                ApiServer.PushTo(
                                    $"The key \"{message.Text}\" cannot be removed, because it is the only remaining key. In order to remove it, please add another key, then execute this command.",
                                    Structures.ServerSources.CommandSystem, Structures.MessageFlag.ConsoleLogMessage,
                                    client);
                                break;
                            }

                        if (RemoveKey(message.Text))
                        {
                            isSingle = true;
                            ApiServer.ApiKeys.Remove(message.Text);
                            ApiServer.PushTo($"The key \"{message.Text}\" is now gone.",
                                Structures.ServerSources.CommandSystem, Structures.MessageFlag.ConsoleLogMessage,
                                client);
                        }
                        else
                        {
                            isSingle = true;
                            ApiServer.PushTo($"Cannot delete the key \"{message.Text}\". It is not registered.",
                                Structures.ServerSources.CommandSystem, Structures.MessageFlag.ConsoleLogMessage,
                                client);
                        }

                        break;
                    }
                    case Structures.DashboardCommands.ReloadBans:
                    {
                        if (!isSingle)
                        {
                            handled = false;
                            break;
                        }

                        isSingle = false; //we need to show that it executed.
                        HighCourt.ReloadBans();
                        break;

                    }
                    case Structures.DashboardCommands.PlayerInfo:
                    {
                        if (isSingle)
                        {
                            handled = false;
                            break;
                        }

                        StringBuilder response = new StringBuilder();
                        response.Append("Players matching request: ");
                        bool matches = false;
                        foreach (var player in GetPlayers())
                        {
                            if (player.Player == null) continue;
                            if (player.Player.Character.PlayerInfo.PlayerName.Contains(message.Text,
                                StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (!matches)
                                {
                                    matches = true;
                                    response.Append("\n");
                                }

                                response.AppendLine(
                                    $"  {player.Player.Character.PlayerInfo.PlayerName}, Address: {player.Player.Client.Connection.EndPoint.Address}:");
                                response.AppendLine($"      Lobby: {player.Player.Game.Code.Code}");
                                response.AppendLine($"      Dead: {player.Player.Character.PlayerInfo.IsDead}");
                                response.AppendLine($"      Impostor: {player.Player.Character.PlayerInfo.IsImpostor}");
                                response.AppendLine(
                                    $"      Color: {(Structures.PlayerColor) player.Player.Character.PlayerInfo.ColorId}");
                                response.AppendLine(
                                    $"      Hat: {(Structures.HatId) player.Player.Character.PlayerInfo.HatId}");
                                response.AppendLine(
                                    $"      Pet: {(Structures.PetId) player.Player.Character.PlayerInfo.PetId}");
                                response.AppendLine(
                                    $"      Skin: {(Structures.SkinId) player.Player.Character.PlayerInfo.SkinId}");
                            }
                        }

                        if (!matches) response.Append("No matches.");
                        ApiServer.PushTo(response.ToString(), Structures.ServerSources.SystemInfo,
                            Structures.MessageFlag.ConsoleLogMessage, client);
                        isSingle = true;
                        break;
                    }
                    case Structures.DashboardCommands.UnBanAddress:
                    {
                        if (isSingle)
                        {
                            handled = false;
                            break;
                        }

                        isSingle = true;
                        if (HighCourt.RemoveBan(message.Text))
                        {
                            ApiServer.PushTo($"The target player has been unbanned.",
                                Structures.ServerSources.CommandSystem, Structures.MessageFlag.ConsoleLogMessage,
                                client);
                            ApiServer.Push(
                                $"A player with the IP address [{message.Text}] has been unbanned by {client.ConnectionInfo.ClientIpAddress}.",
                                Structures.ServerSources.DebugSystemCritical, Structures.MessageFlag.ConsoleLogMessage);

                        }
                        else
                        {
                            ApiServer.PushTo($"Error: the target player [{message.Text}] is not banned.",
                                Structures.ServerSources.DebugSystem, Structures.MessageFlag.ConsoleLogMessage, client);
                        }

                        break;
                    }
                    case Structures.DashboardCommands.ListLogs:
                    {
                        if (!isSingle)
                        {
                            handled = false;
                            break;
                        }

                        isSingle = true;
                        var logs = LogManager.GetLogNames();
                        if (logs == null || logs.Length == 0)
                        {
                            ApiServer.PushTo("There are no logs, which might be a server error.",
                                Structures.ServerSources.DebugSystemCritical, Structures.MessageFlag.ConsoleLogMessage,
                                client);
                            break;
                        }
                        else
                        {
                            var response = "Log dates:\n";
                            foreach (var log in logs)
                            {
                                response += Path.GetFileNameWithoutExtension(log) + '\n';
                            }

                            ApiServer.PushTo(response, Structures.ServerSources.SystemInfo,
                                Structures.MessageFlag.ConsoleLogMessage, client);
                        }

                        break;
                    }
                    case Structures.DashboardCommands.FetchLog:
                    {
                        if (isSingle)
                        {
                            handled = false;
                            break;
                        }

                        isSingle = true;
                        var logs = LogManager.GetLogNames();
                        var log = Path.Combine("hqlogs", message.Text + ".self");

                        if (!logs.Contains(log))
                        {
                            ApiServer.PushTo("", "", Structures.MessageFlag.FetchLogs, client, new float[] {0});
                        }
                        else
                        {
                            ApiServer.PushTo(message.Text + ".csv", "", Structures.MessageFlag.FetchLogs, client,
                                new float[] {1});
                        }

                        break;
                    }
                    case Structures.DashboardCommands.AnnouncementMultiCommand:
                    {
                        if (isSingle)
                        {
                            isSingle = true;
                            ApiServer.PushTo("Invalid syntax. Please use \"/announcement <action> (set ... /clear).\"! Example: \"/announcement set Greetings to all!\"",
                                Structures.ServerSources.DebugSystem, Structures.MessageFlag.ConsoleLogMessage, client);
                            break;
                        }

                        if (message.Text.StartsWith("clear"))
                        {
                            isSingle = false; //inform that it was executed.
                            AnnouncementManager.DisableAnnouncement();
                            break;
                        }
                        else if (message.Text.StartsWith("set "))
                        {
                            message.Text = message.Text.Replace("set ", "");
                            isSingle = true;
                            AnnouncementManager.SetMessage(message.Text);
                            ApiServer.PushTo($"The announcement has been set: \"{message.Text}\"!",
                                Structures.ServerSources.DebugSystem, Structures.MessageFlag.ConsoleLogMessage, client);
                        }
                        else
                        {
                            handled = false;
                        }

                        break;

                    }
                    default:
                        //a command that is not implemented or is invalid.
                        Logger.LogWarning(
                            $"{client.ConnectionInfo.ClientIpAddress} tried to execute an invalid command.");
                        handled = false;
                        break;
                }

                if (handled)
                {
                    //we don't need to inform that the command was executed, if it is a command that returns data (like /help)
                    if (!isSingle)
                        ApiServer.PushTo("Command executed successfully.", "cmdsys",
                            Structures.MessageFlag.ConsoleLogMessage, client);
                }
                else
                    ApiServer.PushTo("Invalid command.", Structures.ServerSources.DebugSystem,
                        Structures.MessageFlag.ConsoleLogMessage, client);
            }
            catch (ArgumentOutOfRangeException)
            {

            }
            catch(Exception ex)
            {
                LogManager.LogError(ex.ToString(), Shared.ErrorLocation.DashboardCommandHandler);
            }
        }

        private void ParallelExecuteBroadcast(Task[] targets)
        {
            //this should take care of some issues.
            Parallel.ForEach(targets, Options, (broadcastTask) =>
            {
                Task.Run(()=>broadcastTask);
            });
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
                if ((player == null ||
                     player.Player == null ||
                     player.Player.Client.Connection == null ||
                     !player.Player.Client.Connection.IsConnected ||
                     string.IsNullOrEmpty(player.Player.Character.PlayerInfo.PlayerName))) continue;
                sb.Append(player.Player.Character.PlayerInfo.PlayerName);
                sb.Append(',');
                sb.Append(player.Player.Client.Connection.EndPoint.Address);
                sb.Append(',');
                sb.Append(player.Player.Game.Code.Code);
                sb.Append(',');
                sb.Append("Host: ");
                sb.Append(player.Player.IsHost);
                sb.Append("\r\n");
            }

            return sb.ToString();
        }

        public IEnumerable<IClient> GetPlayers()
        {
            var tempList = ClientManager.Clients.ToList();
            foreach (var client in tempList) if (client != null && client.Connection != null) yield return client;
        }
        
        public bool AddApiKey(string key)
        {
            if (ApiServer.CheckKey(key))
            {
                //key already exists.
                return false;
            }
            else
            {
                ApiServer.AddKey(key);
                Configuration.APIKeys.Add(key);
                Configuration.SaveTo(ConfigPath);
                return true;
            }
        }

        public bool RemoveKey(string key)
        {
            if (!ApiServer.CheckKey(key))
            {
                return false;
            }
            else
            {
                ApiServer.RemoveKey(key);
                Configuration.APIKeys.Remove(key);
                Configuration.SaveTo(ConfigPath);
                return true;
            }
        }

        private string ParseColor(string input)
        {
            var c = Color.FromName(input);
            return $"[{c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2")}FF]"; //the opacity is hardcoded, it is not needed.
        }
    }
}