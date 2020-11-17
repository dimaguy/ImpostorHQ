using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Impostor.Api.Events.Player;
using Impostor.Api.Games;
using Impostor.Api.Games.Managers;
using Impostor.Commands.Core.DashBoard;
using Microsoft.Extensions.Logging;

namespace Impostor.Commands.Core
{
    public class JusticeSystem
    {
        //
        //  we need a storage for reports (that are not bans, yet)
        public List<Structures.Report> IpReports = new List<Structures.Report>();
        //  we need a storage for permanent bans. This is also present on the DISK.
        public List<Structures.Report> PermanentBans = new List<Structures.Report>();
        //  the global logger.
        private ILogger Logger { get; set; }
        //  this indicates how many reports are required to automatically issue a ban.
        public readonly ushort ReportsPerBan;
        //  this stores the path to our database.
        //  it is a folder storing files for each permanent ban.
        private string BanFolder { get; set; }
        //  a chat interface, used to interact with witnesses.
        private GameCommandChatInterface ChatInterface { get; set; }
        private Class Manager { get; set; }
        /// <summary>
        /// This will initialize a new instance of the JusticeSystem class. It is an example extension, which handles banning cheaters.
        /// </summary>
        /// <param name="banFolder">The path to where the database should be stored.</param>
        /// <param name="reportsPerBan">How many reports are required in order to issue an automatic ban.</param>
        /// <param name="logger">The global logger.</param>
        /// <param name="chatInterface">A chat interface, in order to interact with the players.</param>
        public JusticeSystem(string banFolder, ushort reportsPerBan, ILogger logger, GameCommandChatInterface chatInterface,Impostor.Commands.Core.Class Master)
        {
            this.ReportsPerBan = reportsPerBan;
            this.BanFolder = banFolder;
            this.Logger = logger;
            this.ChatInterface = chatInterface;
            this.Manager = Master;
            //we can now read our database (if we have one).
            LoadPermanentBans();
        }

        /// <summary>
        /// This will read and load permanent bans off the disk, if we have any.
        /// </summary>
        private void LoadPermanentBans()
        {
            foreach (var file in Directory.GetFiles(BanFolder))
            {
                if (file.Contains("ban-"))
                {
                    var report = JsonSerializer.Deserialize<Structures.Report>(File.ReadAllText(file));
                    lock (PermanentBans) PermanentBans.Add(report);
                }
            }

            lock (PermanentBans) Logger.LogInformation($"ImpostorHQ : Loaded {PermanentBans.Count} bans.");
        }

        /// <summary>
        /// Call this whenever the report command is fired. This will handle reporting, logging, and banning.
        /// </summary>
        /// <param name="data">The command data.</param>
        /// <param name="source">The source of the report.</param>
        public void HandleReport(string data, IPlayerChatEvent source)
        {
            if (data.Count(x => x == "'"[0]) != 2)
            {
                ChatInterface.SafeMultiMessage(source.Game,
                    "Invalid format. Please use : \"/report hacking Player's name 'Describe the cheat here'\"!",
                    Structures.BroadcastType.Error, "(server/private)", source.ClientPlayer);
                return;
            }

            if (data.StartsWith("hacking "))
            {
                data = data.Remove(0, 8);
                int pFrom = data.IndexOf("'", StringComparison.InvariantCultureIgnoreCase) + 1;
                int pTo = data.LastIndexOf("'", StringComparison.CurrentCultureIgnoreCase);
                var message = data.Substring(pFrom, pTo - pFrom);
                data = new string(data.Take(pFrom - 2 /*we need to remove the ' and the space.*/).ToArray());
                foreach (var client in source.Game.Players)
                {
                    if (client.Character.PlayerInfo.PlayerName.Equals(data))
                    {
                        lock (IpReports)
                        {
                            bool updated = false;
                            for (int i = 0; i < IpReports.Count; i++)
                            {
                                if (IpReports[i].Target.Equals(client.Client.Connection.EndPoint.Address.ToString()))
                                {
                                    updated = true;
                                    if (IpReports[i].TotalReports >= ReportsPerBan)
                                    {
                                        client.BanAsync();
                                        AddPermBan(IpReports[i]);
                                        IpReports.Remove(IpReports[i]);
                                        ChatInterface.SafeMultiMessage(source.Game,
                                            $"\"{client.Character.PlayerInfo.PlayerName}\" has been permanently banned.",
                                            Structures.BroadcastType.Warning);
                                        OnPlayerBanned?.Invoke(client.Character.PlayerInfo.PlayerName,
                                            client.Client.Connection.EndPoint.Address.ToString());
                                    }
                                    else
                                    {
                                        lock (IpReports)
                                        {
                                            updated = true;
                                            if (!IpReports[i].Sources.Contains(source.ClientPlayer.Client
                                                .Connection.EndPoint.Address.ToString()))
                                            {
                                                IpReports[i].TotalReports += 1;
                                                IpReports[i].Messages.Add(message);
                                                IpReports[i].Sources.Add(source.ClientPlayer.Client
                                                    .Connection
                                                    .EndPoint.Address.ToString());
                                                ChatInterface.SafeMultiMessage(source.Game,
                                                    $"Your report has been filed successfully. The offender has {IpReports[i].TotalReports} complaints now.",
                                                    Structures.BroadcastType.Information, "(server complaints/private)",
                                                    source.ClientPlayer);

                                            }
                                            else
                                            {
                                                ChatInterface.SafeMultiMessage(source.Game,
                                                    $"You cannot report the offender again. He will be taken care of.",
                                                    Structures.BroadcastType.Error, "(server/error/private)",
                                                    source.ClientPlayer);
                                            }
                                        }

                                    }
                                }
                            }

                            if (!updated)
                            {
                                var report = new Structures.Report
                                {
                                    TargetName = client.Character.PlayerInfo.PlayerName,
                                    TotalReports = 1,
                                    Target = client.Client.Connection.EndPoint.Address.ToString(),
                                    Sources = new List<string>()
                                };
                                report.Sources.Add(source.ClientPlayer.Client.Connection.EndPoint.Address.ToString());
                                lock (IpReports) IpReports.Add(report);
                                ChatInterface.SafeMultiMessage(source.Game,
                                    $"A criminal record has been created for {client.Character.PlayerInfo.PlayerName}!",
                                    Structures.BroadcastType.Information, "(server complaints/private)",
                                    source.ClientPlayer);
                            }
                        }

                        return;
                    }
                }

                ChatInterface.SafeMultiMessage(source.Game, "Could not find player", Structures.BroadcastType.Warning,
                    "(server/warn)", source.ClientPlayer);
            }
        }

