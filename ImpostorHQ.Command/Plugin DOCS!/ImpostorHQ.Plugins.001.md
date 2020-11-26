# ImpostorHQ Plugin System - general concepts

Please note that this is just a general overview of the system. The future docs will cover all the remaining functions and systems.

## 1. Prerequisites

In order to write plugins for ImpostorHQ, you must implement the specific functions used by the loader. Therefore, a specific project structure is necessary. To get started, do the following:

1 - Create a .NET Core 3.1 library project.

2 - Download the code from GitHub, and add `Impostor.Commands.Core` as project references.

3 - Now, you can implement the `IPlugin` interface into your main class. Beware: you can only have one of those classes per plugin.

Example template:

```c#
public class MyClass : IPlugin
{
    public string Name => "Example";

    public string Author => "anti";

    public uint HqVersion => 3;

    public void Destroy()
    {
        
    }

    public void Load(QuiteExtendableDirectInterface reference, PluginFileSystem system)
    {
        
    }
}
```



## 2. Interacting with the player chat.

In order to interact with the player chat, for E.G creating commands, we can use the included functions, along with the included command handler.

### Sending messages and broadcasts.

The recommended way to send chat messages is by using the SafeMultiMessage function. It can either send a message to a whole lobby, or to a specific player (if a destination is specified).

Example usage:

`reference.ChatInterface.SafeMultiMessage(game,"Greetings!",Structures.BroadcastType.Information);` - will send a message to the lobby.

`         reference.ChatInterface.SafeMultiMessage(myClientPlayer.Game,"Greetings!",Structures.BroadcastType.Information, destination:myClientPlayer);` - will send a message to our player.

#### Colors and Broadcast Types.

The broadcast type enum indicates a pre-defined color. Those colors will be applied to the text of the message. The included color types are:

`Warning` - yellow in color.

`Error` - red.

`Information` - green.

You can also override the mechanism and use your own color (by assigning a value to `source`). Please refer to the source code to see how it is done.

### Hooking events.

The functions themselves are not of any use without events to trigger them. In that sense, we can hook the following default events (present in the EventListener / GamePluginInterface class):

`OnPlayerSpawnedFirst` -> will be called when a player is spawned.

`OnPlayerLeft` -> will be called when a player is destroyed.

`OnLobbyCreated` -> will be called when a lobby is created.

`OnlobbyTerminated` -> will be called when a lobby is terminated.

`OnLobbyStarted` -> will be called when a game starts.

To hook those events, we can either create a lambda method or a separate function.

Example:

```c#
reference.EventListener.OnPlayerSpawnedFirst += (eventData) =>
{
	reference.Logger.LogInformation($"{eventData.ClientPlayer.Character.PlayerInfo.PlayerName} spawned on {eventData.Game.Code.Code}");
};
```

```c#
...
reference.EventListener.OnPlayerSpawnedFirst += MyEventHandler;


public void MyEventHandler(IPlayerSpawnedEvent evt)
{
	MyReference.Logger.LogInformation($"{evt.ClientPlayer.Character.PlayerInfo.PlayerName} spawned on {evt.Game.Code.Code}");
}
```



You can also create your own event listener class, in order to implement other Impostor events. Please see the Impostor code if you intend to do that.

### Creating player commands.

In order to use the included command handler, you must register your command and then handle it. All commands must start with `/`.

Full example of creating a command and handling it.

```c#
reference.ChatInterface.RegisterCommand("/echo");
reference.ChatInterface.OnCommandInvoked += (command, data, source) =>
{
    if (command.Equals("/echo"))
    {
        reference.ChatInterface.SafeMultiMessage(source.Game,$"Echo: {data}",Structures.BroadcastType.Information,destination:source.ClientPlayer);
    }
};
```





That is about it for this section.

## 3.Interacting with the dashboard API and/or custom clients.

#### One of the most important features that this project offers is the dashboard. It connects to the server's web socket API and can interact with any part of the plugin. The syntax is very similar to that of the player commands.

### Prerequisites/general knowledge before hooking commands, sending a message to one/multiple/all dashboards or working with the network layer.

The dashboards are HTML clients that connect to the API server. In order to send messages trough the API server (therefore, to the dashboards), you can use the 2 included functions, `Push` and `PushTo`.

In order to register an API command, you must also provide documentation for it. It is used in the dynamic generation of help, expressed by the `/help` command.

The command event is only called for registered commands. It means that it will never be called if the command was handled internally. Now, the command can come from any plugin. You must properly handle it in order to avoid conflicts.

`hasData` indicates whether or not data was provided for the function. E.G if your command follows this format: `/command Some Data`, it will be true. Else (`/command`), it will be false.

#### Message flags:

