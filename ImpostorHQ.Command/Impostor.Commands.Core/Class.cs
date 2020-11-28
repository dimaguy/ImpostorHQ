#region Namespaces
using Fleck;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Drawing;
using Impostor.Api.Net;
using System.Threading;
using System.Threading.Tasks;
using Impostor.Api.Innersloth;
using Impostor.Api.Net.Manager;
using Impostor.Api.Net.Messages;
using Impostor.Api.Events.Player;
using System.Collections.Generic;
using Impostor.Commands.Core.SELF;
using Microsoft.Extensions.Logging;
using Impostor.Commands.Core.DashBoard;
using Impostor.Commands.Core.QuantumExtensionDirector;
#endregion

#region Impostor API Imports
using Impostor.Api.Games;
using Impostor.Api.Plugins;
using Impostor.Api.Games.Managers;
using Impostor.Api.Events.Managers;
#endregion

namespace Impostor.Commands.Core
{
    [ImpostorPlugin("ImpostorHQ","Impostor HeadQuarters API","anti, Dimaguy","0.0.7 stable")]
    public class Class : PluginBase
    {
        // this is used by the plugin loader.
        public const int PluginApiVersion = 4;

        #region Members
        //indicates that the plugin is active.
        public bool Running { get; private set; }
        //the main thread.
        public Thread MainThread { get; private set; }
        //the paths to the different files that we use.
        public string ConfigPath = Path.Combine("configs", "Impostor.Command.cfg");
        public string ChatConfigPath = Path.Combine("configs", "playerCommands.cfg");
        public const string PluginFolderPath = "hqplugins";
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
        public QuodEratDemonstrandum.QuiteElegantDirectory QED{ get; private set; }
        public PluginLoader PluginLoader { get; private set; }
        public QuiteExtendableDirectInterface QEDi { get; set; }
        public QuiteEffectiveDetector QEDetector { get; private set; }
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
            this.QED = new QuodEratDemonstrandum.QuiteElegantDirectory();
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
            QED.Shutdown();
            PluginLoader.Shutdown();
            QEDetector.Shutdown();
            return default;
        }
        #endregion
        
