using System;
using System.Linq;
using Impostor.Api.Net;
using Impostor.Api.Games;
using System.Threading.Tasks;
using Impostor.Api.Net.Messages;
using System.Collections.Generic;
using Impostor.Api.Events.Player;
using Microsoft.Extensions.Logging;

namespace Impostor.Commands.Core
{
    public class GameCommandChatInterface
    {
        //
        //  a list of registered commands.
        public List<string> Commands { get; private set; }
        //  the options required for parallel parsing operations.
        private ParallelOptions ParallelOptions { get; set; }
        //  the packet generator required for forging messages.
        public Structures.PacketGenerator Generator { get; set; }
        //  the global logger.
        private ILogger Logger { get; set; }
        /// <summary>
        /// Used to create a new instance of the GameCommandChatInterface class. It is used to parse chat commands, send messages and broadcasts. The parsing operations will run in parallel, to maximize performance.
        /// </summary>
        public GameCommandChatInterface(IMessageWriterProvider provider,ILogger logger)
        {
            this.Logger = logger;
            this.Generator = new Structures.PacketGenerator(provider);
            Commands = new List<string>();
            ParallelOptions = new ParallelOptions()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };
            //this will use all cores.
        }

        /// <summary>
        /// This is used to register a command to the parser. The command must start with '/'. Warning: It will be automatically lowercased.
        /// </summary>
        /// <param name="command">The command to register. If it is already registered, it will not be duplicated.</param>
        public void RegisterCommand(string command)
        {
            if (!command.StartsWith("/")) throw new Structures.Exceptions.CommandPrefixException();
            lock (Commands)
            {
                if (!Commands.Contains(command.ToLower())) Commands.Add(command.ToLower());
            }
        }

        public string GenerateDocs()
        {
            var str = "ImpostorHQ Commands: ";
            foreach (var command in Commands)
            {
                str += $"{command} ";
            }

            return str;
        }
        #pragma warning disable
        /// <summary>
        /// This function is used to broadcast a chat message to a specific lobby.
        /// </summary>
        /// <param name="Game">The game.</param>
        /// <param name="message">The message to broadcast.</param>
        /// <param name="messageType">The type of broadcast message. This affects the color of the in-game source.</param>
        /// <param name="src">The source of the message, that will appear in-game.</param>
        public async void Broadcast(IGame game, string message, Structures.BroadcastType messageType, string src)
        {
            var colorName = string.Empty;
            switch (messageType)
            {
                case Structures.BroadcastType.Error:
                {
                    colorName += "[FF0000FF]";
                    break;
                }
                case Structures.BroadcastType.Warning:
                {
                    colorName += "[FFFF00FF]";
                    break;
                }
                case Structures.BroadcastType.Information:
                {
                    colorName += "[00FF00FF]";
                    break;
                }
                case Structures.BroadcastType.Manual:
                {
                    //the color is in the message already.
                    break;
                }

            }
            //cName disabled : possible ban source.
            message = colorName + message;
            foreach (var destination in game.Players.ToList())
            {
                if (destination.Client.Connection == null || !destination.Client.Connection.IsConnected)
                {
                    Logger.LogWarning("ImpostorHQ : Warning - a residual client has been identified.");
                    continue;
                }
                var msg = (Generator.WriteChat(
                    game.Code.Value, 
                    destination.Character.NetId,
                    destination.Character.PlayerInfo.PlayerName, 
                    new string[] {message}, src
                    ));
                using (msg) destination.Client.Connection.SendAsync(msg);
            }
        }

