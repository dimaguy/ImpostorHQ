using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Impostor.Api.Net.Messages;

namespace Impostor.Commands.Core
{
    public class Structures
    {
        /// <summary>
        /// 
        /// </summary>
        public static class MessageFlag
        {
            public const string LoginApiRequest = "0";      // A request to log in, with a given API key.
            public const string LoginApiAccepted = "1";     // The API Key is correct, so the login is successful.
            public const string LoginApiRejected = "2";     // The API key is incorrect, so the login is rejected.
            public const string ConsoleLogMessage = "3";    // The only working text message, so far.
            public const string ConsoleCommand = "4";       // A command sent from the dashboard to the API.
            public const string HeartbeatMessage = "5";     // Not implemented yet.
            public const string GameListMessage = "6";      // Not implemented yet.
            public const string DoKickOrDisconnect = "7";   // A message when a client is kicked (not implemented) or the server shuts down.
        }
        [Serializable]
        public class BaseMessage
        {
            /// <summary>
            /// The message data.
            /// </summary>
            public string Text { get; set; }
            /// <summary>
            /// The type of the message. JS, please no!!!
            /// </summary>
            public string Type { get; set; }
            /// <summary>
            /// The UNIX date epoch.
            /// </summary>
            public ulong Date { get; set; }
            /// <summary>
            /// The source of the message.
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// Additional data for some messages (e.g the heartbeat)
            /// </summary>
            public float[] Flags { get; set; }
        }

        public static class DashboardCommands
        {
            public const string ServerWideBroadcast = "/broadcast";
            public const string HelpMessage = "/help";
            public const string StatusMessage = "/status";
            public const string BansMessage = "/bans";
        }

        public class Exceptions
        {
            public class PlayerNullException : Exception
            {
                public PlayerNullException() : base("Broadcast error : cannot get carrier player.") { }
            }

            public class CommandPrefixException : Exception
            {
                public CommandPrefixException() : base("Command registration error : Commands must start with '/'.") { }
            }

            public class PleaseProvideDocsException : Exception
            {
                public PleaseProvideDocsException() : base("Please provide docs for the command!") {}
            }
        }

        public class PluginConfiguration
        {
            #region Dashboard
            public bool UseSSL { get; set; }
            public string[] APIKeys { get; set; }
            public ushort APIPort { get; set; }
            public ushort WebsitePort { get; set; }
            public string ListenInterface { get; set; }
            #endregion
            public static PluginConfiguration GetDefaultConfig()
            {
                var cfg = new PluginConfiguration();
                #region Dashboard
                cfg.UseSSL = false;
                cfg.APIKeys = new[] { Guid.NewGuid().ToString() };
                cfg.APIPort = 22023;
                cfg.WebsitePort = 8080;
                cfg.ListenInterface = "0.0.0.0";
                #endregion
                return cfg;
            }

            public void SaveTo(string path)
            {
                if (File.Exists(path)) File.Delete(path);
                File.WriteAllText(path, JsonSerializer.Serialize<PluginConfiguration>(this));
            }

            public static PluginConfiguration LoadFrom(string path)
            {
                return JsonSerializer.Deserialize<PluginConfiguration>(File.ReadAllText(path));
            }
        }

        public static class ServerSources
        {
            public const string CommandSystem = "cmd-sys";
            public const string DebugSystem = "dbg-sys";
            public const string DebugSystemCritical = "dbg-sys / CRITICAL";
            public const string SystemInfo = "sysinfo";

        }
        public enum RpcCalls : byte
        {
            PlayAnimation = 0,
            CompleteTask = 1,
            SyncSettings = 2,
            SetInfected = 3,
            Exiled = 4,
            CheckName = 5,
            SetName = 6,
            CheckColor = 7,
            SetColor = 8,
            SetHat = 9,
            SetSkin = 10,
            ReportDeadBody = 11,
            MurderPlayer = 12,
            SendChat = 13,
            StartMeeting = 14,
            SetScanner = 15,
            SendChatNote = 16,
            SetPet = 17,
            SetStartCounter = 18,
            EnterVent = 19,
            ExitVent = 20,
            SnapTo = 21,
            Close = 22,
            VotingComplete = 23,
            CastVote = 24,
            ClearVote = 25,
            AddVote = 26,
            CloseDoorsOfType = 27,
            RepairSystem = 28,
            SetTasks = 29,
            UpdateGameData = 30,
        }
        //not used yet:
        public class PacketGenerator
        {
            public bool WritingGameData { get; set; }
            public bool WritingRPC { get; set; }
            private IMessageWriterProvider Provider { get; set; }
            public PacketGenerator(IMessageWriterProvider provider)
            {
                this.Provider = provider;
            }

            private IMessageWriter StartRpc(int gameCode,uint netId, RpcCalls callId, int targetClientId = -1, MessageType type = MessageType.Reliable)
            {
                var writer = Provider.Get(type);

                if (targetClientId < 0)
                {
                    writer.StartMessage(MessageFlags.GameData);
                    writer.Write(gameCode);
                }
                else
                {
                    writer.StartMessage(MessageFlags.GameDataTo);
                    writer.Write(gameCode);
                    writer.WritePacked(targetClientId);
                }

                writer.StartMessage((byte)GameDataType.RpcFlag);
                writer.WritePacked(netId);
                writer.Write((byte)callId);

                return writer;
            }

            private IMessageWriter EndRpc(IMessageWriter writer)
            {
                writer.EndMessage();
                writer.EndMessage();
                return writer;
            }

            public IMessageWriter WriteChat(int game, uint netId, string chat)
            {
                var writer = StartRpc(game, netId, RpcCalls.SendChat);
                writer.Write(chat);
                writer = EndRpc(writer);
                return writer;
            }

            internal static class MessageFlags
            {
                public const byte HostGame = 0;
                public const byte JoinGame = 1;
                public const byte StartGame = 2;
                public const byte RemoveGame = 3;
                public const byte RemovePlayer = 4;
                public const byte GameData = 5;
                public const byte GameDataTo = 6;
                public const byte JoinedGame = 7;
                public const byte EndGame = 8;
                public const byte AlterGame = 10;
                public const byte KickPlayer = 11;
                public const byte WaitForHost = 12;
                public const byte Redirect = 13;
                public const byte ReselectServer = 14;
                public const byte GetGameList = 9;
                public const byte GetGameListV2 = 16;
            }
            public enum GameDataType : byte
            {
                RpcFlag = 2,
                SpawnFlag = 4,
                DespawnFlag = 5,
                SceneChangeFlag = 6,
                ReadyFlag = 7,
                ChangeSettingsFlag = 8
            }
            
        }
        public enum BroadcastType
        {
            Warning, Error, Information
        }
        [Serializable]
        public class Report
        {
            /// <summary>
            /// The supposed offender's IPA.
            /// </summary>
            public string Target { get; set; }
            /// <summary>
            /// The name of the offender.
            /// </summary>
            public string TargetName { get; set; }
            /// <summary>
            /// The witnesses's IPAs.
            /// </summary>
            public List<string> Sources { get; set; }
            /// <summary>
            /// The testimonials.
            /// </summary>
            public List<string> Messages { get; set; }
            /// <summary>
            /// How many complaints there are against the offender.
            /// </summary>
            public ushort TotalReports { get; set; }
            /// <summary>
            /// How much jail time the offender gets.
            /// </summary>
            public uint MinutesRemaining { get; set; }
        }
    }
}
