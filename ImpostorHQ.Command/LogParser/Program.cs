using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace LogParser
{
    class Program
    {
        private static long LogsRead = 0;
        static void Main(string[] args)
        {
            Console.WriteLine("ImpostorHQ .SELF log parser.");
            Console.Write("\nPath (drag and drop the file): ");
            var path = Console.ReadLine().Replace("\"", "").Replace("'", "");
            while (string.IsNullOrEmpty(path)||!File.Exists(path))
            {
                Console.Write("\nPlease input a valid path: ");
                path = Console.ReadLine().Replace("\"", "").Replace("'", "");
            }

            var destinationName = Path.GetFileNameWithoutExtension(path) + ".csv";
            var decoder = new SelfDecoder(path);
            var t = new Thread(Status);
            t.Start();
            using (var csvStream = File.CreateText(destinationName))
            {
                Shared.DashboardLog dashboardLog;
                Shared.ErrorLog errorLog;
                foreach (var bLog in decoder.ReadAll())
                {
                    LogsRead++;
                    switch (bLog.Type)
                    {
                        case Shared.LogType.Dashboard:
                        {
                            dashboardLog = Shared.DashboardLog.Deserialize(bLog);
                            csvStream.WriteLine(CompileCsv(new string[]
                            {
                                "Dashboard", "From " + dashboardLog.SourceIp, dashboardLog.Message
                            }));
                            break;
                        }
                        case Shared.LogType.Error:
                        {
                            errorLog = Shared.ErrorLog.Deserialize(bLog);
                            
                            csvStream.Write(CompileCsv(new string[]
                            {
                                "Error","Thrown in " + errorLog.Location,errorLog.Message
                            }));
                            break;
                        }
                    }
                    csvStream.Flush();
                }
            }

            LogsRead = -1;
            Console.ReadLine();

        }

        static void Status()
        {
            long read = 0;
            while ((read = LogsRead) != -1)
            {
                Console.Clear();
                Console.WriteLine($"Logs converted: {read}");
                Thread.Sleep(1000);
            }
            Console.Clear();
            Console.WriteLine("The logs have been successfully parsed.");
        }
        private static string CompileCsv(string[] data)
        {
            string rv = "";
            for (int i = 0; i < data.Length; i++)
            {
                rv += data[i];
                if (i!=data.Length-1)
                {
                    rv += ',';
                }
            }

            return rv;
        }
    }
    public class SelfDecoder
    {
        public FileStream IOStream { get; private set; }
        public SelfDecoder(string path)
        {
            IOStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public BinaryLog ReadLog()
        {
            var sizeBytes = new byte[2];
            IOStream.Read(sizeBytes, 0, 2);
            var size = BitConverter.ToUInt16(sizeBytes,0);
            var data = new byte[size];
            Console.WriteLine($"Size : {size}");
            IOStream.Read(data, 0, size);
            return BinaryLog.Deserialize(new MemoryStream(data), size);
        }

        public IEnumerable<BinaryLog> ReadAll()
        {
            while (IOStream.Position != IOStream.Length) yield return ReadLog();
        }
        public class BinaryLog
        {
            public ushort BaseLength { get; set; }
            public Shared.LogType Type { get; set; }
            public DateTime TimeStamp { get; set; }
            public byte[] LogData { get; set; }
            public static BinaryLog Deserialize(MemoryStream stream, ushort baseLength)
            {
                var type = stream.ReadByte();
                var buffer = new byte[8];
                stream.Read(buffer, 0, 8);
                var epoch = BitConverter.ToUInt64(buffer, 0);
                buffer = new byte[baseLength - 9];
                stream.Read(buffer, 0, buffer.Length);
                return new BinaryLog
                {
                    BaseLength = baseLength,
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
            Rpc = 0, Dashboard = 1, Error = 2
        }

        public enum ErrorLocation : byte
        {
            DashboardCommandHandler = 0, HttpServer, PushTo, AsyncSend
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
                log.GameCode = BitConverter.ToInt32(buffer, 0);
                Buffer.BlockCopy(source.LogData, 5, buffer, 0, 4);
                log.IpAddress = new IPAddress(buffer).ToString();
                buffer = new byte[source.LogData.Length - 9];
                Buffer.BlockCopy(source.LogData, 9, buffer, 0, buffer.Length);
                log.RpcData = buffer;
                return log;
            }
        }

        public class DashboardLog
        {
            public string SourceIp { get; set; }
            public string Message { get; set; }
            public DashboardLog()
            {

            }

            public static DashboardLog Deserialize(SelfDecoder.BinaryLog source)
            {
                var data = new byte[source.LogData.Length - 4];
                Buffer.BlockCopy(source.LogData,4,data,0,data.Length);
                return new DashboardLog
                {
                    SourceIp = new IPAddress(source.LogData.Take(4).ToArray()).ToString(),
                    Message = Encoding.UTF8.GetString(data)
                };
            }
        }

        public class ErrorLog
        {
            public ErrorLocation Location { get; set; }
            public string Message { get; set; }
            public ErrorLog()
            {
            }

            public static ErrorLog Deserialize(SelfDecoder.BinaryLog source)
            {
                var location = (ErrorLocation) source.LogData[0];
                var data = new byte[source.LogData.Length - 1];
                Buffer.BlockCopy(source.LogData,1,data,0,data.Length);
                return new ErrorLog()
                {
                    Location = location,
                    Message =  Encoding.UTF8.GetString(data)
                };
            }
        }
    }
}
