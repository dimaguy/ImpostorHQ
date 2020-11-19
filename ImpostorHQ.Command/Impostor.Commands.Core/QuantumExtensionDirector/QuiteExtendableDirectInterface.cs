using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Impostor.Api.Events.Managers;
using Impostor.Api.Games.Managers;
using Impostor.Api.Net.Manager;
using Impostor.Api.Net.Messages;
using Impostor.Commands.Core.DashBoard;
using Microsoft.Extensions.Logging;

namespace Impostor.Commands.Core.QuantumExtensionDirector
{
    public class QuiteExtendableDirectInterface
    {
        /// <summary>
        /// A logger, to directly output to the console.
        /// </summary>
        public ILogger Logger { get; set; }
        /// <summary>
        /// The thread used by the initializer.
        /// </summary>
        public Thread MainThread { get; set; }
        /// <summary>
        /// The base configuration of ImpostorHQ.
        /// </summary>
        public Structures.PluginConfiguration BaseConfig { get; set; }
        /// <summary>
        /// The player command configuration of ImpostorHQ.
        /// </summary>
        public Structures.PlayerCommandConfiguration PlayerCommandConfig { get; set; }
        /// <summary>
        /// Web API server, that is used by dashboard clients to communicate with the server.
        /// </summary>
        public WebApiServer ApiServer { get; set; }
        /// <summary>
        /// The HTTP server that serves dashboard clients.
        /// </summary>
        public HttpServer DashboardServer { get; set; }
        /// <summary>
        /// An easy interface to the game. To implement more functions, please use the EventManager.
        /// </summary>
        public GamePluginInterface EventListener { get; set; }
        /// <summary>
        /// A way to access lobbies.
        /// </summary>
        public IGameManager GameManager { get; set; }
        /// <summary>
        /// A way to track clients.
        /// </summary>
        public IClientManager ClientManager { get; set; }
        /// <summary>
        /// An event manager. This can be used to register Impostor events on your extension.
        /// </summary>
        public IEventManager EventManager { get; set; }
        /// <summary>
        /// A provider for raw message writers. They should only be used in special cases.
        /// </summary>
        public IMessageWriterProvider MessageWriterProvider { get; set; }
        /// <summary>
        /// The ban handler.
        /// </summary>
        public JusticeSystem JusticeSystem { get; set; }
        /// <summary>
        /// An easy way to interact with in-game players, by chat.
        /// </summary>
        public GameCommandChatInterface ChatInterface { get; set; }
        /// <summary>
        /// A log manager. Please use this to log your important data and errors.
        /// </summary>
        public SpatiallyEfficientLogFileManager LogManager { get; set; }
        /// <summary>
        /// The announcement server. The 2 functions can be used to alter announcements.
        /// </summary>
        public AnnouncementServer AnnouncementServer { get; set; }
        /// <summary>
        /// The quantum player list.
        /// </summary>
        public QuodEratDemonstrandum.QuiteElegantDirectory QED { get; set; }
        /// <summary>
        /// This can be used to acquire a standard path for storing data and configs.
        /// </summary>
        public PluginFileSystem StorageProvider { get; set; }
        /// <summary>
        /// This is a direct reference to the plugin main. Please use this if the referenced object do not meet your requirements. Warning: this offers total access over ImpostorHQ. Messing with the wrong values or function could lead to unwanted errors. Please refer to the source code before accessing this.
        /// </summary>
        public Class UnsafeDirectReference { get; set; }

    }
}
