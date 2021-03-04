using System;
using Impostor.Api.Net;
using Impostor.Commands.Core;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using Impostor.Commands.Core.QuantumExtensionDirector;
using Microsoft.Extensions.Logging;

namespace Plugin.Test
{
    public class MainClass : IPlugin
    {
        public uint HqVersion => 4;
        public string Name => "Test / Example plugin.";
        public string Author => "anti";
        public QuiteExtendableDirectInterface PluginBase { get; private set; }
        public void Load(QuiteExtendableDirectInterface reference,PluginFileSystem pfs)
        {
            this.PluginBase = reference;
            MyFunction();
        }
        public Dictionary<IClientPlayer,IClientPlayer> HijackedPlayers = new Dictionary<IClientPlayer, IClientPlayer>();
        private void MyFunction()
        {
            PluginBase.UnsafeDirectReference.ConsolePluginStatus("Greetings from the test plugin! Warning: This should not be used in production environments!");
            PluginBase.ApiServer.RegisterCommand("/greet", "=> A test command, added by the test plugin.");
            //this is a 'low level' hook, showing how to make your own handler.
            //Please use the default OnDashboardCommandReceived event if your structure corresponds to the default one.
            PluginBase.ApiServer.OnMessageReceived += (message, source) =>
            {
                if (message.Text.Equals("/greet"))
                {
                    PluginBase.ApiServer.PushTo("Hi there! This is the test plugin.", 
                        "TestPlugin", Structures.MessageFlag.ConsoleLogMessage, source);
                }
            };
            PluginBase.ChatInterface.RegisterCommand("/test");
            PluginBase.ChatInterface.RegisterCommand("/slap");
            PluginBase.ChatInterface.RegisterCommand("/kill");
            PluginBase.ChatInterface.RegisterCommand("/speed");
            PluginBase.ChatInterface.RegisterCommand("/freeze");
            PluginBase.ChatInterface.OnCommandInvoked += async (command, data, player) =>
            {
                if (command.Equals("/test"))
                {
                    PluginBase.ChatInterface.SafeMultiMessage(player.Game, "Greetings!", Structures.BroadcastType.Information, destination: player.ClientPlayer);
                }
                else if(command.Equals("/slap"))
                {
                    if(player.ClientPlayer.Character == null) return;
                    if (!data.Contains(','))
                    {
                        PluginBase.ChatInterface.SafeMultiMessage(player.Game, "Usage: /slap x,y", Structures.BroadcastType.Information, destination: player.ClientPlayer);
                        return;
                    }
                    var tk = data.Split(',');
                    if (!float.TryParse(tk[0], out float x)) return;
                    if (!float.TryParse(tk[1], out float y)) return;
                    await player.ClientPlayer.Character.NetworkTransform.SnapToAsync(new System.Numerics.Vector2(x, y)).ConfigureAwait(false);
                }
                else if (command.Equals("/kill"))
                {
                    if (string.IsNullOrEmpty(data)) return;
                    data = data.ToLower();

                    foreach (var clientPlayer in player.Game.Players)
                    {
                        if(clientPlayer.Character==null) continue;
                        if (clientPlayer.Character.PlayerInfo.PlayerName.ToLower().Equals(data))
                        {
                            await clientPlayer.Character.MurderPlayerAsync(clientPlayer.Character).ConfigureAwait(false);
                            return;
                        }
                    }
                    PluginBase.ChatInterface.SafeMultiMessage(player.Game, "Player not found.", Structures.BroadcastType.Information, destination: player.ClientPlayer);

                }
                else if (command.Equals("/speed"))
                {
                    if (player == null || player.ClientPlayer.Character == null|| player.ClientPlayer.Client.Connection==null) return;
                    var options = player.Game.Options;
                    var before = player.Game.Options.PlayerSpeedMod;
                    options.PlayerSpeedMod = 0.000001f;
                    var packet = PluginBase.ChatInterface.Generator.GenerateDataPacket(options, player.Game.Code.Value,
                        player.ClientPlayer.Character.NetId);
                    options.PlayerSpeedMod = before;
                    await player.ClientPlayer.Client.Connection.SendAsync(packet).ConfigureAwait(false);

                }
                else if (command.Equals("/freeze"))
                {
                    if (player == null || player.ClientPlayer.Character == null || player.ClientPlayer.Client.Connection == null) return;
                    var options = player.Game.Options;
                    var before = player.Game.Options.PlayerSpeedMod;
                    options.PlayerSpeedMod = 0f;
                    var packet = PluginBase.ChatInterface.Generator.GenerateDataPacket(options, player.Game.Code.Value,
                        player.ClientPlayer.Character.NetId);
                    options.PlayerSpeedMod = before;
                    await player.ClientPlayer.Client.Connection.SendAsync(packet).ConfigureAwait(false);
                }
            };
        }

        public void Send(string s)
        {
            Console.WriteLine($"Test plugin: {s}");
        }
        public void Destroy()
        {
            PluginBase.UnsafeDirectReference.ConsolePluginStatus("Test plugin shutting down...");
        }
    }
}
