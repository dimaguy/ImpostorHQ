using System;
using System.Linq;
using Impostor.Api.Net;
using Impostor.Api.Games;
using System.Threading.Tasks;
using Impostor.Api.Net.Messages;
using Impostor.Api.Events.Player;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Impostor.Api.Innersloth.Customization;

namespace Impostor.Commands.Core
{
    public class GameCommandChatInterface
    {
        //  a list of registered commands.
        public List<string> Commands { get; private set; }
        //  the options required for parallel parsing operations.
        private ParallelOptions ParallelOptions { get; set; }
        //  the packet generator required for forging messages.
        private Structures.PacketGenerator Generator { get; set; }
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

        #pragma warning disable
        /// <summary>
        /// This function is used to broadcast a chat message to a specific lobby.
        /// </summary>
        /// <param name="Game">The game.</param>
        /// <param name="message">The message to broadcast.</param>
        /// <param name="messageType">The type of broadcast message. This affects the color of the in-game source.</param>
        /// <param name="src">The source of the message, that will appear in-game.</param>
        public async void Broadcast(IGame Game, string message, Structures.BroadcastType messageType, string src)
        {
            var client = Game.Players.FirstOrDefault();
            if (client == null)
            {
                throw new Structures.Exceptions.PlayerNullException();
            }
            //what do you think?
            var originalName = client.Character.PlayerInfo.PlayerName;
            var originalColor = client.Character.PlayerInfo.ColorId;
            var cName = string.Empty;
            switch (messageType)
            {
                case Structures.BroadcastType.Error:
                {
                    await client.Character.SetColorAsync(ColorType.Red).ConfigureAwait(false);
                    cName += "[FF0000FF]";
                    break;
                }
                case Structures.BroadcastType.Warning:
                {
                    await client.Character.SetColorAsync(ColorType.Yellow).ConfigureAwait(false);
                    cName += "[FFFF00FF]";
                    break;
                }
                case Structures.BroadcastType.Information:
                {
                    await client.Character.SetColorAsync(ColorType.Green).ConfigureAwait(false);
                    cName += "[00FF00FF]";
                    break;
                }

            }
            await client.Character.SetNameAsync(cName + $"{src}").ConfigureAwait(false);
            await client.Character.SendChatAsync(cName + message).ConfigureAwait(false);
            await client.Character.SetColorAsync(originalColor).ConfigureAwait(false);
            await client.Character.SetNameAsync(originalName).ConfigureAwait(false);
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
            /*  STRUCTURE->
             *      Get carrier player (which is also our recipient).
             *      Compile colored name and message.
             *      Change our carrier's colors to what we require.
             *      Encode the new RPC network packet, with the source being the synced carrier.
             *      Send it to the target player (which will make it appear that our fake carrier send the message).
             *      Set the player's name and color back.
             */
            //  we need this in order to color the player's name.
            var coloredName = string.Empty;
            //  the carrier will be synced with the target, and our fake message will appear
            //  to be coming from the carrier.
            //  we now store the original data of the carrier, so we can reset his values.
            var oName = player.Character.PlayerInfo.PlayerName;
            var oColor = player.Character.PlayerInfo.ColorId;
            switch (type)
            {
                //we now set the color and compile the name.
                case Structures.BroadcastType.Error:
                {
                    coloredName += "[FF0000FF]";
                    await player.Character.SetColorAsync(ColorType.Red).ConfigureAwait(false);
                    break;
                }
                case Structures.BroadcastType.Warning:
                {
                    coloredName += "[FFFF00FF]";
                    await player.Character.SetColorAsync(ColorType.Yellow).ConfigureAwait(false);
                    break;
                }
                case Structures.BroadcastType.Information:
                {
                    coloredName += "[00FF00FF]";
                    await player.Character.SetColorAsync(ColorType.Green).ConfigureAwait(false);
                    break;
                }

            }
            //we now finish compiling the colored name and set it.
            var srcName = (coloredName + $"{source}");
            await player.Character.SetNameAsync(srcName).ConfigureAwait(false);
            //we now finish compiling the colored message string.
            var srcMessage = (coloredName + message);
            var connection = player.Client.Connection;
            if(connection==null) return; //how?
            //we now compile the final packet.
            var chatMessage = Generator.WriteChat(player.Game.Code, player.Character.NetId, srcMessage);
            
            await connection.SendAsync(chatMessage).ConfigureAwait(false);
            chatMessage.Dispose();
            
            //reset the carrier player's stats.
            await player.Character.SetColorAsync(oColor).ConfigureAwait(false);
            await player.Character.SetNameAsync(oName).ConfigureAwait(false);
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
                    if (evt.Message.StartsWith(prefix + ' '))
                    {
                        var commandData = evt.Message.Remove(0, prefix.Length + 1);
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