The message flags indicate the type of data that is being sent or received. For E.G console commands / messages, the flag is `MessageFlag.ConsoleLogMessage`; for graph data, `Structures.MessageFlag.HeartbeatMessage`, ...

#### The message layer.

The web sockets protocol used by ImpostorHQ is based on messages. Those messages contain different types of data. The current message structure supports string data (which can also represent binary data, using base64) and numeric data (used by the graphs). Using this, you can represent any type of data you want. 

In order to send raw messages, you can use the respective function. Please understand that this is not required while developing normal plugins, but can be used if you are making a separate Web Sockets client for the API, or you want to change behavior in some unspecified way.

##### Working with messages.

You can create a message object, assign values to it, serialize and then send it. The following arguments can be assigned:

`Text` - (string) general message data.

`Type` - (string) the data type, as described earlier or as seen in the `MessageFlag` class.

`Date` - (numeric) the UNIX time epoch, obtained from the `WebApiServer.GetTime()` function.

`Name` - (string) the source system, as displayed in the dashboard console.

`Flags` - (float collection) extra numeric data. It is used by the graphs.

To send the message to a client, you can use the `IWebSocketClient`'s `Send(string)` function. You can get the string data by using `baseMessage.Serialize()`.

##### 

##### The command handler.

The included command handler can process commands in the format of `/command <data>`. Please implement your own handler, by hooking the `OnMessageReceived` function. The included handler is perfect for all types of commands that can be sent from the dashboard. In order to hook the handler, you can use `reference.OnDashboardCommandReceived`, and handle the parameters either locally or on your action.

#### Using the knowledge in order to hook a command and send messages.

It is quite easy to interact with dashboards/clients using the provided functions. Here is a complete example:

```c#
reference.ApiServer.RegisterCommand("/echo","=> a test command.");
reference.OnDashboardCommandReceived += (command, data, hasData, source) =>
{
    if (command.Equals("/echo"))
    {
        if (!hasData)
        {
            reference.ApiServer.PushTo("Please provide something to echo back.", "Echo System",
                Structures.MessageFlag.ConsoleLogMessage, source);
            return;
        }
        
        reference.ApiServer.PushTo($"Echo: {data}", "Echo System", Structures.MessageFlag.ConsoleLogMessage, source);
    }
};
```



## 4.Putting it to good use.

Now that we have a basic understanding of the plugin system, we can create a simple plugin. The following is a full example and will work as advertised.

_Please note that the `GetClientCount()` function will not be available until the 0.7 release. You must compile `Impostor.Commands.Core` yourself in order to use it. That should only be a matter of hitting the build button._

Also, note that your command will automatically be added to all `/help` handlers.

```c#
using Impostor.Commands.Core;
using Impostor.Commands.Core.QuantumExtensionDirector;

namespace Plugin.Example.Docs
{
    public class MyClass : IPlugin
    {
        public string Name => "Example";

        public string Author => "anti";

        public uint HqVersion => 3;
        
        public static string MainCommand = "/complain";
        public QuiteExtendableDirectInterface PluginBase{ get; private set; }
        
        public void Destroy()
        {
            
        }

        public void Load(QuiteExtendableDirectInterface reference, PluginFileSystem system)
        {
            this.PluginBase = reference;
            Main();
        }

        private void Main()
        {
            PluginBase.ChatInterface.RegisterCommand(MainCommand);
            PluginBase.ChatInterface.OnCommandInvoked += (command, data, source) =>
            {
                var player = source.ClientPlayer;
                if (command.Equals(MainCommand))
                {
                    if (string.IsNullOrEmpty(data))
                    {
                        PluginBase.ChatInterface.SafeMultiMessage(player.Game,"Please provide a complaint!",Structures.BroadcastType.Error,destination:player);
                        return;
                    }

                    var admins = PluginBase.ApiServer.GetClientCount();
                    if (admins > 0)
                    {
                        PluginBase.ApiServer.Push($"Complaint, from {player.Character.PlayerInfo.PlayerName}: \"{data}\"", "complaints", Structures.MessageFlag.ConsoleLogMessage);
                        PluginBase.ChatInterface.SafeMultiMessage(player.Game, $"Your complaint was sent to {admins} active admins.", Structures.BroadcastType.Information, destination: player);
                    }
                    else
                    {
                        PluginBase.ChatInterface.SafeMultiMessage(player.Game, $"Sorry, but there are no admins connected.", Structures.BroadcastType.Warning, destination: player);
                    }
                }
            };
        }
    }
}

```

 

Results:

[https://femto.pw/9u7h](https://femto.pw/9u7h)

[https://femto.pw/wmvg](https://femto.pw/wmvg)



Thank you for taking this introductory lesson. See you next time!

​																							`-anti`







