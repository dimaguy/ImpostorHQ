using Hazel;
using System;
using Hazel.Udp;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using Impostor.Commands.Core.SELF;

namespace Impostor.Commands.Core
{
    public class AnnouncementServer
    {
        #region Members
        public MessageWriter Message { get; private set; }
        public MessageWriter DisconnectMessage { get; set; }
        public UdpConnectionListener Listener { get; private set; }
        public Class Master { get; private set; }
        public bool WillSend { get; private set; }
        private string ConfigPath { get; set; }
        private byte[] ReadBuffer { get; set; }
        private FileStream IdStream { get; set; }
        private readonly object _writeLock = new object();
        #endregion

        /// <summary>
        /// The announcement server runs on port 22024/udp and is used to... server announcements to among us players.
        /// </summary>
        /// <param name="master">The plugin's main.</param>
        /// <param name="configFolder">The configuration folder, to store the message identifiers.</param>
        public AnnouncementServer(Class master,string configFolder)
        {
            this.Listener = new UdpConnectionListener(new IPEndPoint(IPAddress.Any, 22024));
            this.Listener.Start();
            this.Listener.NewConnection += Listener_NewConnection;
            this.Master = master;
            this.Message = MessageWriter.Get(SendOption.Reliable);
            this.DisconnectMessage = MessageWriter.Get(SendOption.Reliable);
            DisconnectMessage.Write((byte)09);
            this.ConfigPath = Path.Combine(configFolder, "announcement.id");
            this.ReadBuffer = new byte[4];
            if (!File.Exists(ConfigPath))
            {
                IdStream = File.Create(ConfigPath);
                IdStream.Write(GetInt(0), 0, 4);
            }
            else
            {
                IdStream = File.Open(ConfigPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            }
        }
        /// <summary>
        /// This sets a message on the server. The next time a client navigates to the menu, the message will be shown. Beware that it will only be shown once.
        /// </summary>
        /// <param name="message"></param>
        public void SetMessage(string message)
        {
            lock (_writeLock)
            {
                WillSend = true;
                lock (this.Message)
                {
                    Message.Clear(SendOption.Reliable);
                    Message.StartMessage(1);
                    Message.WritePacked(NextAnnouncementId());
                    Message.Write(message);
                    Message.EndMessage();
                }
            }
        }
        /// <summary>
        /// This is used to disable the current announcement (if any).
        /// </summary>
        public void DisableAnnouncement()
        {
            this.WillSend = false;
        }
        public void Shutdown()
        {
            this.Listener.Dispose();
        }
        private void Listener_NewConnection(NewConnectionEventArgs obj)
        {
            try
            {
                if (!WillSend)
                {
                    obj.Connection.Dispose();
                    return;
                }
                lock (Message)
                {
                    obj.Connection.Send(Message);
                }
                obj.Connection.Send(DisconnectMessage);
                obj.Connection.Dispose();
            }
            catch (Exception e)
            {
                Master.LogManager.LogError(e.ToString(),Shared.ErrorLocation.AnnouncementServer);
            }
        }
        /// <summary>
        /// Will get a suitable ID for the announcement. This is vital to ensure that the message is displayed.
        /// </summary>
        /// <returns></returns>
        private int NextAnnouncementId()
        {
            lock (_writeLock)
            {
                IdStream.Read(ReadBuffer, 0, 4);
                var number = GetInt(ReadBuffer) + 1;
                if (number > 50) number = 0;
                ReadBuffer = GetInt(number);
                IdStream.Seek(0, SeekOrigin.Begin);
                IdStream.Write(ReadBuffer, 0, 4);
                IdStream.Flush();
                return number;
            }
        }
        /// <summary>
        /// Will convert an integer to binary data, according the current system's endian.
        /// </summary>
        /// <param name="num">The integer to convert.</param>
        /// <returns>4 bytes.</returns>
        private byte[] GetInt(int num)
        {
            return BitConverter.GetBytes(num);
        }
        /// <summary>
        /// This will convert 4 bytes of binary data to an integer, according the current system's endian.
        /// </summary>
        /// <param name="buffer">4 bytes to convert.</param>
        /// <returns>An integer.</returns>
        private int GetInt(byte[] buffer)
        {
            return BitConverter.ToInt32(buffer);
        }
    }
}
