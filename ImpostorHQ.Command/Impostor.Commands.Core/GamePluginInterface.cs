using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using Impostor.Api.Events;
using Impostor.Api.Events.Player;
using Impostor.Api.Games;
using Impostor.Api.Net;
using Impostor.Api.Net.Messages;
using Microsoft.Extensions.Logging;

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

        public void OnPlayerDestroyed(IPlayerDestroyedEvent evt)
        {
            OnPlayerLeft?.Invoke(evt);
        }
        public delegate void DelPlayerCommandReceived(string command, string data, IPlayerChatEvent source);
        public delegate void DelPlayerSpawned(IPlayerSpawnedEvent evg);
        public delegate void DelPlayerDestroyed(IPlayerDestroyedEvent evt);

        public event DelPlayerCommandReceived OnPlayerCommandReceived;
        public event DelPlayerSpawned OnPlayerSpawnedFirst;
        public event DelPlayerDestroyed OnPlayerLeft;
    }
}
