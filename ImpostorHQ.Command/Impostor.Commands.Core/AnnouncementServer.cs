using Hazel;
using System;
using Hazel.Udp;
using System.IO;
using System.Net;
using Impostor.Commands.Core.SELF;

namespace Impostor.Commands.Core
{
    public class AnnouncementServer
    {
        public MessageWriter Message { get; private set; }
        public MessageWriter DisconnectMessage { get; set; }
        public UdpConnectionListener Listener { get; private set; }
        public Class Master { get; private set; }
        public bool WillSend { get; private set; }
        private string ConfigPath { get; set; }
        private byte[] ReadBuffer { get; set; }
        private FileStream IdStream { get; set; }
        private readonly object _writeLock = new object();
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
        //this sets a message. The next time a client connects, the message will be displayed.
        public void SetMessage(string message)
        {
            lock (_writeLock)
            {
                WillSend = true;
                lock (this.Message)
                {
                    Message.Clear(SendOption.Reliable);
                    Message.StartMessage(1);
                    Message.WritePacked(AnnouncementId());
                    Message.Write(message);
                    Message.EndMessage();
                }
            }
        }

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
        private int AnnouncementId()
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

        private byte[] GetInt(int num)
        {
            return BitConverter.GetBytes(num);
        }

        private int GetInt(byte[] buffer)
        {
            return BitConverter.ToInt32(buffer);
        }
    }
}
