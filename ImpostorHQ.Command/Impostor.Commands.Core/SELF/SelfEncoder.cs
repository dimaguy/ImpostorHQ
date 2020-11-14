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
        public SelfEncoder(string path)
        {
            Start(path);
        }

        public void Start(string path)
        {
            if (!File.Exists(path)) File.Create(path).Close();
            IoStream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.Read);
            EncodeStream = new MemoryStream();
        }
        private void BeginWriteLine(byte logType , UInt64 unixTimeMs)
        {
            EncodeStream.SetLength(0);
            EncodeStream.WriteByte(logType); 
            EncodeStream.Write(BitConverter.GetBytes(unixTimeMs), 0, 8);
            Console.WriteLine($"Beginning write of type : {(Shared.LogType)logType}");
        }

        private void EndWriteLine()
        {
            IoStream.Write(BitConverter.GetBytes((ushort) EncodeStream.Length), 0, 2);
            IoStream.Write(EncodeStream.ToArray(), 0, (int) EncodeStream.Length); 
            EncodeStream.SetLength(0);
            IoStream.Flush(); //i know, but this will reduce the occurence of errors.
            Console.WriteLine("Ending writes");
        }

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
        public void End()
        {
            if(IoStream.CanRead)IoStream.Close();
        }
        private ulong GetTime()
        {
            return (ulong) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }
    }
}