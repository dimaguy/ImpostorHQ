using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Impostor.Commands.Core.Client.Interpreter;
using Impostor.Commands.Core.Client.Networking;

namespace Impostor.Commands.Core.Client
{
    public class ImpostorHQClient : IDisposable
    {
        private ImpostorHQSocket Client { get; set; }
        public ushort HttpPort { get; set; }
        public bool Interpret { get; set; }
        /// <summary>
        /// This will create a new ImpostorHQ client, and will authenticate with the server.
        /// </summary>
        /// <param name="ep">The server's end point.</param>
        /// <param name="password">Your API key.</param>
        /// <param name="tls">Indicates the usage of TLS.</param>
        /// <param name="interpretTextCommands">If enabled, server console logs will be interpreted to specific events.</param>
        /// <param name="httpPort">The port for the HTTP requests. Set it to 0 if you don't need to download logs or player lists.</param>
        public ImpostorHQClient(IPEndPoint ep, string password, bool tls, bool interpretTextCommands = true, ushort httpPort = 0)
        {
            this.Client = new ImpostorHQSocket(ep);
            this.Interpret = interpretTextCommands;
            this.HttpPort = httpPort;
            Authenticate(password, tls);
        }

        public void WaitForConnection()
        {
            while(!Client.Connected) Thread.Sleep(10);
        }
        private async Task Authenticate(string password, bool tls)
        {
            try
            {
                await Client.Connect(password, tls, MessageHandler).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        private void MessageHandler(BaseMessage message)
        {
            switch (message.Type)
            {
                case MessageFlag.HeartbeatMessage:
                {
                    OnStatusReceived?.Invoke(new StatusUpdate(
                        (uint)message.Flags[4],
                        (uint)message.Flags[3],
                        (uint)message.Flags[1],
                        (uint)message.Flags[0],
                        (uint)message.Flags[2]
                        ));
                    break;
                }
                case MessageFlag.ConsoleLogMessage:
                {
                    OnTextReceived?.Invoke(message.Text);
                    if(Interpret) InterpretCommand(message.Text);
                    break;
                }
            }
        }
        /// <summary>
        /// This sends a command to the server.
        /// </summary>
        /// <param name="message"></param>
        public void SendConsoleMessage(string message)
        {
            Client.SendRaw(new BaseMessage()
            {
                Type = MessageFlag.ConsoleCommand,
                Date = BlackTeaTransport.GetTime(),
                Flags = null,
                Name = null,
                Text = message
            });
        }

        #region Commands
        
        public void SendHelpCommand()
        {
            SendConsoleMessage("/help");
        }

        public void ListBans()
        {
            SendConsoleMessage("/bans");
        }

        public void ListLogs()
        {
            SendConsoleMessage("/logs");
        }

        #endregion

        #region HTTP Api

        private string CreateGET(string path)
        {
            CheckPorts();
            return $"GET /{path} HTTP/1.1\n\r";
        }

        private void CheckPorts()
        {
            if (HttpPort == 0) throw new ArgumentException("No HTTP port selected.");
        }
        /// <summary>
        /// This will get the player list, in CSV format. You must assign the server's HTTP port.
        /// </summary>
        /// <returns>The player list, in CSV format.</returns>
        public string[] GetPlayerList()
        {
            var request = CreateGET($"players.csv?{Client.Transport.Key}");
            Console.WriteLine($"Request: {request}");
            using (TcpClient c = new TcpClient(Client.RemoteEndPoint.Address.ToString(), HttpPort))
            {
                c.GetStream().Write(Encoding.UTF8.GetBytes(request),0,request.Length);
                using (StreamReader rd = new StreamReader(c.GetStream()))
                {
                    return rd.ReadToEnd().Replace("\r\n","\n").Split('\n').Skip(5).ToArray();
                }
            }
        }
        #endregion
        private void InterpretCommand(string message)
        {
            if (message.Contains("Total bans"))
            {
                var reports = new List<Ban>();
                var lines = message.Split('\n').Skip(1).ToArray();
                foreach (var line in lines)
                {
                    if (line.StartsWith("  IPA : "))
                    {
                        var combination = line.Replace("  IPA : ", "").Split('/');
                        reports.Add(new Ban()
                        {
                            Address = IPAddress.Parse(combination[0].TrimEnd(' ')),
                            Name = combination[1].TrimStart(' ')
                        });
                    }
                }
                OnBansListed?.Invoke(reports);
            }
            else if (message.Contains("Log dates"))
            {
                var lines = message.Split('\n').Skip(1).ToArray();
                List<string> logFiles = new List<string>();
                foreach (var line in lines) if(!string.IsNullOrEmpty(line)) logFiles.Add(line);
                OnLogsListed?.Invoke(logFiles);
            }
        }

        public void Dispose()
        {
            this.Client.Disconnect();
        }
        #region Events

        /// <summary>
        /// This message carries server load, and corresponds to the heartbeat message.
        /// </summary>
        public event EventDelegates.DelStatusReceived OnStatusReceived;
        /// <summary>
        /// This message represents the console messages, sent by the server. It will be called for any message.
        /// </summary>
        public event EventDelegates.DelTextReceived OnTextReceived;
        /// <summary>
        /// This event is only called if you enabled the command interpreter. 
        /// </summary>
        public event EventDelegates.DelBansListed OnBansListed;
        /// <summary>
        /// This event is only called if you enabled the command interpreter. 
        /// </summary>
        public event EventDelegates.DelLogsListed OnLogsListed;

        #endregion
    }
}
