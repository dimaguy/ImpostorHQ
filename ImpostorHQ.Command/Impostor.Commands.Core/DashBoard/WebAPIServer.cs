using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fleck;
using Microsoft.Extensions.Logging;

namespace Impostor.Commands.Core.DashBoard
{
    public class WebApiServer
    {
        // A list of authenticated clients.
        #pragma warning disable IDE0044 // Add readonly modifier
        List<IWebSocketConnection> Clients = new List<IWebSocketConnection>();
        // A message that will be sent to all clients connected.
        private Structures.BaseMessage GlobalMessage { get; set; }
        // A list of accepted keys for authentication.
        private List<string> ApiKeys { get; set; }
        // The web socket server.
        private WebSocketServer Server { get; set; }
        // The global logger, to write warnings and errors to the console.
        private ILogger<Class> Logger { get; set; }
        #pragma warning restore IDE0044 // Add readonly modifier

        /// <summary>
        /// This will host an API server, that can be accessed with the given API keys.
        /// </summary>
        /// <param name="port">The port to host the server on.</param>
        /// <param name="listenInterface">The interface to bind the socket to.</param>
        /// <param name="keys">The accepted API keys.</param>
        /// <param name="logger">The global logger.</param>
        public WebApiServer(ushort port, string listenInterface,string[] keys,ILogger<Class> logger)
        {
            this.Logger = logger;
            //we initialize our objects.
            GlobalMessage = new Structures.BaseMessage();
            ApiKeys = new List<string>();
            GlobalMessage.Type = Structures.MessageFlag.ConsoleLogMessage;
            ApiKeys.AddRange(keys);
            Server = new WebSocketServer($"ws://{listenInterface}:{port}");
            //we start the listener.
            Server.Start(socket =>
            {
                //a client connects.
                socket.OnOpen += () => OnOpen(socket);
            });
        }

        /// <summary>
        /// Will push a final status update to the API clients and shut down the API server.
        /// </summary>
        public void Shutdown()
        {
            Push("Impostor server shutting down...",Structures.ServerSources.DebugSystemCritical, Structures.MessageFlag.DoKickOrDisconnect);
            Server.Dispose();
            ApiKeys.Clear();
        }
        
