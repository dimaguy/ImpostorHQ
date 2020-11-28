using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Impostor.Commands.Core;
using Impostor.Commands.Core.QuantumExtensionDirector;

namespace ImpostorHQ.Plugin.DDoSInfo
{
    public class MainClass : IPlugin
    {
        public string Name => "DDoS Info";

        public string Author => "anti";

        public uint HqVersion => 4;

        public QuiteExtendableDirectInterface PluginBase { get; private set; }
        public PluginFileSystem FileSys { get; private set; }
        public string HistoryFilePath { get; private set; }
        public const string MainCommand = "/attackinfo";
        public void Destroy()
        {
        }

        public void Load(QuiteExtendableDirectInterface reference, PluginFileSystem system)
        {
            this.PluginBase = reference;
            this.FileSys = system;
            this.HistoryFilePath = Path.Combine(system.Store, "history.json");
            if(!File.Exists(HistoryFilePath)) File.Create(HistoryFilePath).Close();
            Main();
        }

        private void Main()
        {
            PluginBase.QEDetector.OnBlockedOnce += (address) =>
            {
                File.AppendAllLines(HistoryFilePath,new string[]
                {
                    new HistoricRecord(){Address = address.ToString(),AttackTime = DateTime.Now}.Serialize()
                });
            };
            PluginBase.ApiServer.RegisterCommand(MainCommand,"<now/history> => will display information about the ongoing DoS/DDoS attack(if applicable) and all the past attacks, respectively.");
            PluginBase.OnDashboardCommandReceived += (command, data, single, source) =>
            {
                if (command.Equals(MainCommand))
                {
                    if (string.IsNullOrEmpty(data) || single)
                    {
                        PluginBase.ApiServer.PushTo("Invalid syntax. Please use /help first.","DDoSInfo",Structures.MessageFlag.ConsoleLogMessage,source);
                    }
                    else
                    {
                        if (data.Equals("now"))
                        {
                            PluginBase.ApiServer.PushTo(CompileBlocks(), "DDoSInfo", Structures.MessageFlag.ConsoleLogMessage, source);
                        }
                        else if (data.Equals("history"))
                        {
                            var lines = File.ReadAllLines(HistoryFilePath);
                            if (lines.Length == 0)
                            {
                                PluginBase.ApiServer.PushTo("No attack history.", "DDoSInfo", Structures.MessageFlag.ConsoleLogMessage, source);
                                return;
                            }

                            string result = $"All recorded attacks [{lines.Length}]:\n";
                            foreach (var line in lines)
                            {
                                var record = HistoricRecord.Deserialize(line);
                                result +=
                                    $"    IPA: {record.Address}, on: {record.AttackTime.ToString("G")}.\n";
                            }
                            PluginBase.ApiServer.PushTo(result, "DDoSInfo", Structures.MessageFlag.ConsoleLogMessage, source);
                        }
                        else
                        {
                            PluginBase.ApiServer.PushTo("Invalid command. Available: now, history.", "DDoSInfo", Structures.MessageFlag.ConsoleLogMessage, source);
                        }
                    }
                }
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string CompileBlocks()
        {
            var blocks = PluginBase.QEDetector.GetBlocked().ToArray();
            if (blocks.Length == 0) return "Attackers: none.";

            string result = $"Attackers [{blocks.Length}]:\n";
            foreach (var blockedAddressInfo in blocks)
            {
                result +=
                    $"    {blockedAddressInfo.Address}, blocked at {blockedAddressInfo.AttackStart.ToString("t")} ({blockedAddressInfo.SecondsAgo} rels ago)\n";
            }

            return result;
        }
    }

    [Serializable]
    class HistoricRecord
    {
        /// <summary>
        /// The attacker's IP address.
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        /// The time of the block.
        /// </summary>
        public DateTime AttackTime { get; set; }

        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }

        public static HistoricRecord Deserialize(string str)
        {
            return JsonSerializer.Deserialize<HistoricRecord>(str);
        }
    }
}
