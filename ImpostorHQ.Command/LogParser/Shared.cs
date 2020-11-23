using System;
using System.Net;
using System.Text;
using System.Linq;

namespace Impostor.Commands.Core.SELF
{
    public class Shared
    {
        public enum LogType : byte
        {
            Rpc = 0, Dashboard = 1,Error = 2
        }

        public enum ErrorLocation : byte
        {
            DashboardCommandHandler = 0, HttpServer, PushTo, AsyncSend,AnnouncementServer
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
                Buffer.BlockCopy(source.LogData, 4, data, 0, data.Length);
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
                var location = (ErrorLocation)source.LogData[0];
                var data = new byte[source.LogData.Length - 1];
                Buffer.BlockCopy(source.LogData, 1, data, 0, data.Length);
                return new ErrorLog()
                {
                    Location = location,
                    Message = Encoding.UTF8.GetString(data)
                };
            }
        }
    }
}