        /// <summary>
        /// A cliet has connected to the websocket server.
        /// </summary>
        /// <param name="conn">The client to process.</param>
        private void OnOpen(IWebSocketConnection conn)
        {
            conn.OnMessage = message =>
            {
                //we will handle AUTH and commands here.
                try
                {
                    var msg = JsonSerializer.Deserialize<Structures.BaseMessage>(message);
                    if (msg != null)
                    {
                        if (msg.Type.Equals(Structures.MessageFlag.LoginApiRequest))
                        {
                            //the client has entered an invalid key.
                            lock(ApiKeys)if (!ApiKeys.Contains(msg.Text))
                            {
                                msg.Name = "reject";
                                msg.Date = GetTime();
                                msg.Type = Structures.MessageFlag.LoginApiRejected;
                                conn.Send(JsonSerializer.Serialize(msg));
                                conn.Close();
                                //we log the issue.
                                Logger.LogWarning($"Failed log-in attempt : {conn.ConnectionInfo.ClientIpAddress} - key : {msg.Text}");
                                return;
                            }
                            lock (Clients)
                            {
                                Clients.Add(conn);
                                Logger.LogWarning($"ImpostorHQ : New web admin client : {conn.ConnectionInfo.ClientIpAddress}");
                                msg.Text = "You have successfully connected to ImpostorHQ!";
                                msg.Type = Structures.MessageFlag.LoginApiAccepted;
                                msg.Name = "welcome";
                                msg.Date = GetTime();
                                conn.Send(JsonSerializer.Serialize(msg));
                                conn.OnClose += () =>
                                {
                                    //we handle the client disconnecting.
                                    lock (Clients)
                                    {
                                        if (Clients.Contains(conn)) Clients.Remove(conn);
                                    }
                                };
                            }
                        }
                        else if (msg.Type.Equals(Structures.MessageFlag.ConsoleCommand))
                        {
                            lock (Clients)
                            {
                                if (!Clients.Contains(conn))
                                {
                                    //we are being attacked.
                                    //the client is sending commands without being logged in.
                                    conn.Close();
                                    Logger.LogWarning($"Break-in attempt from : {conn.ConnectionInfo.ClientIpAddress}");
                                    return;
                                }
                            }

                            MessageReceived(msg,conn);
                        }
                        else
                        {
                            //invalid API call.
                            //probably not a client.
                            conn.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    //not JSON.
                    Console.WriteLine($"Fatal error occured : {ex}");
                    return;
                }

            };
        }

        private void MessageReceived(Structures.BaseMessage message,IWebSocketConnection conn)
        {
            //the dashboard clients should not be sending something that does not start with '/'.
            if(message.Text.StartsWith("/")) OnMessageReceived?.Invoke(message,conn);
        }

        /// <summary>
        /// This is used to send messages to all connected dashboards.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void Push(string message,string name,string type)
        {
            lock(GlobalMessage) lock (Clients)
            {
                if (Clients.Count == 0) return; //no connected dashboards.
                GlobalMessage.Text = message;
                GlobalMessage.Type = type;
                GlobalMessage.Date = GetTime();
                GlobalMessage.Name = name;
                var data = JsonSerializer.Serialize<Structures.BaseMessage>(GlobalMessage);
                Task[] sendTasks = new Task[Clients.Count];
                var index = 0;
                foreach (var client in Clients)
                {
                    sendTasks[index] = AsyncSend(client,data);
                    index++;
                }
                //if this is not working, we have an issue with the server.
                Task.WhenAny(sendTasks);
            }

        }

        /// <summary>
        /// Used to send a message to a specific dashboard API client.
        /// </summary>
        /// <param name="message">The text value of the BaseMessage.</param>
        /// <param name="name">The name of the system.</param>
        /// <param name="type">The message type.</param>
        /// <param name="connection">The target.</param>
        public void PushTo(string message, string name, string type, IWebSocketConnection connection)
        {
            try
            {
                var msg = new Structures.BaseMessage
                {
                    Type = type,
                    Name = name,
                    Text = message,
                    Date = GetTime()
                };
                connection.Send(JsonSerializer.Serialize(msg));
            }
            catch(Exception ex)
            {
                //we'd like all the dashboards to know that they have been betrayed.
                Push($"{ex.Message}",Structures.ServerSources.DebugSystemCritical,Structures.MessageFlag.ConsoleLogMessage);
                Logger.LogError(ex.Message);
            }
        }

        /// <summary>
        /// Used to send data asynchronously.
        /// </summary>
        /// <param name="conn">The target client.</param>
        /// <param name="data">The data JSON to send.</param>
        /// <returns></returns>
        private async Task AsyncSend(IWebSocketConnection conn, string data)
        {
            try
            {
                await conn.Send(data);
            }
            catch (TimeoutException)
            {
                lock (Clients)
                {
                    if (Clients.Contains(conn)) /*why shouldn't it...*/ Clients.Remove(conn);
                }
            }
            catch (Exception ex)
            {
                Push(ex.Message,Structures.ServerSources.DebugSystemCritical, Structures.MessageFlag.ConsoleLogMessage);
                Logger.LogError(ex.Message);
            }
        }

        /// <summary>
        /// Will get the UNIX time epoch.
        /// </summary>
        /// <returns></returns>
        public static ulong GetTime()
        {
            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            return (ulong)t.TotalSeconds;
        }


        public delegate void DelMessageReceived(Structures.BaseMessage message,IWebSocketConnection connection);

        public event DelMessageReceived OnMessageReceived;
    }
}
