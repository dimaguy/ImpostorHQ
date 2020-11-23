using Impostor.Api.Events;
using Impostor.Api.Events.Player;

namespace Impostor.Commands.Core
{
    public class GamePluginInterface : IEventListener
    {
        //
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

        public void RegisterCommand(string command)
        {
            Utils.RegisterCommand(command);
        }

        private void PlayerCommandCalled(string command, string data, IPlayerChatEvent source)
        {
            OnPlayerCommandReceived?.Invoke(command, data, source);
        }

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


        public delegate void DelPlayerCommandReceived(string command, string data, IPlayerChatEvent source);
        public delegate void DelPlayerSpawned(IPlayerSpawnedEvent evg);
        public delegate void DelPlayerDestroyed(IPlayerDestroyedEvent evt);
        public delegate void DelGameCreated(IGameCreatedEvent evt);
        public delegate void DelGameDestroyed(IGameDestroyedEvent evt);
        public delegate void DelGameStarted(IGameStartedEvent evt);

        public event DelPlayerCommandReceived OnPlayerCommandReceived;
        public event DelPlayerSpawned OnPlayerSpawnedFirst;
        public event DelPlayerDestroyed OnPlayerLeft;
        public event DelGameCreated OnLobbyCreated;
        public event DelGameDestroyed OnLobbyTerminated;
        public event DelGameStarted OnLobbyStarted;
    }
}