        #pragma warning disable CS1998
        /// <summary>
        /// This will be used to send a message to a specific player.
        /// </summary>
        /// <param name="player">The player to send to.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="source">The source of the message.</param>
        /// <param name="type">The type of the message.</param>
        public async void PrivateMessage(IClientPlayer player,string message,string source, Structures.BroadcastType type)
        {
            var coloredName = string.Empty;
            var origName = player.Character.PlayerInfo.PlayerName;

            switch (type)
            {
                //we now set the color and compile the name.
                case Structures.BroadcastType.Error:
                {
                    coloredName += "[FF0000FF]";
                    break;
                }
                case Structures.BroadcastType.Warning:
                {
                    coloredName += "[FFFF00FF]";
                    break;
                }
                case Structures.BroadcastType.Information:
                {
                    coloredName += "[00FF00FF]";
                    break;
                }
                case Structures.BroadcastType.Manual:
                {
                    //the color is in the message already.
                    break;
                }

            }
            //we now finish compiling the colored message string.
            var srcMessage = coloredName + message;
            var connection = player.Client.Connection;
            if(connection==null) return; //how?
            //we now compile the final packet.
            using (var chatMessage = Generator.WriteChat(
                    player.Game.Code, 
                    player.Character.NetId, 
                    origName, 
                    new string[] { srcMessage }, 
                    source
                )) await connection.SendAsync(chatMessage).ConfigureAwait(false);
        }
        /// <summary>
        /// This will broadcast a message, handling errors, and can be used asynchronously.
        /// </summary>
        /// <param name="game">The game to broadcast to.</param>
        /// <param name="message">The message to broadcast.</param>
        /// <param name="type">The type of broadcast.</param>
        /// <returns></returns>
        public async Task SafeAsyncBroadcast(IGame game, string message, Structures.BroadcastType type)
        #pragma warning restore CS1998
        {
            //the second try loop is for catching unknown errors.
            try
            {
                if (game == null) return;
                SafeMultiMessage(game, message, type);
            }
            catch (Exception ex)
            {
                //we have caught an unknown error. 
                Logger.LogError($"ImpostorHQ : Critical unknown error : {ex.Message}");
            }
        }

        /// <summary>
        /// This will broadcast / send a message. Only assign the last 2 parameters if you want a directional message.
        /// </summary>
        /// <param name="game">The game (required for broadcasts).</param>
        /// <param name="message">The message to send/broadcast.</param>
        /// <param name="broadcastType">The type of message.</param>
        /// <param name="source">The source player name (can be anything) of the message.</param>
        /// <param name="destination">(Only assign for directional messages) the recipient of the message.</param>
        public void SafeMultiMessage(IGame game, string message, Structures.BroadcastType broadcastType, string source = "(server)", IClientPlayer destination = null)
        {
            try
            {
                if (destination == null)
                {
                    //broadcast
                    Broadcast(game, message, broadcastType, source);
                }
                else
                {
                    //private chat message
                    PrivateMessage(destination, message, source, broadcastType);
                }
            }
            catch (Structures.Exceptions.PlayerNullException ex)
            {
                Logger.LogError($"ImpostorHQ : Critical error : {ex.Message}");
            }
        }

        public void SendMessage()
        {
            
        }
        
        #pragma warning restore

        /// <summary>
        /// Use this on any chat event. If a command is registered, the OnCommandInvoked event will be fired.
        /// </summary>
        /// <param name="evt">The chat event data.</param>
        public void ParseCommand(IPlayerChatEvent evt)
        {
            //the parser is parallel, using up all cores.
            lock (Commands)
            {
                Parallel.ForEach(Commands, ParallelOptions, (prefix, state) =>
                {
                    if (evt.Message.StartsWith(prefix))
                    {
                        var commandData = string.Empty;
                        if (evt.Message.StartsWith(prefix + ' '))
                        {
                            commandData = evt.Message.Remove(0, prefix.Length + 1);
                        }
                        OnCommandInvoked?.Invoke(prefix, commandData, evt);
                        state.Break();
                    }
                });
            }
        }

        public delegate void DelCommandInvoked(string command, string data, IPlayerChatEvent source);
        public event DelCommandInvoked OnCommandInvoked;
    }
}