        /// <summary>
        /// Our main thread. This executes all our code.
        /// </summary>
        public void Main()
        {
            if (!Directory.Exists("configs")) Directory.CreateDirectory("configs");
            if (!Directory.Exists(PluginFolderPath)) Directory.CreateDirectory(PluginFolderPath);
            //we get the configuration-->
            if (!File.Exists(ConfigPath))
            {
                //first time run. Create a new configuration.
                var cfg = Structures.PluginConfiguration.GetDefaultConfig();
                cfg.SaveTo(ConfigPath);
                this.Configuration = cfg;
                //we need to create some keys.
                Logger.LogInformation($"ImpostorHQ : Detected first run. Your API key : {cfg.ApiKeys[0]}");
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
            QEDetector = new QuiteEffectiveDetector(250);
            InitializeInterfaces();
            InitializeServers();
            //after we initialize everything, we can load the plugins.
            QEDi = new QuiteExtendableDirectInterface(this)
            {
                Logger = Logger,
                MainThread = MainThread,
                BaseConfig = Configuration,
                PlayerCommandConfig = ChatCommandCfg,
                ApiServer = ApiServer,
                DashboardServer = DashboardServer,
                EventListener = GameEventListener,
                GameManager = GameManager,
                ClientManager = ClientManager,
                EventManager = EventManager,
                MessageWriterProvider = MessageWriterProvider,
                ChatInterface = ChatInterface,
                LogManager = LogManager,
                AnnouncementServer = AnnouncementManager,
                QED = QED,
                QEDetector = QEDetector,
                UnsafeDirectReference = this
            };
            this.PluginLoader = new PluginLoader(PluginFolderPath, QEDi, PluginApiVersion);
            PluginLoader.LoadPlugins();
        }
        /// <summary>
        /// This will initialize the game-plugin interfaces.
        /// </summary>
        private void InitializeInterfaces()
        {
            this.ChatInterface = new GameCommandChatInterface(MessageWriterProvider, Logger);
            this.GameEventListener = new GamePluginInterface(ChatInterface);

            if (ChatCommandCfg.EnableImpostorChange) GameEventListener.RegisterCommand(Structures.PlayerCommands.ImpostorChange);
            if (ChatCommandCfg.EnableMapChange) GameEventListener.RegisterCommand(Structures.PlayerCommands.MapChange);
            if (ChatCommandCfg.EnableMaxPlayersChange) GameEventListener.RegisterCommand(Structures.PlayerCommands.MaxPlayersChange);
            GameEventListener.RegisterCommand(Structures.PlayerCommands.Help);

            EventManager.RegisterListener(GameEventListener);
            GameEventListener.OnPlayerCommandReceived += GameEventListener_OnPlayerCommandReceived;
            GameEventListener.OnPlayerSpawnedFirst += GameEventListener_OnPlayerSpawnedFirst;
            GameEventListener.OnPlayerLeft += GameEventListener_OnPlayerLeft;
        }
        /// <summary>
        /// This will initialize the 2 servers.
        /// </summary>
        private void InitializeServers()
        {
            ClientHTML = File.ReadAllText(Path.Combine("dashboard", "client.html")).Replace("%listenport%", Configuration.WebApiPort.ToString());
            var error404Html = File.ReadAllText(Path.Combine("dashboard", "404.html"));
            var errorMimeHtml = File.ReadAllText(Path.Combine("dashboard", "mime.html"));
            //we initialize our servers and set up the events.
            CertificateAuthority.CertificateSynthesizer synth = new CertificateAuthority.CertificateSynthesizer();
            this.ApiServer = new WebApiServer(Configuration.WebApiPort, Configuration.ListenInterface, Configuration.ApiKeys.ToArray(), Logger,GameManager,this,QEDetector,Configuration.UseSsl,synth.GetHttpsCert("anti.the-great.exterminator"));
            this.DashboardServer = new HttpServer(Configuration.ListenInterface, Configuration.WebsitePort, ClientHTML, error404Html, errorMimeHtml,this,ApiServer,QEDetector, Configuration.UseSsl, synth.GetHttpsCert("anti.the-great.exterminator"));
            Logger.LogInformation($"ImpostorHQ : API Server listening on: {Configuration.ListenInterface}:{Configuration.WebApiPort}. Dashboard listening on: {Configuration.ListenInterface}:{Configuration.WebsitePort}/client.html");
            this.AnnouncementManager = new AnnouncementServer(this,"configs");
            ApiServer.OnMessageReceived += DashboardCommandReceived;

            ApiServer.RegisterCommand(Structures.DashboardCommands.HelpMessage, "=> will display the help message.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.ServerWideBroadcast, " <color>:<message> => will send a message to all lobbies.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.ListColors, "=> will list all the colors accepted by the /broadcast commands.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.ListKeys,"=> will list all registered API keys.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.AddKey,"=> will register the selected key.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.DeleteKey,"=> will delete the API key, if it is valid.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.PlayerInfo," <username> => this will show information about a player.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.ListLogs,"=> will list the logs that you can then fetch.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.FetchLog, " <file, listed by /logs> => will download the log in CSV format. They will be converted to CSV on-demand, so don't spam this command, because it will use up CPU time.");
            ApiServer.RegisterCommand(Structures.DashboardCommands.AnnouncementMultiCommand," set <message> => will set that announcement, clear => will clear the message and delete it from the disk.");
        }
       
        /// <summary>
        /// This is the default player command handler, which also handles commands for plugins.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <param name="data">The optional data that the command may require.</param>
        /// <param name="source">The sender.</param>
        private void GameEventListener_OnPlayerCommandReceived(string command, string data, IPlayerChatEvent source)
        {
            if (command.Equals(Structures.PlayerCommands.Help))
            {
                ChatInterface.SafeMultiMessage(source.Game, ChatInterface.GenerateDocs(), Structures.BroadcastType.Information, destination: source.ClientPlayer);
                return;
            }
            
            switch (command)
            {
                case Structures.PlayerCommands.MapChange:
                {
                    if (string.IsNullOrEmpty(data))
                    {
                        ChatInterface.SafeMultiMessage(source.Game, "Invalid command data. Please use /help for more information.", Structures.BroadcastType.Error, destination: source.ClientPlayer);
                        return;
                    }
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
                    if (string.IsNullOrEmpty(data))
                    {
                        ChatInterface.SafeMultiMessage(source.Game, "Invalid command data. Please use /help for more information.", Structures.BroadcastType.Error, destination: source.ClientPlayer);
                        return;
                    }
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
                    if (string.IsNullOrEmpty(data))
                    {
                        ChatInterface.SafeMultiMessage(source.Game, "Invalid command data. Please use /help for more information.", Structures.BroadcastType.Error, destination: source.ClientPlayer);
                        return;
                    }
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
        /// <summary>
        /// This is called whenever a player spawns. It is used by the ban handler.
        /// </summary>
        /// <param name="evt"></param>
        private void GameEventListener_OnPlayerSpawnedFirst(IPlayerSpawnedEvent evt)
        {
            QED.EntanglePlayer(evt.ClientPlayer).ConfigureAwait(false);
        }
        /// <summary>
        /// This is (possibly) called when a player leaves. It should take some load off the observer thread from the QED player list.
        /// </summary>
        /// <param name="evt">The player that left.</param>
        private void GameEventListener_OnPlayerLeft(IPlayerDestroyedEvent evt)
        {
            QED.RemoveDeadPlayer(evt.ClientPlayer);
        }
        /// <summary>
        /// This function is called when a dashboard user sends a command.
        /// </summary>
        /// <param name="message">The network transport containing the data.</param>
        /// <param name="client">The client that executed the command.</param>
        private void DashboardCommandReceived(Structures.BaseMessage message,IWebSocketConnection client)
        {
            //this indicates if the command was successfully parsed.
            bool commandHandled = true;
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
                            commandHandled = false;
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
                            commandHandled = false;
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
                            commandHandled = false;
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
                    case Structures.DashboardCommands.ListKeys:
                    {
                        if (!isSingle)
                        {
                            commandHandled = false;
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
                            commandHandled = false;
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
                            commandHandled = false;
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
                    case Structures.DashboardCommands.PlayerInfo:
                    {
                        if (isSingle)
                        {
                            commandHandled = false;
                            break;
                        }

                        StringBuilder response = new StringBuilder();
                        response.Append("Players matching request: ");
                        bool matches = false;
                        foreach (var player in GetPlayers())
                        {
                            if (player.Character == null) continue;
                            if (player.Character.PlayerInfo.PlayerName.Contains(message.Text,
                                StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (!matches)
                                {
                                    matches = true;
                                    response.Append("\n");
                                }

                                response.AppendLine(
                                    $"  {player.Character.PlayerInfo.PlayerName}, Address: {player.Client.Connection.EndPoint.Address}:");
                                response.AppendLine($"      Lobby: {player.Game.Code.Code}");
                                response.AppendLine($"      Dead: {player.Character.PlayerInfo.IsDead}");
                                response.AppendLine($"      Impostor: {player.Character.PlayerInfo.IsImpostor}");
                                response.AppendLine(
                                    $"      Color: {(Structures.PlayerColor) player.Character.PlayerInfo.ColorId}");
                                response.AppendLine(
                                    $"      Hat: {(Structures.HatId) player.Character.PlayerInfo.HatId}");
                                response.AppendLine(
                                    $"      Pet: {(Structures.PetId) player.Character.PlayerInfo.PetId}");
                                response.AppendLine(
                                    $"      Skin: {(Structures.SkinId) player.Character.PlayerInfo.SkinId}");
                            }
                        }

                        if (!matches) response.Append("No matches.");
                        ApiServer.PushTo(response.ToString(), Structures.ServerSources.SystemInfo,
                            Structures.MessageFlag.ConsoleLogMessage, client);
                        isSingle = true;
                        break;
                    }
                    case Structures.DashboardCommands.ListLogs:
                    {
                        if (!isSingle)
                        {
                            commandHandled = false;
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
                            commandHandled = false;
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
                            commandHandled = false;
                        }

                        break;

                    }
                    default:
                        OnExternalCommandInvoked?.Invoke(cmd,message.Text,isSingle,client);
                        isSingle = true; //inhibit 'Command executed successfully'.
                        break;
                    
                }

                if (commandHandled)
                {
                    //we don't need to inform that the command was executed, if it is a command that returns data (like /help)
                    if (!isSingle)
                        ApiServer.PushTo("Command executed successfully.", "cmdsys",
                            Structures.MessageFlag.ConsoleLogMessage, client);
                }
                else
                {
                    Logger.LogWarning(
                        $"{client.ConnectionInfo.ClientIpAddress} tried to execute an invalid command.");
                    ApiServer.PushTo("Invalid command.", Structures.ServerSources.DebugSystem,
                        Structures.MessageFlag.ConsoleLogMessage, client);
                }
            }
            catch (ArgumentOutOfRangeException)
            {

            }
            catch(Exception ex)
            {
                LogManager.LogError(ex.ToString(), Shared.ErrorLocation.DashboardCommandHandler);
            }
        }
        /// <summary>
        /// This is used to execute an array of tasks, on all virtual/physical cores.
        /// </summary>
        /// <param name="targets">The broadcast tasks.</param>
        private void ParallelExecuteBroadcast(Task[] targets)
        {
            //this should take care of some issues.
            Parallel.ForEach(targets, Options, (broadcastTask) =>
            {
                Task.Run(()=>broadcastTask);
            });
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
                     player.Character == null ||
                     player.Client.Connection == null ||
                     !player.Client.Connection.IsConnected ||
                     string.IsNullOrEmpty(player.Character.PlayerInfo.PlayerName))) continue;
                sb.Append(player.Character.PlayerInfo.PlayerName);
                sb.Append(',');
                sb.Append(player.Client.Connection.EndPoint.Address);
                sb.Append(',');
                sb.Append(player.Game.Code.Code);
                sb.Append(',');
                sb.Append("Host: ");
                sb.Append(player.IsHost);
                sb.Append("\r\n");
            }

            return sb.ToString();
        }
        /// <summary>
        /// This is used to get a list of players. It is completely thread safe and does not access impostor data. It uses the QED.
        /// </summary>
        /// <returns>A player list. You may use extended functions if you don't need to iterate trough it. You can use this function as many times as you like.</returns>
        public IEnumerable<IClientPlayer> GetPlayers()
        {
            foreach (var client in QED.AcquireList()) if (client != null && client.Client.Connection != null) yield return client;
        }
        /// <summary>
        /// This is used to add an API key and update the config.
        /// </summary>
        /// <param name="key">The key to add. If it is already present, this function will return.</param>
        /// <returns>True if the key is unique and has been added.</returns>
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
                Configuration.ApiKeys.Add(key);
                Configuration.SaveTo(ConfigPath);
                return true;
            }
        }
        /// <summary>
        /// This is used to remove an API key and update the config.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>True if the key was found and removed.</returns>
        public bool RemoveKey(string key)
        {
            if (!ApiServer.CheckKey(key))
            {
                return false;
            }
            else
            {
                ApiServer.RemoveKey(key);
                Configuration.ApiKeys.Remove(key);
                Configuration.SaveTo(ConfigPath);
                return true;
            }
        }
        /// <summary>
        /// This is used to parse HTML color names to Unity text renderer colors.
        /// </summary>
        /// <param name="input">The color to parse.</param>
        /// <returns>A fully formated unity color code. Beware: the opacity is hardcoded to maximum.</returns>
        private string ParseColor(string input)
        {
            var c = Color.FromName(input);
            return $"[{c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2")}FF]"; //the opacity is hardcoded, it is not needed.
        }
        /// <summary>
        /// This can be used by plugins to log data. It is just calling the log manager's LogPlugin function.
        /// </summary>
        /// <param name="sourcePluginName">The name of the plugin/an identifier to help locate the source of the message.</param>
        /// <param name="message">The message to log.</param>
        public void LogPlugin(string sourcePluginName, string message)
        {
            LogManager.LogPlugin(sourcePluginName,message);
        }
        /// <summary>
        /// This is an old function, and can be used by plugins to log warnings to the console.
        /// </summary>
        /// <param name="message"></param>
        public void ConsolePluginWarning(string message)
        {
            Logger.LogWarning("ImpostorHQ Plugin System : " + message);
        }
        /// <summary>
        /// This is an old function, and can be used by plugins to log information to the console.
        /// </summary>
        /// <param name="message"></param>
        public void ConsolePluginStatus(string message)
        {
            Logger.LogInformation("ImpostorHQ Plugin System : " + message);
        }

        public delegate void DelDashboardCommandInvoked(string command, string data, bool single,
            IWebSocketConnection source);
        /// <summary>
        /// This is called when an external player command (a command registered by a plugin) is called.
        /// </summary>
        public event DelDashboardCommandInvoked OnExternalCommandInvoked;
    }
}