using System;
using Impostor.Commands.Core;
using Impostor.Commands.Core.QuantumExtensionDirector;

namespace Plugin.Test
{
    public class MainClass : IPlugin
    {
        public uint HqVersion => 0;
        public string Name => "Test / Example plugin.";
        public string Author => "anti";
        public Class PluginBase { get; private set; }
        public void Load(Class reference)
        {
            this.PluginBase = reference;
            MyFunction();
        }

        private void MyFunction()
        {
            PluginBase.ConsolePluginStatus("Greetings from the test plugin!");
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
            PluginBase.ConsolePluginStatus("Test plugin shutting down...");
        }
    }
}
