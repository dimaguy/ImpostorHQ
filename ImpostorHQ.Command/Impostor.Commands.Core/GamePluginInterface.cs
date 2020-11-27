using Impostor.Api.Events;
using Impostor.Api.Events.Player;

namespace Impostor.Commands.Core
{
    public class GamePluginInterface : IEventListener
    {
        public GameCommandChatInterface Utils{ get; set; }
        /// <summary>
        /// This is our event handler. It is an interface for interacting with the game.
        /// </summary>
        /// <param name="chatInterface"></param>
        public GamePluginInterface(GameCommandChatInterface chatInterface)
        {
            Utils = chatInterface;
            Utils.OnCommandInvoked += PlayerCommandCalled;
        }
        /// <summary>
        /// This will register a player command.
        /// </summary>
        /// <param name="command">The command to register. It must start with '/' (forward slash).</param>
        public void RegisterCommand(string command)
        {
            Utils.RegisterCommand(command);
        }

        private void PlayerCommandCalled(string command, string data, IPlayerChatEvent source)
        {
            OnPlayerCommandReceived?.Invoke(command, data, source);
        }

        #region Impostor Hooks
        [EventListener(Priority = EventPriority.Normal)]
        public void OnPlayerChat(IPlayerChatEvent evt)
        {
            Utils.ParseCommand(evt);
        }
        [EventListener(Priority = EventPriority.Highest)]
        public void OnPlayerSpawned(IPlayerSpawnedEvent evt)
        {
            OnPlayerSpawnedFirst?.Invoke(evt);
        }
        [EventListener]
        public void OnPlayerDestroyed(IPlayerDestroyedEvent evt)
        {
            OnPlayerLeft?.Invoke(evt);
        }
        [EventListener]
        public void OnGameCreated(IGameCreatedEvent evt)
        {
            OnLobbyCreated?.Invoke(evt);
        }
        [EventListener]
        public void OnGameDestroyed(IGameDestroyedEvent evt)
        {
            OnLobbyTerminated?.Invoke(evt);
        }
        [EventListener]
        public void OnGameStarted(IGameStartedEvent evt)
        {
            OnLobbyStarted?.Invoke(evt);
        }
        #endregion

        #region Events
        public delegate void DelPlayerCommandReceived(string command, string data, IPlayerChatEvent source);
        public delegate void DelPlayerSpawned(IPlayerSpawnedEvent evg);
        public delegate void DelPlayerDestroyed(IPlayerDestroyedEvent evt);
        public delegate void DelGameCreated(IGameCreatedEvent evt);
        public delegate void DelGameDestroyed(IGameDestroyedEvent evt);
        public delegate void DelGameStarted(IGameStartedEvent evt);

        /// <summary>
        /// This is called when a registered command is invoked.
        /// </summary>
        public event DelPlayerCommandReceived OnPlayerCommandReceived;
        /// <summary>
        /// This is called when a player spawns.
        /// </summary>
        public event DelPlayerSpawned OnPlayerSpawnedFirst;
        /// <summary>
        /// This is called when a player is destroyed.
        /// </summary>
        public event DelPlayerDestroyed OnPlayerLeft;
        /// <summary>
        /// This is called when a lobby is created.
        /// </summary>
        public event DelGameCreated OnLobbyCreated;
        /// <summary>
        /// This is called when a lobby is destroyed.
        /// </summary>
        public event DelGameDestroyed OnLobbyTerminated;
        /// <summary>
        /// This is called when a game starts.
        /// </summary>
        public event DelGameStarted OnLobbyStarted;
        #endregion
    }
}
