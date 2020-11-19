using System;
using Impostor.Commands.Core;
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
            PluginBase.ChatInterface.OnCommandInvoked += (command, data, player) =>
            {
                if (command.Equals("/test"))
                {
                    PluginBase.ChatInterface.SafeMultiMessage(player.Game, "Greetings!", Structures.BroadcastType.Information, destination: player.ClientPlayer);
                }
            };
        }
        public void Destroy()
        {
            PluginBase.UnsafeDirectReference.ConsolePluginStatus("Test plugin shutting down...");
        }
    }
}
