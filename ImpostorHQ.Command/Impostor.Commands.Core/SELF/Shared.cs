using System.Text;

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
    }
}