        /// <summary>
        /// This will add a permanent ban to both the memory list and the disk database.
        /// </summary>
        /// <param name="rep"></param>
        public void AddPermBan(Structures.Report rep)
        {
            lock (PermanentBans)
            {
                PermanentBans.Add(rep);
            }

            File.WriteAllText(Path.Combine(BanFolder, $"ban-{rep.Target}.json"), JsonSerializer.Serialize(rep));
        }
        /// <summary>
        /// Can be used by modified impostor servers to ban players, for e.g anti-cheat reasons.
        /// </summary>
        /// <param name="address">The address of the target to ban.</param>
        /// <param name="name">The player name of the target to ban.</param>
        /// <param name="reason">The reason for the ban. It will appear in the ban file.</param>
        /// <param name="source">The source system that triggered the ban, e.g "RPC Cheat Detection"</param>
        public void ExternalBanPlayer(string address, string name, string reason, string source)
        {
            AddPermBan( new Structures.Report
            {
                Sources = new List<string>()
            {
                source
            },
                Messages = new List<string>()
            {
                reason
            },
                Target = address,
                TargetName = name,
                TotalReports = 0,
                MinutesRemaining = 0
            });
            var message = string.Empty;
            try
            {
                foreach (var possibleTarget in Manager.GetPlayers())
                {
                    var connection = possibleTarget.Client.Connection;
                    if (connection != null)
                    {
                        if (connection.EndPoint.Address.ToString().Equals(address) && possibleTarget!=null)
                        {
                            possibleTarget.BanAsync();
                            message = "The target was also connected to a lobby. He has been kicked.";
                            break;
                        }
                    }

                }

                message = "The target was not playing in any lobby.";
            }
            catch (Exception e)
            {
                message = $"Unfortunately, a critical error has occured : {e.Message}";
            }
            finally
            {
                try
                {
                    Manager.ApiServer.Push($"Critical : external ban function was called, by \"{source}\", because \"{reason}\", banning \"{name}/{address}\". {message}", Structures.ServerSources.ExternalCall, Structures.MessageFlag.ConsoleLogMessage);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"CRITICAL : Push error occured! : {ex.Message}");
                }
            }

        }
        public void ReloadBans()
        {
            lock(PermanentBans) PermanentBans.Clear();
            LoadPermanentBans();
        }

        public bool RemoveBan(string address)
        {
            lock (PermanentBans)
            {
                foreach (var report in PermanentBans)
                {
                    if (report.Target.Equals(address))
                    {
                        PermanentBans.Remove(report);
                        File.Delete(Path.Combine(BanFolder, "ban-" + address + ".json"));
                        return true;
                    }
                }
            }

            return false;
        }
        /// <summary>
        /// Call this whenever a player spawns. It will handle any permanent bans.
        /// </summary>
        /// <param name="evt"></param>
        public void HandleSpawn(IPlayerSpawnedEvent evt)
        {
            lock (PermanentBans)
            {
                foreach (var ban in PermanentBans)
                {
                    if (ban.Target.Equals(evt.ClientPlayer.Client.Connection.EndPoint.Address.ToString()))
                    {
                        evt.ClientPlayer.BanAsync();
                    }
                }
            }
        }

        public bool BanPlayer(IPAddress addr,string dashboard)
        {
            foreach (var possibleTarget in Manager.GetPlayers())
            {
                if(possibleTarget == null || possibleTarget.Client.Connection==null ||possibleTarget.Character == null) continue;
                if (possibleTarget.Client.Connection.EndPoint.Address.Equals(addr))
                {
                    //we found our target!
                    possibleTarget.BanAsync();
                    var report = new Structures.Report
                    {
                        Messages = new List<string>
                    {
                        "<adminsys / " + DateTime.Now.ToString() + ">"
                    },
                        Sources = new List<string>
                    {
                        dashboard
                    },
                        Target = possibleTarget.Client.Connection.EndPoint.Address.ToString(),
                        TargetName = possibleTarget.Character.PlayerInfo.PlayerName,

                        MinutesRemaining = 0,
                        TotalReports = 0
                    };
                    AddPermBan(report);
                    return true;
                }
            }

            return false;
        }

        public delegate void PlayerBanned(string name, string ipa);

        public event PlayerBanned OnPlayerBanned;
    }
}