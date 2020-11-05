# Writing extensions
Please follow the example in order to write extensions to this project. Here are the most important points : 
## Preresequites : 
###### Main class constructor.
In order to use the APIs, you need the following:
###### `ILogger<Class> logger, IEventManager manager, IGameManager gameManager,IMessageWriterProvider provider`

Please follow the plugin example on the Impostor github page. After you have the 2 functions figured out, add the following parameters to your plugin's constructor:

```csharp
public Class(ILogger<Class> logger, IEventManager manager, IGameManager gameManager,IMessageWriterProvider provider)
{
  this.GameManager = gameManager;
  this.Logger = logger;
  this.MessageWriterProvider = provider;
  this.EventManager = manager;
}
```
###### Using the API.
Using those objects, you will be able to create an instance of each API required:

```csharp
this.ChatInterface = new GameCommandChatInterface(MessageWriterProvider, Logger);
this.GameEventListener = new GamePluginInterface(ChatInterface);
EventManager.RegisterListener(GameEventListener);
```

Now, in order to start using the APIs, you can register commands. Here is how to register player commands (commands executed in the chat of a lobby):

```csharp
GameEventListener.RegisterCommand("/command");    
```
Then, in order to receive those commands, you need to hook the event, like so:

```csharp
 GameEventListener.OnPlayerCommandReceived += MyCommandReceivedFunction;

```

The event will pass the following arguments:
`string command, string data, IPlayerChatEvent source`

Then, you can use a switch to extract your command, and then handle it.

###### Exporting data.

This project features a complete Web API, HTTP Server and Web Client. In order to use the Web API features, you must set up servers.
Here is an example of how to set up servers:
```csharp
var ClientHTML = File.ReadAllText(Path.Combine("dashboard", "client.html")).Replace("%listenport%", Configuration.APIPort.ToString());
var error404Html = File.ReadAllText(Path.Combine("dashboard", "404.html"));
var errorMimeHtml = File.ReadAllText(Path.Combine("dashboard", "mime.html"));
this.ApiServer = new WebApiServer(Configuration.APIPort, Configuration.ListenInterface, Configuration.APIKeys, Logger,GameManager);
this.DashboardServer = new HttpServer(Configuration.ListenInterface, Configuration.WebsitePort, ClientHTML, error404Html, errorMimeHtml);
```

You need to provide a client and error pages for the HTTP server. Use the provided ones, for starters.
The API server can be bound to the same port as the game server. It requires a logger, a game manager and keys, for authentication.
The dashboard HTTP server is just a simple HTTP server which handles sending the client code to browsers. It is buffering the client HTML and error pages in order to reduce overhead. All other files required by the sites are read from the disk dynamically.

Now, in order to receive commands from your dashboard clients, you need to register the commands and hook the event:

```csharp
ApiServer.RegisterCommand("/mycommand","=> my command's documentation, for /help.");
ApiServer.OnMessageReceived += MyDashboardCommandHandler;

```
Please provide a clear, short documentation.
The event passes the following arguments:
`Structures.BaseMessage message,IWebSocketConnection client`
There, you can use the message type inside a switch, and handle your command appropriately.

###### Going deeper.
In order to send messages to your dashboards, use the following functions:


`ApiServer.Push(string message,string name,string type,float[] flags = null)`
This function will send a message to all connected dashboards (if any).


`ApiServer.PushTo(string message, string name, string type, IWebSocketConnection connection)`
This function will send a message to a specific dashboard client.

Now, you might also want to send data to your players. In order to do this, please use the following functions:

`ChatInterface.SafeMultiMessage(IGame game, string message, Structures.BroadcastType broadcastType, string source = "(server)", IClientPlayer destination = null)`
The ultimate chat function. It can send broadcasts, and private messages. The last argument specifies if you are broadcasting, or sending a message to a specific client. Leaving it null will broadcast the message.


`ChatInterface.SafeAsyncBroadcast(IGame game, string message, Structures.BroadcastType type)`
This function is useful when trying to send a large number of broadcasts. It will broadcast your message to the specified game. The broadcast type will set the color of the message.

`Broadcast(IGame Game, string message, Structures.BroadcastType messageType, string src)`
This will broadcast your message to the specified game.

`PrivateMessage(IClientPlayer player,string message,string source, Structures.BroadcastType type)`
This will send a message to a specific player. It's implementation is more complex than the rest, because it is forging a packet manually.


## The ban system.
The ban system is an example extension. It handles reports, and hooks the command `/report hacking <player name> '<Reason for the report.>'`. It has a simple disk database, and has an event which is triggered when a player is permanently banned. Now, the bans are IP Address based, so they may be deleted once they get old, because, most of the time, addresses are dynamic.
Please refer to the source code for more information.


# The client
The cient is a web application, written in HTML/JS/CSS. It is delivered by the integrated HTTP server (`antiHTTP`), and is responsable for communication with the WebAPI. You may tweak the code to your needs. It provides an administration console, for sending commands and viewing them, some tables for general monitoring, and graphs for server load. 
