using Impostor.Api.Net;
using Impostor.Commands.Core;
using System.Collections.Generic;
using Impostor.Commands.Core.QuantumExtensionDirector;

namespace Plugin.Test
{
    public class MainClass : IPlugin
    {
        public uint HqVersion => 1;
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
            PluginBase.UnsafeDirectReference.ConsolePluginStatus("Greetings from the test plugin!");
            PluginBase.ApiServer.RegisterCommand("/greet", "=> A test command, added by the test plugin.");
            PluginBase.ApiServer.OnMessageReceived += (message, source) =>
            {
                if (message.Text.Equals("/greet"))
                {
                    PluginBase.ApiServer.PushTo("Hi there! This is the test plugin.", "TestPlugin", Structures.MessageFlag.ConsoleLogMessage, source);
                }
            };

            PluginBase.ChatInterface.RegisterCommand("/test");
            PluginBase.ChatInterface.RegisterCommand("/slap");
            PluginBase.ChatInterface.RegisterCommand("/kill");

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
                            await clientPlayer.Character.SetMurderedAsync().ConfigureAwait(false);
                            return;
                        }
                    }
                    PluginBase.ChatInterface.SafeMultiMessage(player.Game, "Player not found.", Structures.BroadcastType.Information, destination: player.ClientPlayer);

                }
            };
        }
        public void Destroy()
        {
            PluginBase.UnsafeDirectReference.ConsolePluginStatus("Test plugin shutting down...");
        }
    }
}
