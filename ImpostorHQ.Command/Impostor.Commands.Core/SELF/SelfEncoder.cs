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
            EncodeStream.WriteByte(logType);
            EncodeStream.Write(BitConverter.GetBytes(unixTimeMs),0,8);
        }

        private void EndWriteLine()
        {
            IoStream.Write(BitConverter.GetBytes((ushort)EncodeStream.Length),0,2);
            IoStream.Write(EncodeStream.ToArray(),0,(int)EncodeStream.Length);
            EncodeStream.SetLength(0);
        }

        public void WriteRpcLog(int gameCode, string ipAddress,Shared.RpcCalls rpcCall, byte[] data)
        {
            if(data.Length>240) throw new Exception("Too much data. Please write max. 255 bytes at a time.");
            BeginWriteLine((byte)Shared.LogType.Rpc,GetTime());
            EncodeStream.WriteByte((byte)rpcCall);
            EncodeStream.Write(BitConverter.GetBytes(gameCode), 0, 4);
            EncodeStream.Write((IPAddress.Parse(ipAddress).GetAddressBytes()),0,4);
            EncodeStream.Write(data,0,data.Length);
            EndWriteLine();
        }

        public void WriteDashboardLog(IPAddress sourceIpA, string command)
        {
            BeginWriteLine((byte)Shared.LogType.Dashboard,GetTime());
            EncodeStream.Write(sourceIpA.GetAddressBytes(),0,4);
            var data = Encoding.UTF8.GetBytes(command);
            EncodeStream.Write(data,0,data.Length);
            EndWriteLine();
        }

        public void WriteExceptionLog(string trace,Shared.ErrorLocation location)
        {
            BeginWriteLine((byte) Shared.LogType.Error,GetTime());
            EncodeStream.WriteByte((byte) location);
            var data = Encoding.UTF8.GetBytes(trace);
            EncodeStream.Write(data,0,data.Length);
            EndWriteLine();
        }
        public void End()
        {
            IoStream.Close();
            EncodeStream.SetLength(0);
        }
        private ulong GetTime()
        {
            return (ulong) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

        
    }
}