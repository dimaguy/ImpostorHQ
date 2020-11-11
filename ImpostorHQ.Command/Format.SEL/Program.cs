using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Xml;
using BitConverter = System.BitConverter;
using Console = System.Console;

namespace Format.SEL
{
    class Program
    {
        static void Main(string[] args)
        {
            var encoder = new SelfEncoder("test.self");
            var testData =
                Encoding.UTF8.GetBytes("dick");
            
            
            //the first write takes 300 milliseconds, weirdly enough. The next writes take no time.
            encoder.WriteRpcLog(3141, "3.1.4.1", Shared.RpcCalls.PlayAnimation, testData);
            var dt0 = DateTime.Now.Ticks;
            for (int i = 0; i < 1000; i++)
            {
                encoder.WriteRpcLog(3141, "3.1.4.1", Shared.RpcCalls.PlayAnimation, testData);
            }
            var dt1 = DateTime.Now.Ticks;
            encoder.End();
            var decoder = new SelfDecoder("test.self");
            foreach (var log in decoder.ReadAll())
            {
                Console.WriteLine(new string('-',Console.BufferWidth));
                Console.WriteLine("Decoded: ");
                Console.WriteLine("  Type: " + log.Type);
                Console.WriteLine("  Time: " + log.TimeStamp);
                Console.WriteLine($"  Data [{log.LogData.Length}]:");
                Console.WriteLine();
                var rpcLog = Shared.RpcLog.Deserialize(log);
                Console.WriteLine($"    Game Code : {rpcLog.GameCode}");
                Console.WriteLine($"    RPC Address:    {rpcLog.IpAddress}");
                Console.WriteLine($"    RPC Type:       {rpcLog.Type}");
                Console.WriteLine($"    RPC Data:       {Encoding.UTF8.GetString(rpcLog.RpcData)}");
            }
            long microseconds = (dt1-dt0) / (TimeSpan.TicksPerMillisecond / 1000);
            Console.WriteLine($"\n\n\n =====> Time per log operation (microseconds) : {((double)(microseconds))/1000.0}");

            Thread.Sleep(-1);
        }
    }

    //SPATIALLY EFFICIENT LOG FILE - copyright@anti :)

    class SelfEncoder
    {
        public FileStream IoStream { get; private set; }
        private MemoryStream EncodeStream { get; set; }
        public SelfEncoder(string path)
        {
            IoStream = new FileStream(path,FileMode.Create,FileAccess.Write,FileShare.Read);
            EncodeStream = new MemoryStream();
        }

        private void BeginWriteLine(byte logType , UInt64 unixTimeMs)
        {
            EncodeStream.WriteByte(logType);
            EncodeStream.Write(BitConverter.GetBytes(unixTimeMs),0,8);
        }

        private void EndWriteLine()
        {
            IoStream.WriteByte((byte)EncodeStream.Length);
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

        public void End()
        {
            IoStream.Close();
        }
        private ulong GetTime()
        {
            return (ulong) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

        
    }

    public class SelfDecoder
    {
        public FileStream IOStream { get; private set; }
        public SelfDecoder(string path)
        {
            IOStream = new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
        }

        public BinaryLog ReadLog()
        {
            var size = (byte) IOStream.ReadByte();
            var data = new byte[size];
            IOStream.Read(data, 0, size);
            return BinaryLog.Deserialize(new MemoryStream(data),size);
        }

        public IEnumerable<BinaryLog> ReadAll()
        {
            while (IOStream.Position != IOStream.Length) yield return ReadLog();
        }
        public class BinaryLog
        {
            public byte BaseLength { get; set; }
            public Shared.LogType Type { get; set; }
            public DateTime TimeStamp { get; set; }
            public byte[] LogData { get; set; }
            public static BinaryLog Deserialize(MemoryStream stream, byte baseLength)
            {
                var type = stream.ReadByte();
                var buffer = new byte[8];
                stream.Read(buffer, 0, 8);
                var epoch = BitConverter.ToUInt64(buffer,0);
                buffer = new byte[baseLength-9];
                stream.Read(buffer, 0, buffer.Length);
                return new BinaryLog
                {
                    BaseLength =  baseLength,
                    Type = (Shared.LogType)type,
                    TimeStamp = GetTime(epoch),
                    LogData = buffer
                };
            }

            private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            private static DateTime GetTime(ulong unixTime)
            {
                return Epoch.AddMilliseconds(unixTime);
            }
        }

        
    }
    public class Shared
    {
        public enum LogType : byte
        {
            Rpc = 0
        }

        public enum RpcCalls : byte
        {
            PlayAnimation = 0,
            CompleteTask,
            SyncSettings,
            SetInfected,
            Exiled,
            CheckName,
            SetName,
            CheckColor,
            SetColor,
            SetHat,
            SetSkin,
            ReportDeadBody,
            MurderPlayer,
            SendChat,
            StartMeeting,
            SetScanner,
            SendChatNote,
            SetPet,
            SetStartCounter,
            EnterVent,
            ExitVent,
            SnapTo,
            Close,
            VotingComplete,
            CastVote,
            ClearVote,
            AddVote,
            CloseDoorsOfType,
            RepairSystem,
            SetTasks,
            UpdateGameData
        }
        public static readonly byte[] CrLf = Encoding.ASCII.GetBytes("\r\n");
        public class RpcLog
        {
            public Shared.RpcCalls Type { get; set; }
            public string IpAddress { get; set; }
            public byte[] RpcData { get; set; }
            public int GameCode { get; set; }
            public RpcLog()
            {

            }

            public static RpcLog Deserialize(SelfDecoder.BinaryLog source)
            {
                var log = new RpcLog();
                log.Type = (Shared.RpcCalls)source.LogData[0];
                var buffer = new byte[4];
                Buffer.BlockCopy(source.LogData, 1, buffer, 0, 4);
                log.GameCode = BitConverter.ToInt32(buffer,0);
                Buffer.BlockCopy(source.LogData, 5, buffer, 0, 4);
                log.IpAddress = new IPAddress(buffer).ToString();
                buffer = new byte[source.LogData.Length - 9]; 
                Buffer.BlockCopy(source.LogData, 9, buffer, 0, buffer.Length);
                log.RpcData = buffer;
                return log;
            }
        }

    }
}
