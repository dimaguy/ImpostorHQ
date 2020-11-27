using System;
using System.IO;
using System.Net;
using System.Text;

namespace Impostor.Commands.Core.SELF
{
    public class SelfEncoder
    {
        public FileStream IoStream { get; private set; }
        private MemoryStream EncodeStream { get; set; }
        private readonly object _writerLock = new object();
        /// <summary>
        /// SELF is used to save some storage space, for logs. That applies when there is very little raw data to be logged. This just calls the start method.
        /// </summary>
        /// <param name="path">The path to start writing logs at. If a file is not present there, it will be created.</param>
        public SelfEncoder(string path)
        {
            Start(path);
        }
        /// <summary>
        /// This is used to begin writing a log file to a specified location.
        /// </summary>
        /// <param name="path">The path to start writing to. If a file does not exist there, it will be created.</param>
        public void Start(string path)
        {
            if (!File.Exists(path)) File.Create(path).Close();
            IoStream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.Read);
            EncodeStream = new MemoryStream();
        }
        /// <summary>
        /// This is used to start writing a log line.
        /// </summary>
        /// <param name="logType">The type of the log.</param>
        /// <param name="unixTimeMs">The time epoch.</param>
        private void BeginWriteLine(byte logType , UInt64 unixTimeMs)
        {
            EncodeStream.SetLength(0);
            EncodeStream.WriteByte(logType); 
            EncodeStream.Write(BitConverter.GetBytes(unixTimeMs), 0, 8);
        }
        /// <summary>
        /// This is used to end writing a log line. It will also flush the data to the physical device.
        /// </summary>
        private void EndWriteLine()
        {
            IoStream.Write(BitConverter.GetBytes((ushort) EncodeStream.Length), 0, 2);
            IoStream.Write(EncodeStream.ToArray(), 0, (int) EncodeStream.Length); 
            EncodeStream.SetLength(0);
            IoStream.Flush(); //i know, but this will reduce the occurence of errors.
        }
        /// <summary>
        /// This is used to log RPC. It is not currently in use anywhere, but is handled.
        /// </summary>
        /// <param name="gameCode">The Game's ID.</param>
        /// <param name="ipAddress">The IP address of the sender.</param>
        /// <param name="rpcCall">The RPC call.</param>
        /// <param name="data">The RPC data.</param>
        public void WriteRpcLog(int gameCode, string ipAddress,Shared.RpcCalls rpcCall, byte[] data)
        {
            lock (_writerLock)
            {
                if (!IoStream.CanWrite) return;
                BeginWriteLine((byte)Shared.LogType.Rpc, GetTime());
                EncodeStream.WriteByte((byte) rpcCall);
                EncodeStream.Write(BitConverter.GetBytes(gameCode), 0, 4);
                EncodeStream.Write((IPAddress.Parse(ipAddress).GetAddressBytes()), 0, 4);
                EncodeStream.Write(data, 0, data.Length);
                EndWriteLine();
            }
        }
        /// <summary>
        /// This is used to log dashboard commands.
        /// </summary>
        /// <param name="sourceIpA">The sender of the command.</param>
        /// <param name="command">The command that was sent.</param>
        public void WriteDashboardLog(IPAddress sourceIpA, string command)
        {
            lock (_writerLock)
            {
                if (!IoStream.CanWrite) return;
                BeginWriteLine((byte) Shared.LogType.Dashboard, GetTime());
                EncodeStream.Write(sourceIpA.GetAddressBytes(), 0, 4);
                var data = Encoding.UTF8.GetBytes(command);
                EncodeStream.Write(data, 0, data.Length);
                EndWriteLine();
            }
        }
        /// <summary>
        /// This is used to log plugins.
        /// </summary>
        /// <param name="sourceName">The source system / the name of the plugin.</param>
        /// <param name="message">The data to log.</param>
        public void WritePluginLog(string sourceName, string message)
        {
            lock (_writerLock)
            {
                if (!IoStream.CanWrite) return;
                BeginWriteLine((byte)Shared.LogType.Plugin,GetTime());
                var data = Encoding.UTF8.GetBytes(sourceName);
                EncodeStream.WriteByte((byte)data.Length);
                EncodeStream.Write(data,0,data.Length);
                data = Encoding.UTF8.GetBytes(message);
                EncodeStream.Write(data,0,data.Length);
                EndWriteLine();
            }
        }
        /// <summary>
        /// This is used to write exception logs.
        /// </summary>
        /// <param name="trace">The trace error.</param>
        /// <param name="location">The source system / plugin name.</param>
        public void WriteExceptionLog(string trace,Shared.ErrorLocation location)
        {
            lock (_writerLock)
            {
                if (!IoStream.CanWrite) return;
                BeginWriteLine((byte) Shared.LogType.Error, GetTime());
                EncodeStream.WriteByte((byte) location);
                var data = Encoding.UTF8.GetBytes(Convert.ToBase64String(Encoding.UTF8.GetBytes(trace)));
                EncodeStream.Write(data, 0, data.Length);
                EndWriteLine();
            }
        }
        /// <summary>
        /// This will close the physical stream.
        /// </summary>
        public void End()
        {
            if(IoStream.CanRead)IoStream.Close();
        }
        /// <summary>
        /// This is a duplicate Linux epoch generator.
        /// </summary>
        /// <returns></returns>
        private ulong GetTime()
        {
            return (ulong) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }
    }
}